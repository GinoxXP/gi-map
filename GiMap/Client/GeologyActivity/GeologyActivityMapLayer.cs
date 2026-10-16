using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.GeologyActivity;

public class GeologyActivityMapLayer : AClimateMapLayer
{
    public override string Title => MapTypes.GeologyActivity;

    private readonly int _lowColor;
    private readonly int _moderateColor;
    private readonly int _significantColor;
    private readonly int _highColor;
    private readonly int _extremeColor;

    public GeologyActivityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.GeologyActivityMode;
        _lowColor = ColorUtilExtensions.HexToColor(config.lowActivity);
        _moderateColor = ColorUtilExtensions.HexToColor(config.moderateActivity);
        _significantColor = ColorUtilExtensions.HexToColor(config.significantActivity);
        _highColor = ColorUtilExtensions.HexToColor(config.highActivity);
        _extremeColor = ColorUtilExtensions.HexToColor(config.extremeActivity);
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_lowColor, Lang.Get("very-low"));
        FillDictionary(_moderateColor, Lang.Get("low"));
        FillDictionary(_significantColor, Lang.Get("medium"));
        FillDictionary(_highColor, Lang.Get("high"));
        FillDictionary(_extremeColor, Lang.Get("very-high"));
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new GeologyChunkMapComponent(_capi, baseCord, this);

    protected override float GetValue(ClimateCondition climate)
        => climate.GeologicActivity;

    protected override void GetValueRange(ClimateRange range, int minY, int maxY, out float min, out float max)
    {
        // Geologic activity does not depend on height
        min = (float)range.MinGeologicActivity / 255f;
        max = (float)range.MaxGeologicActivity / 255f;
    }

    protected override int GetColor(float geologicalActivity)
    {
        if (geologicalActivity < 0.2f)
            return _lowColor;
        if (geologicalActivity < 0.4f)
            return _moderateColor;
        if (geologicalActivity < 0.6f)
            return _significantColor;
        if (geologicalActivity < 0.8f)
            return _highColor;

        return _extremeColor;
    }
}
