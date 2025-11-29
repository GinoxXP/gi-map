using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class PrecipitationMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.Precipitation;
    
    public PrecipitationMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new PrecipitationMultiChunkMapComponent(_capi, baseCord, this);
    
    protected override int GetColor(BlockPos pos)
    {
        var climateCondition = api.World.BlockAccessor.GetClimateAt(pos);
        var rainfall = climateCondition.WorldgenRainfall;
        return GetColor(rainfall);
    }
    
    private int GetColor(float precipitation)
    {
        if (precipitation < 0.15f) return ConfigManager.ConfigInstance.PrecipitationMode.veryLowPrecipitation; 
        if (precipitation < 0.45f) return ConfigManager.ConfigInstance.PrecipitationMode.lowPrecipitation; 
        if (precipitation < 0.70f) return ConfigManager.ConfigInstance.PrecipitationMode.mediumPrecipitation; 
        if (precipitation < 0.90f) return ConfigManager.ConfigInstance.PrecipitationMode.highPrecipitation;

        return ConfigManager.ConfigInstance.PrecipitationMode.veryHighPrecipitation; 
    }
}