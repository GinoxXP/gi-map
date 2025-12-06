using GiMap.Client;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using OreMapLayer = GiMap.Client.OreMapLayer;

namespace GiMap;

public class GiMapModSystem : ModSystem
{
    private string patchId = "gimap";
    private Harmony harmonyInstance;

    public static string Version { get; private set;}
    
    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);

        Version = Mod.Info.Version;
        
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
        
        if (ConfigManager.ConfigInstance.TopographicMode.isEnabled)
            mapManager.RegisterMapLayer<TopographicMapLayer>(MapTypes.Topographic, 1);
        
        if (ConfigManager.ConfigInstance.HeightMode.isEnabled)
            mapManager.RegisterMapLayer<HeightMapLayer>(MapTypes.Height, 2);
        
        if (ConfigManager.ConfigInstance.FertilityMode.isEnabled)
            mapManager.RegisterMapLayer<FertilityMapLayer>(MapTypes.Fertility, 3);
        
        if (ConfigManager.ConfigInstance.PrecipitationMode.isEnabled)
            mapManager.RegisterMapLayer<PrecipitationMapLayer>(MapTypes.Precipitation, 4);
        
        if (ConfigManager.ConfigInstance.TemperatureMode.isEnabled)
            mapManager.RegisterMapLayer<TemperatureMapLayer>(MapTypes.Temperature, 5);
        
        if (ConfigManager.ConfigInstance.GeologyActivityMode.isEnabled)
            mapManager.RegisterMapLayer<GeologyActivityMapLayer>(MapTypes.GeologyActivity, 6);
        
        if (ConfigManager.ConfigInstance.LightMode.isEnabled)
            mapManager.RegisterMapLayer<LightMapLayer>(MapTypes.Light, 7);
        
        if (ConfigManager.ConfigInstance.ChunkGridMode.isEnabled)
            mapManager.RegisterMapLayer<ChunkGridMapLayer>(MapTypes.ChunkGrid, 8);
        
        if (ConfigManager.ConfigInstance.OreMode.isEnabled)
            mapManager.RegisterMapLayer<OreMapLayer>(MapTypes.Ore, 9);
        
        if (ConfigManager.ConfigInstance.TemporalStabilityMode.isEnabled)
            mapManager.RegisterMapLayer<TemporalStabilityMapLayer>(MapTypes.TemporalStability, 10);
    }

    public override void Dispose()
    {
        ConfigManager.ConfigInstance = null;
        harmonyInstance?.UnpatchAll(patchId);
        base.Dispose();
    }
}