using Cairo;
using GiMap.Client;
using GiMap.Client.Bathymetric;
using GiMap.Client.Fertility;
using GiMap.Client.Topographic;
using GiMap.Patches;
using HarmonyLib;
using Newtonsoft.Json;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace GiMap.Config;

/// <summary>
/// Content of the "GiMap" tab of the in-game settings, see <see cref="GuiCompositeSettingsPatch"/>
/// </summary>
public static class GiMapSettingsTab
{
    private const double TopY = 85;
    private const double RowHeight = 32;
    private const double HeaderHeight = 36;
    private const double LabelWidth = 250;
    private const double SliderWidth = 150;
    private const double LeftColumnX = 0;
    private const double RightColumnX = 470;
    private const double ControlOffset = 260;

    // Layers are redrawn a moment after the last change, so dragging a slider does not purge the map on every step
    private const int ReloadDelayMs = 500;

    private static readonly string[] MinZoomValues = ["0.25", "0.125", "0.0625", "0.03125"];
    private static readonly string[] MinZoomNames = ["1/4", "1/8", "1/16", "1/32"];

    private static readonly Func<GuiCompositeSettings, string, string, GuiComposer> ComposerHeader =
        AccessTools.MethodDelegate<Func<GuiCompositeSettings, string, string, GuiComposer>>(
            AccessTools.Method(typeof(GuiCompositeSettings), "ComposerHeader"));

    private static readonly AccessTools.FieldRef<GuiCompositeSettings, IGameSettingsHandler> HandlerRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, IGameSettingsHandler>("handler");

    private static readonly AccessTools.FieldRef<GuiCompositeSettings, GuiComposer> ComposerRef =
        AccessTools.FieldRefAccess<GuiCompositeSettings, GuiComposer>("composer");

    private record ModeSwitch(string MapType, Func<GiMapConfig, bool> Get, Action<GiMapConfig, bool> Set);

    // Same order as the map tabs
    private static readonly ModeSwitch[] Modes =
    [
        new(MapTypes.Topographic, c => c.TopographicMode.isEnabled, (c, v) => c.TopographicMode.isEnabled = v),
        new(MapTypes.Height, c => c.HeightMode.isEnabled, (c, v) => c.HeightMode.isEnabled = v),
        new(MapTypes.Bathymetric, c => c.BathymetricMode.isEnabled, (c, v) => c.BathymetricMode.isEnabled = v),
        new(MapTypes.Caves, c => c.CavesMode.isEnabled, (c, v) => c.CavesMode.isEnabled = v),
        new(MapTypes.Fertility, c => c.FertilityMode.isEnabled, (c, v) => c.FertilityMode.isEnabled = v),
        new(MapTypes.Precipitation, c => c.PrecipitationMode.isEnabled, (c, v) => c.PrecipitationMode.isEnabled = v),
        new(MapTypes.Temperature, c => c.TemperatureMode.isEnabled, (c, v) => c.TemperatureMode.isEnabled = v),
        new(MapTypes.GeologyActivity, c => c.GeologyActivityMode.isEnabled, (c, v) => c.GeologyActivityMode.isEnabled = v),
        new(MapTypes.Light, c => c.LightMode.isEnabled, (c, v) => c.LightMode.isEnabled = v),
        new(MapTypes.ChunkGrid, c => c.ChunkGridMode.isEnabled, (c, v) => c.ChunkGridMode.isEnabled = v),
        new(MapTypes.Ore, c => c.OreMode.isEnabled, (c, v) => c.OreMode.isEnabled = v),
        new(MapTypes.TemporalStability, c => c.TemporalStabilityMode.isEnabled, (c, v) => c.TemporalStabilityMode.isEnabled = v),
        new(MapTypes.Butterflies, c => c.ButterfliesMode.isEnabled, (c, v) => c.ButterfliesMode.isEnabled = v),
        new(MapTypes.Notes, c => c.NotesMode.isEnabled, (c, v) => c.NotesMode.isEnabled = v),
    ];

    private static bool _reloadTopographic;
    private static bool _reloadFertility;
    private static bool _reloadBathymetric;
    private static long _reloadCallbackId;

    private static GiMapConfig Config => ConfigManager.ConfigInstance;

    /// <summary>
    /// Map types in the order of the map tabs
    /// </summary>
    public static IEnumerable<string> ModeOrder => Modes.Select(mode => mode.MapType);

