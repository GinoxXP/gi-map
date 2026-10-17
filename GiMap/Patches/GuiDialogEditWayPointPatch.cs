using GiMap.Client.ContextMenu;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GiMap.Patches;

// Adds a "Teleport here" row under the buttons of the vanilla "Modify waypoint" dialog
[HarmonyPatch(typeof(GuiDialogEditWayPoint), "ComposeDialog")]
class GuiDialogEditWayPointPatch
{
    // Set while the dialog composes, see GuiComposerEndChildElementsPatch
    [ThreadStatic]
    public static GuiDialogEditWayPoint? Composing;

    static void Prefix(GuiDialogEditWayPoint __instance) => Composing = __instance;

    static void Finalizer() => Composing = null;
}

// The vanilla dialog is a single fluent chain, so the button is added right before it closes the background children:
// the background fits to its children and grows with the new row
[HarmonyPatch(typeof(GuiComposer), nameof(GuiComposer.EndChildElements))]
class GuiComposerEndChildElementsPatch
{
    // Composer name of GuiDialogEditWayPoint
    private const string EditWaypointDialogName = "worldmap-modwp";
    private const string SaveButtonKey = "saveButton";
    private const string TeleportButtonKey = "gimap-teleportButton";
    // Width of the vanilla button row
    private const double RowWidth = 360;
    private const double RowHeight = 25;
    private const double Gap = 10;

    static void Prefix(GuiComposer __instance)
    {
        var dialog = GuiDialogEditWayPointPatch.Composing;
        if (dialog == null || __instance.DialogName != EditWaypointDialogName)
            return;

        if (__instance.GetElement(TeleportButtonKey) != null || __instance.GetElement(SaveButtonKey) is not { } saveButton)
            return;

        var capi = __instance.Api;
        if (!capi.World.Player.HasPrivilege(Privilege.tp))
            return;

        var y = saveButton.Bounds.fixedY + saveButton.Bounds.fixedHeight + Gap;
        __instance.AddSmallButton(Lang.Get("worldmap-contextmenu-teleport"), () =>
        {
            MapTeleport.TeleportTo(capi, dialog.Waypoint.Position);
            dialog.TryClose();
            return true;
        }, ElementBounds.Fixed(0, y, RowWidth, RowHeight), EnumButtonStyle.Normal, TeleportButtonKey);
    }
}
