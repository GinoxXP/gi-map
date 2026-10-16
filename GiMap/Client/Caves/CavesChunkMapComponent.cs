using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Caves;

public class CavesChunkMapComponent : AChunkMapComponent
{
    private const int SideLength = 96;

    // Packed floors of all layers, pixels of the current layer are built from them
    private readonly int[]?[] _data = new int[9][];
    private readonly CavesMapLayer _cavesLayer;
    private Vec3d _mouseWorldPos = new();

    public CavesChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, CavesMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
        _cavesLayer = mapLayer;
    }

    public override void setChunk(int dx, int dz, int[] data)
    {
        _data[dz * 3 + dx] = data;
        base.setChunk(dx, dz, _cavesLayer.BuildPixels(data));
    }

    public override void unsetChunk(int dx, int dz)
    {
        _data[dz * 3 + dx] = null;
        base.unsetChunk(dx, dz);
    }

    /// <summary>
    /// Rebuilds the texture for the current layer of <see cref="CavesMapLayer"/>
    /// </summary>
    public void RefreshLayer()
    {
        var isAnySet = false;
        for (var i = 0; i < _data.Length; i++)
        {
            var data = _data[i];
            if (data == null)
                continue;

            base.setChunk(i % 3, i / 3, _cavesLayer.BuildPixels(data));
            isAnySet = true;
        }

        if (!isAnySet)
            return;

        ResetTexture();
        FinishSetChunks();
    }

    public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        _viewPos.X = args.X - (float)mapElem.Bounds.renderX;
        _viewPos.Y = args.Y - (float)mapElem.Bounds.renderY;
        mapElem.TranslateViewPosToWorldPos(_viewPos, ref _mouseWorldPos);

        if (_mouseWorldPos.X < _chunkWorldPos.X ||
            _mouseWorldPos.X >= _chunkWorldPos.X + SideLength ||
            _mouseWorldPos.Z < _chunkWorldPos.Z ||
            _mouseWorldPos.Z >= _chunkWorldPos.Z + SideLength)
            return;

        var posInChunk = _mouseWorldPos - _chunkWorldPos;
        var data = _data[posInChunk.XInt / 32 + posInChunk.ZInt / 32 * 3];
        if (data == null)
            return;

        var text = _cavesLayer.GetHoverText(data, posInChunk.XInt % 32 + posInChunk.ZInt % 32 * 32);
        if (text != null)
            hoverText.Append(text);
    }
}
