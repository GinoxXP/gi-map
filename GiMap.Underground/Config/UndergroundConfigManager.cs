using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace GiMap.Underground.Config;

public static class UndergroundConfigManager
{
    public static UndergroundConfig ConfigInstance { get; private set; } = new();
    private static readonly string configPath = "GiMapUndergroundConfig.json";

    public static void LoadModConfig(ICoreAPI api)
    {
        try
        {
            ConfigInstance = api.LoadModConfig<UndergroundConfig>(configPath) ?? new UndergroundConfig();
        }
        catch (Exception e)
        {
            api.Logger.Error("[GiMap: Underground] - Could not load config! Loading default settings instead.");
            api.Logger.Error(e);
            ConfigInstance = new UndergroundConfig();
        }

        api.StoreModConfig(ConfigInstance, configPath);
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
            api.Logger.Error("[GiMap: Underground] - Could not delete config {0}", path);
            api.Logger.Error(e);
        }

        ConfigInstance = new UndergroundConfig();
        api.StoreModConfig(ConfigInstance, configPath);
    }
}
