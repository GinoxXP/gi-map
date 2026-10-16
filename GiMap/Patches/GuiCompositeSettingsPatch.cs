using System.Runtime.CompilerServices;
using GiMap.Config;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace GiMap.Patches;

// Adds the "GiMap" tab to the in-game settings, after "Interface" (or "Developer")
[HarmonyPatch(typeof(GuiCompositeSettings))]
class GuiCompositeSettingsPatch
{
    public const string TabCode = "gimap";
    public const string HeaderLangKey = "gimap-settings-header";

    private static readonly ConditionalWeakTable<GuiCompositeSettings, ElementBounds> ButtonBounds = new();

    private static readonly AccessTools.FieldRef<GuiCompositeSettings, ElementBounds> GButtonBoundsRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, ElementBounds>("gButtonBounds");

    private static readonly AccessTools.FieldRef<GuiCompositeSettings, ElementBounds> IButtonBoundsRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, ElementBounds>("iButtonBounds");

    private static readonly AccessTools.FieldRef<GuiCompositeSettings, ElementBounds> DButtonBoundsRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, ElementBounds>("dButtonBounds");

    private static readonly AccessTools.FieldRef<GuiCompositeSettings, ElementBounds> BackButtonBoundsRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, ElementBounds>("backButtonBounds");

    private static ElementBounds GetButtonBounds(GuiCompositeSettings settings)
        => ButtonBounds.GetValue(settings, _ => ElementBounds.Fixed(0, 0, 0, 40).WithFixedPadding(0, 3));

    // The back button is placed after the tab button; the in-game dialog width follows the back button
    [HarmonyPostfix]
    [HarmonyPatch("updateButtonBounds")]
    static void UpdateButtonBoundsPostfix(GuiCompositeSettings __instance)
    {
        if (ConfigManager.ConfigInstance == null)
            return;

        var bounds = GetButtonBounds(__instance);
        var width = CairoFont.ButtonText().GetTextExtents(Lang.Get(HeaderLangKey)).Width / ClientSettings.GUIScale + 15;
        var previous = ClientSettings.DeveloperMode ? DButtonBoundsRef(__instance) : IButtonBoundsRef(__instance);

        bounds.WithFixedWidth(width).FixedRightOf(previous, 15);
        BackButtonBoundsRef(__instance).FixedRightOf(bounds, 25);
    }

    [HarmonyPostfix]
    [HarmonyPatch("ComposerHeader")]
    static void ComposerHeaderPostfix(GuiCompositeSettings __instance, string currentTab, GuiComposer __result)
    {
        if (ConfigManager.ConfigInstance == null)
            return;

        var bounds = GetButtonBounds(__instance);
        bounds.ParentBounds = GButtonBoundsRef(__instance).ParentBounds;

        __result.AddToggleButton(Lang.Get(HeaderLangKey), CairoFont.ButtonText(), _ => GiMapSettingsTab.Open(__instance), bounds, TabCode);
        __result.GetToggleButton(TabCode).SetValue(currentTab == TabCode);
    }
}
