using GiMap.Client;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GiMap;

public class GiMapModSystem : ModSystem
{
    private string patchId = "gimap";
    private Harmony harmonyInstance;
    
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);

        if (api.Side == EnumAppSide.Client)
        {
            ConfigManager.LoadModConfig(api);

            if (!Harmony.HasAnyPatches(patchId))
            {
                harmonyInstance = new Harmony(patchId);
                harmonyInstance.PatchAll();
            }
        }
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        var mapManager = api.ModLoader.GetModSystem<WorldMapManager>();
        mapManager.RegisterMapLayer<TopographicMapLayer>(MapTypes.Topographic, 1);
        mapManager.RegisterMapLayer<HeightMapLayer>(MapTypes.Height, 2);
        mapManager.RegisterMapLayer<FertilityMapLayer>(MapTypes.Fertility, 3);
        mapManager.RegisterMapLayer<PrecipitationMapLayer>(MapTypes.Precipitation, 4);
        mapManager.RegisterMapLayer<TemperatureMapLayer>(MapTypes.Temperature, 5);
        mapManager.RegisterMapLayer<GeologyActivityMapLayer>(MapTypes.GeologyActivity, 6);
        mapManager.RegisterMapLayer<LightMapLayer>(MapTypes.Light, 7);
    }

    public override void Dispose()
    {
        ConfigManager.ConfigInstance = null;
        harmonyInstance?.UnpatchAll(patchId);
        base.Dispose();
    }
}