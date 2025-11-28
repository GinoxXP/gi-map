using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class TemperatureMapLayer : AClimateMapLayer<TemperatureChunkMapComponent>
{
    private readonly int _arcticCold = ColorUtil.ColorFromRgba(0, 0, 100, 255);
    private readonly int _extremeCold = ColorUtil.ColorFromRgba(0, 50, 180, 255);
    private readonly int _veryCold = ColorUtil.ColorFromRgba(0, 100, 220, 255);
    private readonly int _cold = ColorUtil.ColorFromRgba(100, 180, 255, 255);

    private readonly int _cool = ColorUtil.ColorFromRgba(150, 220, 150, 255);
    private readonly int _temperate = ColorUtil.ColorFromRgba(50, 200, 50, 255);
    private readonly int _mild = ColorUtil.ColorFromRgba(200, 200, 80, 255);

    private readonly int _warm = ColorUtil.ColorFromRgba(240, 180, 0, 255);
    private readonly int _hot = ColorUtil.ColorFromRgba(220, 80, 0, 255);
    private readonly int _extremeHot = ColorUtil.ColorFromRgba(150, 30, 30, 255);
    
    public override string Title => MapTypes.Temperature;

    public TemperatureMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    protected override TemperatureChunkMapComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out TemperatureChunkMapComponent mccomp))
            _loadedMapData[mcord] = mccomp = new TemperatureChunkMapComponent(api as ICoreClientAPI, baseCord, this);

        return mccomp;
    }

    protected override int GetColor(ClimateCondition climateCondition)
    {
        var temperature = climateCondition.WorldGenTemperature;

        if (temperature < -10f)
            return _arcticCold;
        if (temperature < -5f)
            return _extremeCold;
        if (temperature < 0f)
            return _veryCold;
        if (temperature < 5f)
            return _cold;
        if (temperature < 10f)
            return _cool;
        if (temperature < 18f)
            return _temperate;
        if (temperature < 25f)
            return _mild;
        if (temperature < 30f)
            return _warm;
        if (temperature < 35f) 
            return _hot;

        return _extremeHot;
    }
}