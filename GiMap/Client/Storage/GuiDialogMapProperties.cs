using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace GiMap.Client.Storage;

/// <summary>
/// "Map properties" dialog opened from the title bar menu of the world map: sizes of the map files of the world
/// and buttons that delete them
/// </summary>
public class GuiDialogMapProperties : GuiDialog
{
    private const double NameWidth = 220;
    private const double SizeWidth = 100;
    private const double ButtonWidth = 110;
    private const double Gap = 10;
    private const double RowHeight = 30;
    private const double Width = NameWidth + SizeWidth + ButtonWidth + Gap * 2;
    // Clearing a db that is open happens on the map generation thread, sizes are read again after it
    private const int RescanDelayMs = 1000;

    private static GuiDialogMapProperties? _instance;

    private List<MapStorageEntry> _entries = new();
    private string? _status;

    public override string? ToggleKeyCombinationCode => null;

    // Above the full-screen world map, below confirm dialogs
    public override double DrawOrder => 0.2;

    private GuiDialogMapProperties(ICoreClientAPI capi) : base(capi)
    {
        Refresh();
    }

    public static void Open(ICoreClientAPI capi)
    {
        if (_instance != null && _instance.IsOpened())
        {
            _instance.Refresh();
            return;
        }

        _instance = new GuiDialogMapProperties(capi);
        _instance.TryOpen();
    }

    private void Refresh()
    {
        _entries = MapStorage.Scan(capi);
        Compose();
    }

    private void Compose()
    {
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

        var composer = capi.Gui
            .CreateCompo("gimap-mapproperties", dialogBounds)
            .AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(Lang.Get("worldmap-mapproperties"), () => TryClose())
            .BeginChildElements(bgBounds);

        double y = GuiStyle.TitleBarHeight + 5;

        var total = _entries.Sum(entry => entry.Size);
        composer.AddStaticText(Lang.Get("gimap-mapproperties-total", MapStorage.FormatSize(total)),
            CairoFont.WhiteSmallishText(), ElementBounds.Fixed(0, y, Width, 28));
        y += RowHeight + 5;

        if (_entries.Count == 0)
        {
            composer.AddStaticText(Lang.Get("gimap-mapproperties-empty"), CairoFont.WhiteSmallText(),
                ElementBounds.Fixed(0, y, Width, 25));
            y += RowHeight;
        }

        var sizeFont = CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Right);
        foreach (var entry in _entries)
        {
            composer.AddStaticText(entry.Name, CairoFont.WhiteSmallText(), ElementBounds.Fixed(0, y + 3, NameWidth, 25));
            composer.AddStaticText(MapStorage.FormatSize(entry.Size), sizeFont,
                ElementBounds.Fixed(NameWidth + Gap, y + 3, SizeWidth, 25));
            composer.AddSmallButton(Lang.Get("gimap-mapproperties-delete"), () => OnDeleteClicked(entry),
                ElementBounds.Fixed(NameWidth + SizeWidth + Gap * 2, y, ButtonWidth, 24), EnumButtonStyle.Small);
            y += RowHeight;
        }

        if (_status != null)
        {
            composer.AddStaticText(_status, CairoFont.WhiteSmallText().WithColor(GuiStyle.ErrorTextColor),
                ElementBounds.Fixed(0, y + 5, Width, 25));
            y += RowHeight;
        }

        y += 10;
        if (_entries.Count > 0)
        {
            composer.AddSmallButton(Lang.Get("gimap-mapproperties-deleteall"), OnDeleteAllClicked,
                ElementBounds.Fixed(0, y, 160, 26), EnumButtonStyle.Small);
        }

        composer.AddSmallButton(Lang.Get("gimap-mapproperties-close"), () => TryClose(),
            ElementBounds.Fixed(Width - 110, y, 110, 26), EnumButtonStyle.Small);

        SingleComposer = composer.EndChildElements().Compose();
    }

    private bool OnDeleteClicked(MapStorageEntry entry)
    {
        var text = entry.IsTerrain
            ? Lang.Get("gimap-mapproperties-confirm-terrain")
            : Lang.Get("gimap-mapproperties-confirm", entry.Name);

        Confirm(text, () => Delete([entry]));
        return true;
    }

    private bool OnDeleteAllClicked()
    {
        Confirm(Lang.Get("gimap-mapproperties-confirm-all"), () => Delete(_entries));
        return true;
    }

    private void Confirm(string text, Action onConfirmed)
    {
        new GuiDialogConfirm(capi, text, confirmed =>
        {
            if (confirmed)
                onConfirmed();
        }).TryOpen();
    }

    private void Delete(IEnumerable<MapStorageEntry> entries)
    {
        var success = true;
        foreach (var entry in entries.ToList())
            success &= MapStorage.Clear(capi, entry);

        _status = success ? null : Lang.Get("gimap-mapproperties-error");
        Refresh();

        capi.Event.RegisterCallback(_ =>
        {
            if (IsOpened())
                Refresh();
        }, RescanDelayMs);
    }
}
