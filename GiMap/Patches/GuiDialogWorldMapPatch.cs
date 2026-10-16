using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(GuiDialogWorldMap), "ComposeDialog")]
class GuiDialogWorldMapPatch
{
    // Vertical tabs are drawn 200 units to the left of the centered dialog, so the same space is reserved on both sides
    private const double TabsWidth = 200;
    // Title bar (28) + background padding (2 * 3) + layer list column
    private const double HorizontalExtra = 20;
    private const double VerticalExtra = 40;
    private const int MinSize = 200;
    // Leaves room for labels around the map
    private const double SizeFactor = 0.9;

    // Size the dialog had before the big map overwrote it, restored when the big map is turned off
    private static readonly ConditionalWeakTable<GuiDialogWorldMap, Tuple<int, int>> OriginalSizes = new();

    static void Prefix(GuiDialogWorldMap __instance, EnumDialogType dlgType, ICoreClientAPI ___capi, ref int ___mapWidth, ref int ___mapHeight)
    {
        if (dlgType != EnumDialogType.Dialog)
            return;

        if (!BigWorldMap.IsEnabled(___capi))
        {
            if (OriginalSizes.TryGetValue(__instance, out var originalSize))
            {
                (___mapWidth, ___mapHeight) = originalSize;
                OriginalSizes.Remove(__instance);
            }
            return;
        }

        if (!OriginalSizes.TryGetValue(__instance, out _))
            OriginalSizes.Add(__instance, Tuple.Create(___mapWidth, ___mapHeight));

        var screenPadding = GuiStyle.DialogToScreenPadding;
        var frameWidth = ___capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        var frameHeight = ___capi.Render.FrameHeight / RuntimeEnv.GUIScale;

        ___mapWidth = Math.Max(MinSize, (int)((frameWidth - 2 * (TabsWidth + screenPadding) - HorizontalExtra) * SizeFactor));
        ___mapHeight = Math.Max(MinSize, (int)((frameHeight - 2 * screenPadding - VerticalExtra) * SizeFactor));
    }
}
