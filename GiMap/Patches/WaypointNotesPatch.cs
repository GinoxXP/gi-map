using GiMap.Client.Notes;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.GameContent;

namespace GiMap.Patches;

// Waypoints that store notes are drawn and edited by NotesMapLayer, so their vanilla icon, hover text and
// "Modify waypoint" dialog are skipped
[HarmonyPatch(typeof(WaypointMapComponent))]
class WaypointNotesPatch
{
    private static readonly AccessTools.FieldRef<WaypointMapComponent, Waypoint> WaypointRef =
        AccessTools.FieldRefAccess<WaypointMapComponent, Waypoint>("waypoint");

    private static bool IsNote(WaypointMapComponent component)
        => ConfigManager.ConfigInstance?.NotesMode.isEnabled == true && MapNoteCodec.IsNote(WaypointRef(component));

    [HarmonyPrefix]
    [HarmonyPatch(nameof(WaypointMapComponent.Render))]
    static bool Render(WaypointMapComponent __instance) => !IsNote(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(WaypointMapComponent.OnMouseMove))]
    static bool OnMouseMove(WaypointMapComponent __instance) => !IsNote(__instance);

    [HarmonyPrefix]
    [HarmonyPatch(nameof(WaypointMapComponent.OnMouseUpOnElement))]
    static bool OnMouseUpOnElement(WaypointMapComponent __instance) => !IsNote(__instance);
}
