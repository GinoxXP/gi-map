using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class GeologyActivityMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.GeologyActivity;

    public GeologyActivityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new GeologyChunkMapComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos)
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