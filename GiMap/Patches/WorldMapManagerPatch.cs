using GiMap.Client;
using HarmonyLib;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(WorldMapManager), "getTabsOrdered")]
class WorldMapManagerPatch
{
    // Mod layers go after "terrain" in the order of GiMapModes
    static void Postfix(ref List<string> __result)
    {
        var terrainLayerIndex = __result.FindIndex(x => x.EqualsFast("terrain"));

        // Only registered layers are moved: isEnabled may be changed in the settings until the world is rejoined
        var registered = GiMapModes.All
            .Select(mode => mode.MapType)
            .Where(mapType => mapType != MapTypes.Notes)
            .Where(__result.Remove)
            .ToList();

        // Notes have no tab: they are always shown, and a layer without a tab is never deactivated
        __result.Remove(MapTypes.Notes);

        __result.InsertRange(Math.Min(terrainLayerIndex + 1, __result.Count), registered);
    }
}
