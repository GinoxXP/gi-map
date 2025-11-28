using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class PrecipitationMapLayer : AClimateMapLayer<PrecipitationMultiChunkMapComponent>
{
    private readonly int _veryHighPrecipitation = ColorUtil.ColorFromRgba(70, 130, 230, 255);
    private readonly int _highPrecipitation = ColorUtil.ColorFromRgba(40, 175, 175, 255);
    private readonly int _mediumPrecipitation = ColorUtil.ColorFromRgba(110, 190, 110, 255);
    private readonly int _lowPrecipitation = ColorUtil.ColorFromRgba(190, 190, 100, 255);
    private readonly int _veryLowPrecipitation = ColorUtil.ColorFromRgba(240, 230, 160, 255);
 
    public override string Title => MapTypes.Precipitation;
    
    public PrecipitationMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override PrecipitationMultiChunkMapComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out PrecipitationMultiChunkMapComponent mccomp))
            _loadedMapData[mcord] = mccomp = new PrecipitationMultiChunkMapComponent(api as ICoreClientAPI, baseCord, this);

        return mccomp;
    }
    
    protected override int GetColor(ClimateCondition climateCondition)
    {
        var rainfall = climateCondition.WorldgenRainfall;
        return GetColor(rainfall);
    }
    
    private int GetColor(float precipitation)
    {
        if (precipitation < 0.15f) return _veryLowPrecipitation; 
        if (precipitation < 0.45f) return _lowPrecipitation; 
        if (precipitation < 0.70f) return _mediumPrecipitation; 
        if (precipitation < 0.90f) return _highPrecipitation;

        return _veryHighPrecipitation; 
    }
}