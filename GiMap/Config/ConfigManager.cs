using Vintagestory.API.Common;

namespace GiMap.Config;

public static class ConfigManager
{
    public static GiMapConfig ConfigInstance { get; internal set; }
    private static readonly string configPath = "GiMapConfig.json";
    private static readonly string backupConfigPath = "GiMapConfig.jsonbackup";
    
    private static readonly string[] compatibleConfigVersions = [GiMapModSystem.Version];

    public static void LoadModConfig(ICoreAPI api)
    {
        try
        {
            ConfigInstance = api.LoadModConfig<GiMapConfig>(configPath);
            if (ConfigInstance == null)
                ConfigInstance = new GiMapConfig();

            api.StoreModConfig<GiMapConfig>(ConfigInstance, configPath);

            CheckCompatibleConfigVersion(api);
        }
        catch (Exception e)
        {
            api.Logger.Error("[GiMap] - Could not load config! Loading default settings instead.");
            api.Logger.Error(e);
            ConfigInstance = new GiMapConfig();
        }
    }

    public static void SaveModConfig(ICoreAPI api)
    {
        api.StoreModConfig(ConfigInstance, configPath);
    }

    private static void CheckCompatibleConfigVersion(ICoreAPI api)
    {
        if (compatibleConfigVersions.Any(version => version == ConfigInstance.Version))
            return;
        
        api.StoreModConfig(ConfigInstance, backupConfigPath);
        
        throw new Exception("Unsupported config version! Your old config has been saved as jsonbackup.");
    }
}