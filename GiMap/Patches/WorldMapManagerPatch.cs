using GiMap.Client;
using HarmonyLib;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(WorldMapManager), "getTabsOrdered")]
class WorldMapManagerPatch
{
    private static readonly string[] codes =
    {
        MapTypes.Topographic,
        MapTypes.Height,
        MapTypes.Fertility,
        MapTypes.Precipitation,
        MapTypes.Temperature,
        MapTypes.GeologyActivity,
        MapTypes.Light,
        MapTypes.ChunkGrid,
        MapTypes.Ore,
    };
    
    static void Postfix(ref List<string> __result)
    {
        int terrainLayerIndex = __result.FindIndex(x => x.EqualsFast("terrain"));

        for (int i = 0; i < codes.Length; i++)
        {
            __result.Remove(codes[i]);
        }
        
        for (int i = 0; i < codes.Length; i++)
        {
            __result.Insert(terrainLayerIndex + i + 1, codes[i]);
        }
    }
}