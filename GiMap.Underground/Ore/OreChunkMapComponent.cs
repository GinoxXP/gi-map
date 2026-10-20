using System.Text;
using GiMap.Client;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Underground.Ore;

public class OreChunkMapComponent : AChunkMapComponent
{
    private const int SideLength = 96;

    // Packed ore groups of the columns, pixels of the shown ores are built from them
    private readonly int[]?[] _data = new int[9][];
    private readonly OreMapLayer _oreLayer;
    private Vec3d _mouseWorldPos = new();

    public OreChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, OreMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
        _oreLayer = mapLayer;
    }

    public override void setChunk(int dx, int dz, int[] data)
    {
        _data[dz * 3 + dx] = data;
        base.setChunk(dx, dz, _oreLayer.BuildPixels(data));
    }

    public override void unsetChunk(int dx, int dz)
    {
        _data[dz * 3 + dx] = null;
        base.unsetChunk(dx, dz);
    }

    /// <summary>
    /// Rebuilds the texture for the shown ores of <see cref="OreMapLayer"/>
    /// </summary>
    public void RefreshFilter()
    {
        var isAnySet = false;
        for (var i = 0; i < _data.Length; i++)
        {
            var data = _data[i];
            if (data == null)
                continue;

            base.setChunk(i % 3, i / 3, _oreLayer.BuildPixels(data));
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
        int k = posInChunk.XInt % 32 + posInChunk.ZInt % 32 * 32;
        if (data == null || k >= data.Length)
            return;

        var text = _oreLayer.GetHoverText(data[k]);
        if (text != null)
            hoverText.Append(text);
    }
}
