using Vintagestory.API.Client;

namespace GiMap.Patches;

// The state is kept in the game's client settings so it survives restarts without being part of the mod config
static class BigWorldMap
{
    private const string SettingKey = "gimapBigWorldMap";

    public static bool IsEnabled(ICoreClientAPI capi)
        => capi.Settings.Bool.Exists(SettingKey) && capi.Settings.Bool[SettingKey];

    public static void Toggle(ICoreClientAPI capi)
        => capi.Settings.Bool[SettingKey] = !IsEnabled(capi);
}
