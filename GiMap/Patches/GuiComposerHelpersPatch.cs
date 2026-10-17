using HarmonyLib;
using Vintagestory.API.Client;

namespace GiMap.Patches;

// Widens the mode tabs of the full-screen map to the left so that inactive tab names are not cut
[HarmonyPatch(typeof(GuiComposerHelpers), nameof(GuiComposerHelpers.AddVerticalToggleTabs))]
class GuiComposerHelpersPatch
{
    static void Prefix(GuiTab[] tabs, ElementBounds bounds, string key)
    {
        if (!GuiDialogWorldMapPatch.IsComposing || key != "verticalTabs")
            return;

        var width = GuiDialogWorldMapPatch.GetTabsWidth(tabs.Select(tab => tab.Name));
        if (width <= bounds.fixedWidth)
            return;

        bounds.fixedX -= width - bounds.fixedWidth;
        bounds.fixedWidth = width;
    }
}
