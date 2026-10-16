using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.TemporalStability;

public class TemporalStabilityMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.TemporalStability;

    private readonly SystemTemporalStability temporalStabilitySystem;

    private readonly int _minColor;
    private readonly int _lowColor;
    private readonly int _mediumColor;
    private readonly int _highColor;
    private readonly int _maxColor;

    public TemporalStabilityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        temporalStabilitySystem = api.ModLoader.GetModSystem<SystemTemporalStability>();

        var config = ConfigManager.ConfigInstance.TemporalStabilityMode;
        _minColor = ColorUtilExtensions.HexToColor(config.minColor);
        _lowColor = ColorUtilExtensions.HexToColor(config.lowColor);
        _mediumColor = ColorUtilExtensions.HexToColor(config.mediumColor);
        _highColor = ColorUtilExtensions.HexToColor(config.highColor);
        _maxColor = ColorUtilExtensions.HexToColor(config.maxColor);
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_minColor, Lang.Get("very-low"));
        FillDictionary(_lowColor, Lang.Get("low"));
        FillDictionary(_mediumColor, Lang.Get("medium"));
        FillDictionary(_highColor, Lang.Get("high"));
        FillDictionary(_maxColor, Lang.Get("very-high"));
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new TemporalStabilityMultiChunkComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos, Block block)
    {
        float stability = temporalStabilitySystem.GetTemporalStability(pos.X, pos.Y, pos.Z);

        if (stability < 0.6)
            return _minColor;
        if (stability < 0.8)
            return _lowColor;
        if (stability < 1)
            return _mediumColor;
        if (stability < 1.2)
            return _highColor;

        return _maxColor;
    }

    protected override bool IsBlockValid(Block block)
    {
        if (!base.IsBlockValid(block))
            return false;

        return block.BlockMaterial == EnumBlockMaterial.Gravel
               || block.BlockMaterial == EnumBlockMaterial.Sand
               || block.BlockMaterial == EnumBlockMaterial.Soil
               || block.BlockMaterial == EnumBlockMaterial.Stone
               || block.BlockMaterial == EnumBlockMaterial.Ice
               || block.BlockMaterial == EnumBlockMaterial.Snow
               || block.BlockMaterial == EnumBlockMaterial.Water;
    }
}
