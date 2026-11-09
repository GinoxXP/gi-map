using System.Reflection.Emit;
using GiMap.Client.Lod;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(GuiElementMap), "ZoomAdd")]
class GuiElementMapPatch
{
    private const float VanillaMinZoom = 0.25f;
    private const float AbsoluteMinZoom = 0.01f;

    // The wheel step is a linear 0.25, which would hit zero below the vanilla minimum, so the zoom is halved/doubled there instead
    static void Prefix(GuiElementMap __instance, ref float zoomDiff)
    {
        var zoom = __instance.ZoomLevel;

        if (zoomDiff < 0 && zoom <= VanillaMinZoom)
            zoomDiff = -zoom / 2;
        else if (zoomDiff > 0 && zoom < VanillaMinZoom)
            zoomDiff = Math.Min(zoom, VanillaMinZoom - zoom);
    }

    // Replaces the hardcoded lower zoom limit with the configured one
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var getMinZoom = AccessTools.Method(typeof(GuiElementMapPatch), nameof(GetMinZoom));

        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float value && value == VanillaMinZoom)
                yield return new CodeInstruction(OpCodes.Call, getMinZoom).MoveLabelsFrom(instruction);
            else
                yield return instruction;
        }
    }

    static float GetMinZoom()
    {
        var minZoom = ConfigManager.ConfigInstance?.minZoom ?? VanillaMinZoom;
        return Math.Clamp(minZoom, AbsoluteMinZoom, VanillaMinZoom);
    }
}

// Remembers the created map so layers know the zoom when picking a LOD level, until the dialog is opened
[HarmonyPatch(typeof(GuiElementMap), MethodType.Constructor,
    typeof(List<MapLayer>), typeof(ICoreClientAPI), typeof(GuiDialogWorldMap), typeof(ElementBounds), typeof(bool))]
class GuiElementMapConstructorPatch
{
    static void Postfix(GuiElementMap __instance)
        => LodState.Map = __instance;
}

// The minimap element is created once and reused, the full-screen one on every opening,
// so the map is taken from the opened dialog, otherwise the zoom of a closed full-screen map would stay in effect
[HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.OnGuiOpened))]
class GuiDialogWorldMapOpenedPatch
{
    static void Postfix(GuiDialogWorldMap __instance)
    {
        if (__instance.SingleComposer?.GetElement("mapElem") is GuiElementMap map)
            LodState.Map = map;
    }
}
