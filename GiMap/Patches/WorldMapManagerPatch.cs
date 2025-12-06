using GiMap.Client;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(WorldMapManager), "getTabsOrdered")]
class WorldMapManagerPatch
{
    static void Postfix(ref List<string> __result)
    {
        var terrainLayerIndex = __result.FindIndex(x => x.EqualsFast("terrain"));

        Remove(__result);
        
        Insert(__result, terrainLayerIndex + 1);
    }

    static void Remove(List<string> result)
    {
        if (ConfigManager.ConfigInstance.TopographicMode.isEnabled)
            result.Remove(MapTypes.Topographic);
        
        if (ConfigManager.ConfigInstance.HeightMode.isEnabled)
            result.Remove(MapTypes.Height);
        
        if (ConfigManager.ConfigInstance.FertilityMode.isEnabled)
            result.Remove(MapTypes.Fertility);
        
        if (ConfigManager.ConfigInstance.PrecipitationMode.isEnabled)
            result.Remove(MapTypes.Precipitation);
        
        if (ConfigManager.ConfigInstance.TemperatureMode.isEnabled)
            result.Remove(MapTypes.Temperature);
        
        if (ConfigManager.ConfigInstance.GeologyActivityMode.isEnabled)
            result.Remove(MapTypes.GeologyActivity);
        
        if (ConfigManager.ConfigInstance.LightMode.isEnabled)
            result.Remove(MapTypes.Light);
        
        if (ConfigManager.ConfigInstance.ChunkGridMode.isEnabled)
            result.Remove(MapTypes.ChunkGrid);
        
        if (ConfigManager.ConfigInstance.OreMode.isEnabled)
            result.Remove(MapTypes.Ore);
        
        if (ConfigManager.ConfigInstance.TemporalStabilityMode.isEnabled)
            result.Remove(MapTypes.TemporalStability);
    }
    
    static void Insert(List<string> result, int index)
    {
        if (ConfigManager.ConfigInstance.TopographicMode.isEnabled)
            result.Insert(index++, MapTypes.Topographic);
        
        if (ConfigManager.ConfigInstance.HeightMode.isEnabled)
            result.Insert(index++, MapTypes.Height);
        
        if (ConfigManager.ConfigInstance.FertilityMode.isEnabled)
            result.Insert(index++, MapTypes.Fertility);
        
        if (ConfigManager.ConfigInstance.PrecipitationMode.isEnabled)
            result.Insert(index++, MapTypes.Precipitation);
        
        if (ConfigManager.ConfigInstance.TemperatureMode.isEnabled)
            result.Insert(index++, MapTypes.Temperature);
        
        if (ConfigManager.ConfigInstance.GeologyActivityMode.isEnabled)
            result.Insert(index++, MapTypes.GeologyActivity);
        
        if (ConfigManager.ConfigInstance.LightMode.isEnabled)
            result.Insert(index++, MapTypes.Light);
        
        if (ConfigManager.ConfigInstance.ChunkGridMode.isEnabled)
            result.Insert(index++, MapTypes.ChunkGrid);
        
        if (ConfigManager.ConfigInstance.OreMode.isEnabled)
            result.Insert(index++, MapTypes.Ore);
        
        if (ConfigManager.ConfigInstance.TemporalStabilityMode.isEnabled)
            result.Insert(index++, MapTypes.TemporalStability);
    }
}