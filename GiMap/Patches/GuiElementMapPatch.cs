using System.Reflection.Emit;
using GiMap.Config;
using HarmonyLib;
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
