using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Precipitation;

public class PrecipitationMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.Precipitation;
    
    public PrecipitationMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        FillDictionaries();
    }
    
    private void FillDictionaries()
    {
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.veryLowPrecipitation), Lang.Get("very-low"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.lowPrecipitation), Lang.Get("low"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.mediumPrecipitation), Lang.Get("medium"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.highPrecipitation), Lang.Get("high"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.veryHighPrecipitation), Lang.Get("very-high"));
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new PrecipitationMultiChunkMapComponent(Capi, baseCord, this);
    
    protected override int GetColor(BlockPos pos, Block block)
    {
        var climateCondition = api.World.BlockAccessor.GetClimateAt(pos);
        var rainfall = climateCondition.WorldgenRainfall;
        return GetColor(rainfall);
    }
    
    private int GetColor(float precipitation)
    {
        if (precipitation < 0.15f) return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.veryLowPrecipitation); 
        if (precipitation < 0.45f) return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.lowPrecipitation); 
        if (precipitation < 0.70f) return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.mediumPrecipitation); 
        if (precipitation < 0.90f) return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.highPrecipitation);

        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.PrecipitationMode.veryHighPrecipitation); 
    }
}