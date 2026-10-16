using GiMap.Config;
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

    static void Prefix(EnumDialogType dlgType, ICoreClientAPI ___capi, ref int ___mapWidth, ref int ___mapHeight)
    {
        if (!ConfigManager.ConfigInstance.bigWorldMap || dlgType != EnumDialogType.Dialog)
            return;

        var screenPadding = GuiStyle.DialogToScreenPadding;
        var frameWidth = ___capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        var frameHeight = ___capi.Render.FrameHeight / RuntimeEnv.GUIScale;

        ___mapWidth = Math.Max(MinSize, (int)((frameWidth - 2 * (TabsWidth + screenPadding) - HorizontalExtra) * SizeFactor));
        ___mapHeight = Math.Max(MinSize, (int)((frameHeight - 2 * screenPadding - VerticalExtra) * SizeFactor));
    }
}
