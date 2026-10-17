using System.Runtime.CompilerServices;
using GiMap.Client;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GiMap.Patches;

[HarmonyPatch(typeof(GuiDialogWorldMap), "ComposeDialog")]
class GuiDialogWorldMapPatch
{
    // Vertical tabs are drawn to the left of the centered dialog (vanilla width 200), so the same space is reserved on both sides
    private const double MinTabsWidth = 200;
    // Tab padding (2 * 3) and a margin for glyph overhangs
    private const double TabTextExtra = 12;
    // Title bar (28) + background padding (2 * 3) + layer list column
    private const double HorizontalExtra = 20;
    private const double VerticalExtra = 40;
    private const int MinSize = 200;
    // Leaves room for labels around the map
    private const double SizeFactor = 0.9;

    // Size the dialog had before the big map overwrote it, restored when the big map is turned off
    private static readonly ConditionalWeakTable<GuiDialogWorldMap, Tuple<int, int>> OriginalSizes = new();

    // Set while the full-screen map composes its tabs, see GuiComposerHelpersPatch
    [ThreadStatic]
    public static bool IsComposing;

    // Width that fits the longest tab name: vanilla draws inactive tab names into a texture of the bounds width and cuts them
    public static double GetTabsWidth(IEnumerable<string> names)
    {
        // Same font as AddVerticalToggleTabs
        var font = CairoFont.WhiteDetailText().WithFontSize(17);
        var textWidth = names.Select(name => font.GetTextExtents(name).Width).DefaultIfEmpty(0).Max() / RuntimeEnv.GUIScale;
        return Math.Max(MinTabsWidth, Math.Ceiling(textWidth + TabTextExtra));
    }

    static void Prefix(GuiDialogWorldMap __instance, EnumDialogType dlgType, ICoreClientAPI ___capi, ref int ___mapWidth, ref int ___mapHeight)
    {
        if (dlgType != EnumDialogType.Dialog)
            return;

        IsComposing = true;

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

        var tabNames = ___capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers
            .Select(layer => layer.LayerGroupCode)
            .Where(code => code != MapTypes.Notes)
            .Distinct()
            .Select(code => Lang.Get("maplayer-" + code));
        var tabsWidth = GetTabsWidth(tabNames);

        var screenPadding = GuiStyle.DialogToScreenPadding;
        var frameWidth = ___capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        var frameHeight = ___capi.Render.FrameHeight / RuntimeEnv.GUIScale;

        ___mapWidth = Math.Max(MinSize, (int)((frameWidth - 2 * (tabsWidth + screenPadding) - HorizontalExtra) * SizeFactor));
        ___mapHeight = Math.Max(MinSize, (int)((frameHeight - 2 * screenPadding - VerticalExtra) * SizeFactor));
    }

    static void Finalizer() => IsComposing = false;
}
