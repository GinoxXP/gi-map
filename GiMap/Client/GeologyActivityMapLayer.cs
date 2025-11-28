using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class GeologyActivityMapLayer : AClimateMapLayer<GeologyChunkMapComponent>
{
    private readonly int _lowActivity = ColorUtil.ColorFromRgba(100, 100, 100, 255);
    private readonly int _moderateActivity = ColorUtil.ColorFromRgba(180, 140, 100, 255);
    private readonly int _significantActivity = ColorUtil.ColorFromRgba(230, 190, 80, 255);
    private readonly int _highActivity = ColorUtil.ColorFromRgba(220, 80, 0, 255);
    private readonly int _extremeActivity = ColorUtil.ColorFromRgba(170, 30, 30, 255);
    
    public override string Title => MapTypes.GeologyActivity;

    public GeologyActivityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override GeologyChunkMapComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out GeologyChunkMapComponent mccomp))
            _loadedMapData[mcord] = mccomp = new GeologyChunkMapComponent(api as ICoreClientAPI, baseCord, this);

        return mccomp;
    }

    protected override int GetColor(ClimateCondition climateCondition)
    {
        var geologicalActivity = climateCondition.GeologicActivity;

        if (geologicalActivity < 0.2f)
            return _lowActivity;
        if (geologicalActivity < 0.4f)
            return _moderateActivity;
        if (geologicalActivity < 0.6f)
            return _significantActivity;
        if (geologicalActivity < 0.8f)
            return _highActivity;

        return _extremeActivity;
    }
}