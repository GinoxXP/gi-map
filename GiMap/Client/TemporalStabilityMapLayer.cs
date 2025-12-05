using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class TemporalStabilityMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.TemporalStability;

    private readonly SystemTemporalStability temporalStabilitySystem;
    
    public TemporalStabilityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        temporalStabilitySystem = api.ModLoader.GetModSystem<SystemTemporalStability>();
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new TemporalStabilityMultiChunkComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos)
    {
        float stability = temporalStabilitySystem.GetTemporalStability(pos.X, pos.Y, pos.Z);
        
        if (stability < 0.6)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemporalStabilityMode.minColor);
        if (stability < 0.8)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemporalStabilityMode.lowColor);
        if (stability < 1)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemporalStabilityMode.mediumColor);
        if (stability < 1.2)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemporalStabilityMode.highColor);
        
        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemporalStabilityMode.maxColor);
    }
    
    protected override bool IsBlockValid(Block block)
    {
        return block.BlockMaterial == EnumBlockMaterial.Gravel
               || block.BlockMaterial == EnumBlockMaterial.Sand
               || block.BlockMaterial == EnumBlockMaterial.Soil
               || block.BlockMaterial == EnumBlockMaterial.Stone
               || block.BlockMaterial == EnumBlockMaterial.Ice
               || block.BlockMaterial == EnumBlockMaterial.Snow
               || block.BlockMaterial == EnumBlockMaterial.Liquid;
    }
}