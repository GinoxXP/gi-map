using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class ChunkGridMapLayer : ABlockMapLayer<ChunkGridMultiChunkComponent>
{
    private readonly int _borderColor = ColorUtil.ColorFromRgba(0, 0, 0, 255);
    private readonly int _smallBorderColor = ColorUtil.ColorFromRgba(0, 0, 0, 150);
    private readonly int _verySmallBorderColor = ColorUtil.ColorFromRgba(0, 0, 0, 100);
    private readonly int _backgroundColor = ColorUtil.ColorFromRgba(0, 0, 0, 0);

    public override string Title => MapTypes.ChunkGrid;
    
    public ChunkGridMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override ChunkGridMultiChunkComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out ChunkGridMultiChunkComponent mccomp))
            _loadedMapData[mcord] = mccomp = new ChunkGridMultiChunkComponent(api as ICoreClientAPI, baseCord, this);

        return mccomp;
    }

    protected override int GetColor(BlockPos pos)
    {
        if (pos.X % 32 == 0 || pos.Z % 32 == 0)
            return _borderColor;
        
        if (pos.X % 16 == 0 || pos.Z % 16 == 0)
            return _smallBorderColor;
        
        if (pos.X % 8 == 0 || pos.Z % 8 == 0)
            return _verySmallBorderColor;
        
        return _backgroundColor;
    }
}