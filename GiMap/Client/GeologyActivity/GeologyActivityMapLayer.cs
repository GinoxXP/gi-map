using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.GeologyActivity;

public class GeologyActivityMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.GeologyActivity;

    public GeologyActivityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.lowActivity), Lang.Get("very-low"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.moderateActivity), Lang.Get("low"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.significantActivity), Lang.Get("medium"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.highActivity), Lang.Get("high"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.extremeActivity), Lang.Get("very-high"));
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new GeologyChunkMapComponent(Capi, baseCord, this);

    protected override int GetColor(BlockPos pos, Block block)
    {
        var climateCondition = api.World.BlockAccessor.GetClimateAt(pos);
        var geologicalActivity = climateCondition.GeologicActivity;

        if (geologicalActivity < 0.2f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.lowActivity);
        if (geologicalActivity < 0.4f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.moderateActivity);
        if (geologicalActivity < 0.6f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.significantActivity);
        if (geologicalActivity < 0.8f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.highActivity);

        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.GeologyActivityMode.extremeActivity);
    }
}