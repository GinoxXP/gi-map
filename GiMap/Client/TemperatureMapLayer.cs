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
            return ConfigManager.ConfigInstance.TemperatureMode._arcticCold;
        if (temperature < -5f)
            return ConfigManager.ConfigInstance.TemperatureMode._extremeCold;
        if (temperature < 0f)
            return ConfigManager.ConfigInstance.TemperatureMode._veryCold;
        if (temperature < 5f)
            return ConfigManager.ConfigInstance.TemperatureMode._cold;
        if (temperature < 10f)
            return ConfigManager.ConfigInstance.TemperatureMode._cool;
        if (temperature < 18f)
            return ConfigManager.ConfigInstance.TemperatureMode._temperate;
        if (temperature < 25f)
            return ConfigManager.ConfigInstance.TemperatureMode._mild;
        if (temperature < 30f)
            return ConfigManager.ConfigInstance.TemperatureMode._warm;
        if (temperature < 35f) 
            return ConfigManager.ConfigInstance.TemperatureMode._hot;

        return ConfigManager.ConfigInstance.TemperatureMode._extremeHot;
    }
}