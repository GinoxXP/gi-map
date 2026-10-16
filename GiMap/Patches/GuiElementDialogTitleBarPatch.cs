using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GiMap.Patches;

// Adds a big map toggle to the title bar menu of the full-screen world map
[HarmonyPatch(typeof(GuiElementDialogTitleBar), MethodType.Constructor,
    [typeof(ICoreClientAPI), typeof(string), typeof(GuiComposer), typeof(Action), typeof(CairoFont), typeof(ElementBounds)])]
class GuiElementDialogTitleBarPatch
{
    // Composer name of GuiDialogWorldMap in EnumDialogType.Dialog mode
    private const string WorldMapDialogName = "worldmapDialog";
    private const string BigMapValue = "gimap-bigmap";

    private static readonly AccessTools.FieldRef<GuiElementDialogTitleBar, GuiElementListMenu> ListMenuRef =
        AccessTools.FieldRefAccess<GuiElementDialogTitleBar, GuiElementListMenu>("listMenu");

    private static readonly AccessTools.FieldRef<GuiElementListMenu, SelectionChangedDelegate> OnSelectionChangedRef =
        AccessTools.FieldRefAccess<GuiElementListMenu, SelectionChangedDelegate>("onSelectionChanged");

    static void Postfix(GuiElementDialogTitleBar __instance, ICoreClientAPI capi, GuiComposer composer)
    {
        if (composer?.DialogName != WorldMapDialogName)
            return;

        var listMenu = ListMenuRef(__instance);

        // Set directly instead of SetList: it composes immediately, but the bounds have no parent yet
        listMenu.Values = [..listMenu.Values, BigMapValue];
        listMenu.Names = [..listMenu.Names, Lang.Get(BigWorldMap.IsEnabled(capi) ? "worldmap-bigmap-disable" : "worldmap-bigmap-enable")];

        var original = OnSelectionChangedRef(listMenu);
        OnSelectionChangedRef(listMenu) = (value, selected) =>
        {
            if (value != BigMapValue)
            {
                original?.Invoke(value, selected);
                return;
            }

            // Keep the fixed/movable item selected
            listMenu.SetSelectedIndex(__instance.Movable ? 1 : 0);
            BigWorldMap.Toggle(capi);

            // Called from the old composer's mouse handler, so the dialog is recomposed later
            capi.Event.EnqueueMainThreadTask(() =>
            {
                var worldMapDlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
                if (worldMapDlg != null && worldMapDlg.IsOpened() && worldMapDlg.DialogType == EnumDialogType.Dialog)
                    worldMapDlg.Open(EnumDialogType.Dialog);
            }, "gimap-bigmap");
        };
    }
}
