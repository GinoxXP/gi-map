using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Temperature;

public class TemperatureMapLayer : ABlockMapLayer
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

    protected override int GetColor(BlockPos pos, Block block)
    {
        var climateCondition = api.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
        var temperature = climateCondition.WorldGenTemperature;

        for (var i = 0; i < Thresholds.Length; i++)
        {
            if (temperature < Thresholds[i])
                return _colors[i];
        }

        return _colors[^1];
    }
}
