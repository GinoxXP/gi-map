using GiMap.Client.ContextMenu;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GiMap.Patches;

// Right click on the world map opens the context menu instead of the "Add waypoint" dialog.
// Vanilla first lets the layers handle the click (right click on a waypoint edits it) and only then creates the dialog,
// so the dialog is intercepted and opened later from the menu
[HarmonyPatch(typeof(GuiDialogWorldMap), nameof(GuiDialogWorldMap.OnMouseUp))]
class GuiDialogWorldMapMouseUpPatch
{
    // Set while vanilla handles a right click on the map
    [ThreadStatic]
    public static bool IsIntercepting;

    static void Prefix(MouseEvent args)
        => IsIntercepting = args.Button == EnumMouseButton.Right;

    static void Finalizer() => IsIntercepting = false;
}

[HarmonyPatch(typeof(GuiDialogAddWayPoint), nameof(GuiDialogAddWayPoint.TryOpen))]
class GuiDialogAddWayPointPatch
{
    static bool Prefix(GuiDialogAddWayPoint __instance, ICoreClientAPI ___capi, ref bool __result)
    {
        if (!GuiDialogWorldMapMouseUpPatch.IsIntercepting || __instance.WorldPos == null)
            return true;

        GuiDialogWorldMapMouseUpPatch.IsIntercepting = false;
        __result = false;

        var capi = ___capi;
        var worldPos = __instance.WorldPos.Clone();
        var mouseX = capi.Input.MouseX;
        var mouseY = capi.Input.MouseY;

        // Called from the mouse handler of the map dialog, so the menu is opened later
        capi.Event.EnqueueMainThreadTask(
            () => GuiDialogMapContextMenu.Open(capi, mouseX, mouseY, worldPos, __instance), "gimap-contextmenu");
        return false;
    }
}