    public static void Open(GuiCompositeSettings settings)
    {
        var composer = ComposerHeader(settings, "gamesettings-gimap", GuiCompositeSettingsPatch.TabCode);

        var left = new Column(LeftColumnX);
        left.Header(composer, "gimap-settings-general");
        left.Slider(composer, "gimap-settings-overlayalpha", "overlayAlpha", OnOverlayAlphaChanged);
        left.DropDown(composer, "gimap-settings-minzoom", "minZoom", MinZoomValues, MinZoomNames, GetMinZoomIndex(), OnMinZoomChanged);
        left.Header(composer, "gimap-settings-modes", "gimap-settings-modes-hover");
        foreach (var mode in Modes)
            left.Switch(composer, "maplayer-" + mode.MapType, "mode-" + mode.MapType, on => OnModeChanged(mode, on));
        left.Button(composer, "gimap-settings-reset", "resetSettings", () => OnResetClicked(settings));

        var right = new Column(RightColumnX);
        right.Header(composer, "gimap-settings-topographic");
        right.Switch(composer, "gimap-settings-showcontours", "showContours", OnShowContoursChanged);
        right.Slider(composer, "gimap-settings-contourinterval", "contourInterval", OnContourIntervalChanged);
        right.Slider(composer, "gimap-settings-majorcontourevery", "majorContourEvery", OnMajorContourEveryChanged);
        right.Switch(composer, "gimap-settings-showheightlabels", "showHeightLabels", OnShowHeightLabelsChanged);
        right.Slider(composer, "gimap-settings-contourlabelsminzoom", "contourLabelsMinZoom", OnContourLabelsMinZoomChanged);
        right.Slider(composer, "gimap-settings-peaklabelsminzoom", "peakLabelsMinZoom", OnPeakLabelsMinZoomChanged);
        right.Header(composer, "gimap-settings-bathymetric");
        right.Switch(composer, "gimap-settings-showisobaths", "bathyShowContours", OnBathyShowContoursChanged);
        right.Slider(composer, "gimap-settings-contourinterval", "bathyContourInterval", OnBathyContourIntervalChanged);
        right.Slider(composer, "gimap-settings-majorcontourevery", "bathyMajorContourEvery", OnBathyMajorContourEveryChanged);
        right.Switch(composer, "gimap-settings-showdepthlabels", "bathyShowDepthLabels", OnBathyShowDepthLabelsChanged);
        right.Slider(composer, "gimap-settings-contourlabelsminzoom", "bathyContourLabelsMinZoom", OnBathyContourLabelsMinZoomChanged);
        right.Slider(composer, "gimap-settings-deepestlabelsminzoom", "bathyDeepestLabelsMinZoom", OnBathyDeepestLabelsMinZoomChanged);
        right.Header(composer, "gimap-settings-fertility");
        right.Switch(composer, "gimap-settings-hatching", "hatching", OnHatchingChanged);
        right.Switch(composer, "gimap-settings-ignoresnow", "ignoreSnow", OnIgnoreSnowChanged);

        composer.EndChildElements().Compose();

        composer.GetSlider("overlayAlpha").SetValues((int)Math.Round(Config.overlayAlpha * 100), 0, 100, 1, "%");
        foreach (var mode in Modes)
            composer.GetSwitch("mode-" + mode.MapType).SetValue(mode.Get(Config));

        var topographic = Config.TopographicMode;
        composer.GetSwitch("showContours").SetValue(topographic.showContours);
        composer.GetSlider("contourInterval").SetValues(Math.Clamp(topographic.contourInterval, 1, 20), 1, 20, 1);
        composer.GetSlider("majorContourEvery").SetValues(Math.Clamp(topographic.majorContourEvery, 2, 10), 2, 10, 1);
        composer.GetSwitch("showHeightLabels").SetValue(topographic.showHeightLabels);
        SetZoomSlider(composer.GetSlider("contourLabelsMinZoom"), topographic.contourLabelsMinZoom);
        SetZoomSlider(composer.GetSlider("peakLabelsMinZoom"), topographic.peakLabelsMinZoom);

        var bathymetric = Config.BathymetricMode;
        composer.GetSwitch("bathyShowContours").SetValue(bathymetric.showContours);
        composer.GetSlider("bathyContourInterval").SetValues(Math.Clamp(bathymetric.contourInterval, 1, 10), 1, 10, 1);
        composer.GetSlider("bathyMajorContourEvery").SetValues(Math.Clamp(bathymetric.majorContourEvery, 2, 10), 2, 10, 1);
        composer.GetSwitch("bathyShowDepthLabels").SetValue(bathymetric.showDepthLabels);
        SetZoomSlider(composer.GetSlider("bathyContourLabelsMinZoom"), bathymetric.contourLabelsMinZoom);
        SetZoomSlider(composer.GetSlider("bathyDeepestLabelsMinZoom"), bathymetric.deepestLabelsMinZoom);

        composer.GetSwitch("hatching").SetValue(Config.FertilityMode.hatchingEnabled);
        composer.GetSwitch("ignoreSnow").SetValue(Config.FertilityMode.ignoreSnow);

        ComposerRef(settings) = composer;
        HandlerRef(settings).LoadComposer(composer);
    }

