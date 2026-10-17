using GiMap.Client;
using HarmonyLib;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(WorldMapManager), "getTabsOrdered")]
class WorldMapManagerPatch
{
    // Tab order of the mod layers after "terrain"
    private static readonly string[] Order =
    [
        MapTypes.Topographic,
        MapTypes.Height,
        MapTypes.Bathymetric,
        MapTypes.Caves,
        MapTypes.Fertility,
        MapTypes.Precipitation,
        MapTypes.Temperature,
        MapTypes.GeologyActivity,
        MapTypes.Light,
        MapTypes.ChunkGrid,
        MapTypes.Ore,
        MapTypes.TemporalStability,
    ];

    static void Postfix(ref List<string> __result)
    {
        var terrainLayerIndex = __result.FindIndex(x => x.EqualsFast("terrain"));

        // Only registered layers are moved: isEnabled may be changed in the settings until the world is rejoined
        var registered = Order.Where(__result.Remove).ToList();

        // Notes have no tab: they are always shown, and a layer without a tab is never deactivated
        __result.Remove(MapTypes.Notes);

        __result.InsertRange(Math.Min(terrainLayerIndex + 1, __result.Count), registered);
    }
}
