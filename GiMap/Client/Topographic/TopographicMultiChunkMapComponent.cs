using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Topographic;

public class TopographicMultiChunkMapComponent : AChunkMapComponent
{
    // In blocks: index contour labels of the same height closer than this are dropped
    private const int SameHeightLabelDistance = 40;
    // In blocks: no label is placed closer than this to another label
    private const int AnyLabelDistance = 8;

    private readonly HeightLabel[]?[] _chunkLabels = new HeightLabel[9][];
    // Labels of all chunks of the component, in component coordinates, after thinning out
    private readonly List<HeightLabel> _visibleLabels = new();
    private bool _isLabelsDirty;

    private readonly Vec3d _labelWorldPos = new();
    private Vec2f _labelViewPos = new();

    public TopographicMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, TopographicMapLayer mapLayer): base(capi, baseChunkCord, mapLayer)
    {
    }

    public override void setChunk(int dx, int dz, int[] pixels)
    {
        _chunkLabels[dz * 3 + dx] = HeightLabel.Extract(pixels);
        _isLabelsDirty = true;

        if (pixels.Length > HeightLabel.ChunkPixelCount)
            pixels = pixels[..HeightLabel.ChunkPixelCount];

        base.setChunk(dx, dz, pixels);
    }

    public override void unsetChunk(int dx, int dz)
    {
        _chunkLabels[dz * 3 + dx] = null;
        _isLabelsDirty = true;

        base.unsetChunk(dx, dz);
    }

    public override void Render(GuiElementMap map, float dt)
    {
        base.Render(map, dt);

        if (_mapLayer is not TopographicMapLayer layer || !layer.ShowHeightLabels)
            return;

        bool showContourLabels = map.ZoomLevel >= layer.ContourLabelsMinZoom;
        bool showPeakLabels = map.ZoomLevel >= layer.PeakLabelsMinZoom;
        if (!showContourLabels && !showPeakLabels)
            return;

        if (_isLabelsDirty)
            RebuildVisibleLabels();

        var color = layer.GetLabelColor();
        float z = renderZ + 1;

        foreach (var label in _visibleLabels)
        {
            if (label.IsPeak ? !showPeakLabels : !showContourLabels)
                continue;

            _labelWorldPos.Set(_chunkWorldPos.X + label.X + 0.5, 0, _chunkWorldPos.Z + label.Z + 0.5);
            map.TranslateWorldPosToViewPos(_labelWorldPos, ref _labelViewPos);
            float x = (float)(map.Bounds.renderX + _labelViewPos.X);
            float y = (float)(map.Bounds.renderY + _labelViewPos.Y);

            var text = layer.GetLabelTexture(label.Height, label.IsPeak);
            if (label.IsPeak)
            {
                var dot = layer.GetPeakDotTexture();
                capi.Render.Render2DTexturePremultipliedAlpha(dot.TextureId, x - dot.Width / 2f, y - dot.Height / 2f, dot.Width, dot.Height, z, color);
                capi.Render.Render2DTexturePremultipliedAlpha(text.TextureId, x + dot.Width / 2f, y - text.Height / 2f, text.Width, text.Height, z, color);
            }
            else
            {
                capi.Render.Render2DTexturePremultipliedAlpha(text.TextureId, x - text.Width / 2f, y - text.Height / 2f, text.Width, text.Height, z, color);
            }
        }
    }

    // Peaks are always kept. Index contour labels closest to the component center win,
    // so a long line gets one label per component instead of one per chunk.
    private void RebuildVisibleLabels()
    {
        _isLabelsDirty = false;
        _visibleLabels.Clear();

        var contourLabels = new List<HeightLabel>();
        for (var i = 0; i < _chunkLabels.Length; i++)
        {
            var labels = _chunkLabels[i];
            if (labels == null)
                continue;

            int offsetX = i % 3 * 32;
            int offsetZ = i / 3 * 32;
            foreach (var label in labels)
            {
                var componentLabel = label with { X = label.X + offsetX, Z = label.Z + offsetZ };
                if (label.IsPeak)
                    _visibleLabels.Add(componentLabel);
                else
                    contourLabels.Add(componentLabel);
            }
        }

        contourLabels.Sort((a, b) => DistanceToCenter(a).CompareTo(DistanceToCenter(b)));
        foreach (var label in contourLabels)
        {
            if (CanPlace(label))
                _visibleLabels.Add(label);
        }
    }

    private bool CanPlace(HeightLabel label)
    {
        foreach (var other in _visibleLabels)
        {
            int dx = label.X - other.X;
            int dz = label.Z - other.Z;
            int distance = dx * dx + dz * dz;

            if (distance < AnyLabelDistance * AnyLabelDistance)
                return false;

            if (other.Height == label.Height && !other.IsPeak && distance < SameHeightLabelDistance * SameHeightLabelDistance)
                return false;
        }

        return true;
    }

    private static int DistanceToCenter(HeightLabel label)
    {
        int dx = label.X - 48;
        int dz = label.Z - 48;
        return dx * dx + dz * dz;
    }

    public override void Dispose()
    {
        base.Dispose();
        Array.Clear(_chunkLabels);
        _visibleLabels.Clear();
    }
}
