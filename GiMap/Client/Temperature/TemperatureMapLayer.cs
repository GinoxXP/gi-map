using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Temperature;

public class TemperatureMapLayer : AClimateMapLayer
{
    public override string Title => MapTypes.Temperature;

    private static readonly float[] Thresholds = { -10f, -5f, 0f, 5f, 10f, 18f, 25f, 30f, 35f };

    private readonly int[] _colors;

    public TemperatureMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.TemperatureMode;
        _colors = new[]
        {
            ColorUtilExtensions.HexToColor(config.arcticCold),
            ColorUtilExtensions.HexToColor(config.extremeCold),
            ColorUtilExtensions.HexToColor(config.veryCold),
            ColorUtilExtensions.HexToColor(config.cold),
            ColorUtilExtensions.HexToColor(config.cool),
            ColorUtilExtensions.HexToColor(config.temperate),
            ColorUtilExtensions.HexToColor(config.mild),
            ColorUtilExtensions.HexToColor(config.warm),
            ColorUtilExtensions.HexToColor(config.hot),
            ColorUtilExtensions.HexToColor(config.extremeHot),
        };
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_colors[0], Lang.Get("temperature-arctic-cold"));
        FillDictionary(_colors[1], Lang.Get("temperature-extreme-cold"));
        FillDictionary(_colors[2], Lang.Get("temperature-very-cold"));
        FillDictionary(_colors[3], Lang.Get("temperature-cold"));
        FillDictionary(_colors[4], Lang.Get("temperature-cool"));
        FillDictionary(_colors[5], Lang.Get("temperature-temperate"));
        FillDictionary(_colors[6], Lang.Get("temperature-mild"));
        FillDictionary(_colors[7], Lang.Get("temperature-warm"));
        FillDictionary(_colors[8], Lang.Get("temperature-hot"));
        FillDictionary(_colors[9], Lang.Get("temperature-extreme-hot"));
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new TemperatureChunkMapComponent(_capi, baseCord, this);

    protected override float GetValue(ClimateCondition climate)
        => climate.WorldGenTemperature;

    protected override void GetValueRange(ClimateRange range, int minY, int maxY, out float min, out float max)
    {
        // Temperature grows with the climate byte and drops with height
        int seaLevel = api.World.SeaLevel;
        min = Climate.GetScaledAdjustedTemperatureFloatClient(range.MinTemperature, maxY - seaLevel);
        max = Climate.GetScaledAdjustedTemperatureFloatClient(range.MaxTemperature, minY - seaLevel);
    }

    protected override int GetColor(float temperature)
    {
        for (var i = 0; i < Thresholds.Length; i++)
        {
            if (temperature < Thresholds[i])
                return _colors[i];
        }

        return _colors[^1];
    }
}