    // Label zoom is set in quarters: 1..16 is x0.25..x4
    private static void SetZoomSlider(GuiElementSlider slider, float zoom)
    {
        slider.OnSliderTooltip = value => "x" + (value / 4f).ToString("0.##");
        slider.SetValues(Math.Clamp((int)Math.Round(zoom * 4), 1, 16), 1, 16, 1);
    }

    private static int GetMinZoomIndex()
    {
        // The closest preset, the config may hold any value
        var index = 0;
        for (var i = 1; i < MinZoomValues.Length; i++)
        {
            if (Math.Abs(float.Parse(MinZoomValues[i], System.Globalization.CultureInfo.InvariantCulture) - Config.minZoom)
                < Math.Abs(float.Parse(MinZoomValues[index], System.Globalization.CultureInfo.InvariantCulture) - Config.minZoom))
                index = i;
        }

        return index;
    }

    private static bool OnOverlayAlphaChanged(int value)
    {
        Config.overlayAlpha = value / 100f;
        AMapLayer.ApplyOverlayAlphaToAll();
        Save();
        return true;
    }

    private static void OnMinZoomChanged(string value, bool selected)
    {
        Config.minZoom = float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        Save();
    }

    // Layers are registered on world load, so the switch applies after rejoining
    private static void OnModeChanged(ModeSwitch mode, bool on)
    {
        mode.Set(Config, on);
        Save();
    }

    private static bool OnResetClicked(GuiCompositeSettings settings)
    {
        var capi = GiMapModSystem.ClientApi;
        if (capi == null)
            return true;

        new GuiDialogConfirm(capi, Lang.Get("gimap-settings-reset-confirm"), confirmed =>
        {
            if (confirmed)
                ResetSettings(settings);
        }).TryOpen();

        return true;
    }

    private static void ResetSettings(GuiCompositeSettings settings)
    {
        var capi = GiMapModSystem.ClientApi;
        if (capi == null || ConfigManager.ConfigInstance == null)
            return;

        var old = Config;
        ConfigManager.ResetModConfig(capi);

        AMapLayer.ApplyOverlayAlphaToAll();

        // Only layers whose baked settings changed are redrawn
        _reloadTopographic |= !SameSettings(old.TopographicMode, Config.TopographicMode);
        _reloadBathymetric |= !SameSettings(old.BathymetricMode, Config.BathymetricMode);
        _reloadFertility |= !SameSettings(old.FertilityMode, Config.FertilityMode);
        if (_reloadTopographic || _reloadBathymetric || _reloadFertility)
            ScheduleReload();

        Open(settings);
    }

    // Sections hold arrays, so they are compared by their json
    private static bool SameSettings<T>(T a, T b)
        => JsonConvert.SerializeObject(a) == JsonConvert.SerializeObject(b);

    private static void OnShowContoursChanged(bool on)
    {
        Config.TopographicMode.showContours = on;
        ReloadTopographic();
    }

    private static bool OnContourIntervalChanged(int value)
    {
        if (Config.TopographicMode.contourInterval != value)
        {
            Config.TopographicMode.contourInterval = value;
            ReloadTopographic();
        }

        return true;
    }

    private static bool OnMajorContourEveryChanged(int value)
    {
        if (Config.TopographicMode.majorContourEvery != value)
        {
            Config.TopographicMode.majorContourEvery = value;
            ReloadTopographic();
        }

        return true;
    }

    private static void OnShowHeightLabelsChanged(bool on)
    {
        Config.TopographicMode.showHeightLabels = on;
        ReloadTopographic();
    }

    // Label zoom is checked on render, no redraw is needed
    private static bool OnContourLabelsMinZoomChanged(int value)
    {
        Config.TopographicMode.contourLabelsMinZoom = value / 4f;
        Save();
        return true;
    }

    private static bool OnPeakLabelsMinZoomChanged(int value)
    {
        Config.TopographicMode.peakLabelsMinZoom = value / 4f;
        Save();
        return true;
    }

    private static void OnBathyShowContoursChanged(bool on)
    {
        Config.BathymetricMode.showContours = on;
        ReloadBathymetric();
    }

    private static bool OnBathyContourIntervalChanged(int value)
    {
        if (Config.BathymetricMode.contourInterval != value)
        {
            Config.BathymetricMode.contourInterval = value;
            ReloadBathymetric();
        }

        return true;
    }

    private static bool OnBathyMajorContourEveryChanged(int value)
    {
        if (Config.BathymetricMode.majorContourEvery != value)
        {
            Config.BathymetricMode.majorContourEvery = value;
            ReloadBathymetric();
        }

        return true;
    }

    private static void OnBathyShowDepthLabelsChanged(bool on)
    {
        Config.BathymetricMode.showDepthLabels = on;
        ReloadBathymetric();
    }

