using Vintagestory.API.Common;

namespace GiMap.Config;

public static class ConfigManager
{
    public static GiMapConfig ConfigInstance { get; internal set; }
    private static string configPath = "GiMapConfig.json";

    public static void LoadModConfig(ICoreAPI api)
    {
        try
        {
            ConfigInstance = api.LoadModConfig<GiMapConfig>(configPath);
            if (ConfigInstance == null)
            {
                ConfigInstance = new GiMapConfig();
            }

            api.StoreModConfig<GiMapConfig>(ConfigInstance, configPath);
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
}