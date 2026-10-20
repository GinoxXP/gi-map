using GiMap.Client;
using GiMap.Client.Bathymetric;
using GiMap.Client.Chunk;
using GiMap.Client.Fertility;
using GiMap.Client.GeologyActivity;
using GiMap.Client.Height;
using GiMap.Client.Light;
using GiMap.Client.Notes;
using GiMap.Client.Precipitation;
using GiMap.Client.Temperature;
using GiMap.Client.TemporalStability;
using GiMap.Client.Topographic;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using ButterfliesMapLayer = GiMap.Client.Butterflies.ButterfliesMapLayer;

namespace GiMap;

public class GiMapModSystem : ModSystem
{
    private string patchId = "gimap";
    private Harmony harmonyInstance;

    public static string Version { get; private set;}

    public static ICoreClientAPI? ClientApi { get; private set; }

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);

        Version = Mod.Info.Version;
        
        if (api.Side == EnumAppSide.Client)
        {
            ConfigManager.LoadModConfig(api);
            RegisterModes(api);

            if (!Harmony.HasAnyPatches(patchId))
            {
                harmonyInstance = new Harmony(patchId);
                harmonyInstance.PatchAll();
            }
        }
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        ClientApi = api;
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
        
        if (ConfigManager.ConfigInstance.TemporalStabilityMode.isEnabled)
            mapManager.RegisterMapLayer<TemporalStabilityMapLayer>(MapTypes.TemporalStability, 10);

        if (ConfigManager.ConfigInstance.BathymetricMode.isEnabled)
            mapManager.RegisterMapLayer<BathymetricMapLayer>(MapTypes.Bathymetric, 12);

        if (ConfigManager.ConfigInstance.ButterfliesMode.isEnabled)
            mapManager.RegisterMapLayer<ButterfliesMapLayer>(MapTypes.Butterflies, 13);

        if (ConfigManager.ConfigInstance.NotesMode.isEnabled)
            mapManager.RegisterMapLayer<NotesMapLayer>(MapTypes.Notes, 14);
    }

    // Tab orders leave gaps for add-on modes: caves 40, ore 110
    private static void RegisterModes(ICoreAPI api)
    {
        void Register(string mapType, int order, System.Func<GiMapConfig, bool> get, Action<GiMapConfig, bool> set)
            => GiMapModes.Register(mapType, order,
                () => get(ConfigManager.ConfigInstance),
                value => set(ConfigManager.ConfigInstance, value),
                () => ConfigManager.SaveModConfig(api));

        Register(MapTypes.Topographic, 10, c => c.TopographicMode.isEnabled, (c, v) => c.TopographicMode.isEnabled = v);
        Register(MapTypes.Height, 20, c => c.HeightMode.isEnabled, (c, v) => c.HeightMode.isEnabled = v);
        Register(MapTypes.Bathymetric, 30, c => c.BathymetricMode.isEnabled, (c, v) => c.BathymetricMode.isEnabled = v);
        Register(MapTypes.Fertility, 50, c => c.FertilityMode.isEnabled, (c, v) => c.FertilityMode.isEnabled = v);
        Register(MapTypes.Precipitation, 60, c => c.PrecipitationMode.isEnabled, (c, v) => c.PrecipitationMode.isEnabled = v);
        Register(MapTypes.Temperature, 70, c => c.TemperatureMode.isEnabled, (c, v) => c.TemperatureMode.isEnabled = v);
        Register(MapTypes.GeologyActivity, 80, c => c.GeologyActivityMode.isEnabled, (c, v) => c.GeologyActivityMode.isEnabled = v);
        Register(MapTypes.Light, 90, c => c.LightMode.isEnabled, (c, v) => c.LightMode.isEnabled = v);
        Register(MapTypes.ChunkGrid, 100, c => c.ChunkGridMode.isEnabled, (c, v) => c.ChunkGridMode.isEnabled = v);
        Register(MapTypes.TemporalStability, 120, c => c.TemporalStabilityMode.isEnabled, (c, v) => c.TemporalStabilityMode.isEnabled = v);
        Register(MapTypes.Butterflies, 130, c => c.ButterfliesMode.isEnabled, (c, v) => c.ButterfliesMode.isEnabled = v);
        Register(MapTypes.Notes, 140, c => c.NotesMode.isEnabled, (c, v) => c.NotesMode.isEnabled = v);
    }

    public override void Dispose()
    {
        GiMapModes.Clear();
        ConfigManager.ConfigInstance = null;
        ClientApi = null;
        harmonyInstance?.UnpatchAll(patchId);
        base.Dispose();
    }
}