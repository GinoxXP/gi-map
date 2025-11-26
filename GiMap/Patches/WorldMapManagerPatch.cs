using HarmonyLib;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(WorldMapManager), "getTabsOrdered")]
class WorldMapManagerPatch
{
    static void Postfix(ref List<string> __result)
    {
        var topographicCode = "topographic";
        var heightCode = "height";
        int terrainLayerIndex = __result.FindIndex(x => x.EqualsFast("terrain"));
        __result.Remove(topographicCode);
        __result.Remove(heightCode);
        __result.Insert(terrainLayerIndex + 1, topographicCode);
        __result.Insert(terrainLayerIndex + 2, heightCode);
    }
}