using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class ChunkGridMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.ChunkGrid;
    
    public ChunkGridMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new ChunkGridMultiChunkComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos)
    {
        if (pos.X % 32 == 0 || pos.Z % 32 == 0)
            return ConfigManager.ConfigInstance.ChunkGridMode.borderColor;
        
        if (pos.X % 16 == 0 || pos.Z % 16 == 0)
            return ConfigManager.ConfigInstance.ChunkGridMode.smallBorderColor;
        
        if (pos.X % 8 == 0 || pos.Z % 8 == 0)
            return ConfigManager.ConfigInstance.ChunkGridMode.verySmallBorderColor;
        
        return ConfigManager.ConfigInstance.ChunkGridMode.backgroundColor;
    }
}