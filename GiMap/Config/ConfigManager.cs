using Vintagestory.API.Common;
using Vintagestory.API.Config;

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
            
            CheckCompatibleConfigVersion(api);
        }
        catch (Exception e)
        {
            api.Logger.Error("[GiMap] - Could not load config! Loading default settings instead.");
            api.Logger.Error(e);
            ConfigInstance = new GiMapConfig();
        }
        
        api.StoreModConfig<GiMapConfig>(ConfigInstance, configPath);
    }

    public static void SaveModConfig(ICoreAPI api)
    {
        api.StoreModConfig(ConfigInstance, configPath);
    }

    /// <summary>
    /// Deletes the config file and writes the default settings in its place
    /// </summary>
    public static void ResetModConfig(ICoreAPI api)
    {
        var path = Path.Combine(GamePaths.ModConfig, configPath);
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception e)
        {
            api.Logger.Error("[GiMap] - Could not delete config {0}", path);
            api.Logger.Error(e);
        }

        ConfigInstance = new GiMapConfig();
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