using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class TemperatureMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.Temperature;

    public TemperatureMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new TemperatureChunkMapComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos)
    {
        var climateCondition = api.World.BlockAccessor.GetClimateAt(pos);
        var temperature = climateCondition.WorldGenTemperature;

        if (temperature < -10f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.arcticCold);
        if (temperature < -5f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.extremeCold);
        if (temperature < 0f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.veryCold);
        if (temperature < 5f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.cold);
        if (temperature < 10f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.cool);
        if (temperature < 18f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.temperate);
        if (temperature < 25f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.mild);
        if (temperature < 30f)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.warm);
        if (temperature < 35f) 
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.hot);

        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TemperatureMode.extremeHot);
    }
}