    private static bool OnBathyContourLabelsMinZoomChanged(int value)
    {
        Config.BathymetricMode.contourLabelsMinZoom = value / 4f;
        Save();
        return true;
    }

    private static bool OnBathyDeepestLabelsMinZoomChanged(int value)
    {
        Config.BathymetricMode.deepestLabelsMinZoom = value / 4f;
        Save();
        return true;
    }

    private static void OnHatchingChanged(bool on)
    {
        Config.FertilityMode.hatchingEnabled = on;
        ReloadFertility();
    }

    private static void OnIgnoreSnowChanged(bool on)
    {
        Config.FertilityMode.ignoreSnow = on;
        ReloadFertility();
    }

    private static void ReloadTopographic()
    {
        _reloadTopographic = true;
        Save();
        ScheduleReload();
    }

    private static void ReloadBathymetric()
    {
        _reloadBathymetric = true;
        Save();
        ScheduleReload();
    }

    private static void ReloadFertility()
    {
        _reloadFertility = true;
        Save();
        ScheduleReload();
    }

    private static void Save()
    {
        var capi = GiMapModSystem.ClientApi;
        if (capi != null)
            ConfigManager.SaveModConfig(capi);
    }

    private static void ScheduleReload()
    {
        var capi = GiMapModSystem.ClientApi;
        if (capi == null)
            return;

        capi.Event.UnregisterCallback(_reloadCallbackId);
        // The settings are opened from the pause menu, the callback has to run while the game is paused
        _reloadCallbackId = capi.Event.RegisterCallback(_ => ReloadLayers(), ReloadDelayMs, true);
    }

    private static void ReloadLayers()
    {
        if (ConfigManager.ConfigInstance == null)
            return;

        if (_reloadTopographic)
            AMapLayer.Find<TopographicMapLayer>()?.Reload();

        if (_reloadBathymetric)
            AMapLayer.Find<BathymetricMapLayer>()?.Reload();

        if (_reloadFertility)
            AMapLayer.Find<FertilityMapLayer>()?.Reload();

        _reloadTopographic = false;
        _reloadBathymetric = false;
        _reloadFertility = false;
    }

    /// <summary>
    /// Column of rows: a label on the left, the control on the right
    /// </summary>
    private class Column(double x)
    {
        private double _y = TopY;

        public void Header(GuiComposer composer, string langKey, string? hoverLangKey = null)
        {
            var font = CairoFont.WhiteSmallishText().Clone().WithWeight(FontWeight.Bold);
            font.Color[3] = 0.6;

            var bounds = ElementBounds.Fixed(x, _y + 6, ControlOffset + SliderWidth, 30);
            composer.AddStaticText(Lang.Get(langKey), font, bounds);
            if (hoverLangKey != null)
                composer.AddHoverText(Lang.Get(hoverLangKey), CairoFont.WhiteSmallText(), 250, bounds.FlatCopy());

            _y += HeaderHeight;
        }

        public void Switch(GuiComposer composer, string langKey, string key, Action<bool> onToggle)
        {
            Label(composer, langKey);
            composer.AddSwitch(onToggle, ElementBounds.Fixed(x + ControlOffset, _y, 30, 30), key, 26);
            _y += RowHeight;
        }

        public void Slider(GuiComposer composer, string langKey, string key, Vintagestory.API.Common.ActionConsumable<int> onNewValue)
        {
            Label(composer, langKey);
            composer.AddSlider(onNewValue, ElementBounds.Fixed(x + ControlOffset, _y + 5, SliderWidth, 20), key);
            _y += RowHeight;
        }

        public void DropDown(GuiComposer composer, string langKey, string key, string[] values, string[] names, int selectedIndex, SelectionChangedDelegate onChanged)
        {
            Label(composer, langKey);
            composer.AddDropDown(values, names, selectedIndex, onChanged, ElementBounds.Fixed(x + ControlOffset, _y + 2, SliderWidth, 26), key);
            _y += RowHeight;
        }

        public void Button(GuiComposer composer, string langKey, string key, Vintagestory.API.Common.ActionConsumable onClick)
        {
            _y += 8;
            composer.AddSmallButton(Lang.Get(langKey), onClick, ElementBounds.Fixed(x, _y, ControlOffset + SliderWidth, 26), EnumButtonStyle.Small, key);
            _y += RowHeight;
        }

        private void Label(GuiComposer composer, string langKey)
        {
            var bounds = ElementBounds.Fixed(x, _y + 4, LabelWidth, 26);
            composer.AddStaticText(Lang.Get(langKey), CairoFont.WhiteSmallText(), bounds);

            var hoverKey = langKey + "-hover";
            if (Lang.HasTranslation(hoverKey, false, false))
                composer.AddHoverText(Lang.Get(hoverKey), CairoFont.WhiteSmallText(), 250, bounds.FlatCopy());
        }
    }
}
