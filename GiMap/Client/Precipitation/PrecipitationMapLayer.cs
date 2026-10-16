using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Precipitation;

public class PrecipitationMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.Precipitation;

    private readonly int _veryLowColor;
    private readonly int _lowColor;
    private readonly int _mediumColor;
    private readonly int _highColor;
    private readonly int _veryHighColor;

    public PrecipitationMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.PrecipitationMode;
        _veryLowColor = ColorUtilExtensions.HexToColor(config.veryLowPrecipitation);
        _lowColor = ColorUtilExtensions.HexToColor(config.lowPrecipitation);
        _mediumColor = ColorUtilExtensions.HexToColor(config.mediumPrecipitation);
        _highColor = ColorUtilExtensions.HexToColor(config.highPrecipitation);
        _veryHighColor = ColorUtilExtensions.HexToColor(config.veryHighPrecipitation);
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_veryLowColor, Lang.Get("very-low"));
        FillDictionary(_lowColor, Lang.Get("low"));
        FillDictionary(_mediumColor, Lang.Get("medium"));
        FillDictionary(_highColor, Lang.Get("high"));
        FillDictionary(_veryHighColor, Lang.Get("very-high"));
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new PrecipitationMultiChunkMapComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos, Block block)
    {
        var climateCondition = api.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
        var rainfall = climateCondition.WorldgenRainfall;
        return GetColor(rainfall);
    }

    private int GetColor(float precipitation)
    {
        if (precipitation < 0.15f) return _veryLowColor;
        if (precipitation < 0.45f) return _lowColor;
        if (precipitation < 0.70f) return _mediumColor;
        if (precipitation < 0.90f) return _highColor;

        return _veryHighColor;
    }
}
