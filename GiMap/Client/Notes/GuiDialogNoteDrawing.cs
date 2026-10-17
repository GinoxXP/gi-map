using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GiMap.Client.Notes;

/// <summary>
/// Panel at the top of the screen while the points of a note are placed on the map
/// </summary>
public class GuiDialogNoteDrawing : GuiDialog
{
    private const double Width = 420;
    private const double ButtonWidth = 130;
    private const double ButtonHeight = 26;
    private const double Gap = 10;

    private readonly NoteDrawingSession _session;

    public override string? ToggleKeyCombinationCode => null;

    // Above the full-screen world map, like the context menu
    public override double DrawOrder => 0.2;

    public override bool PrefersUngrabbedMouse => true;

    public GuiDialogNoteDrawing(ICoreClientAPI capi, NoteDrawingSession session) : base(capi)
    {
        _session = session;
        Compose();
    }

    private void Compose()
    {
        var hintBounds = ElementBounds.Fixed(0, 0, Width, 40);
        var buttonBounds = ElementBounds.Fixed(0, 48, ButtonWidth, ButtonHeight);

        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.CenterTop)
            .WithFixedAlignmentOffset(0, GuiStyle.DialogToScreenPadding);

        SingleComposer = capi.Gui
            .CreateCompo("gimap-notedrawing", dialogBounds)
            .AddShadedDialogBG(bgBounds, false)
            .BeginChildElements(bgBounds)
            .AddDynamicText("", CairoFont.WhiteSmallText(), hintBounds, "hint")
            .AddSmallButton(Lang.Get("gimap-note-drawing-done"), OnDone, buttonBounds, EnumButtonStyle.Normal, "done")
            .AddSmallButton(Lang.Get("gimap-note-drawing-undo"), OnUndo, buttonBounds.RightCopy(Gap), EnumButtonStyle.Normal, "undo")
            .AddSmallButton(Lang.Get("gimap-note-drawing-cancel"), OnCancel, buttonBounds.RightCopy(2 * Gap + ButtonWidth))
            .EndChildElements()
            .Compose();

        UpdateState();
    }

    public void UpdateState()
    {
        if (SingleComposer == null)
            return;

        var hintKey = _session.Note.Type switch
        {
            MapNoteType.Line => "gimap-note-drawing-hint-line",
            MapNoteType.Rect => "gimap-note-drawing-hint-rect",
            MapNoteType.Circle => "gimap-note-drawing-hint-circle",
            _ => "gimap-note-drawing-hint-poly",
        };
        SingleComposer.GetDynamicText("hint").SetNewText(Lang.Get(hintKey, _session.PointCount));
        SingleComposer.GetButton("undo").Enabled = _session.PointCount > 0;
    }

    private bool OnDone()
    {
        _session.Finish();
        return true;
    }

    private bool OnUndo()
    {
        _session.Undo();
        return true;
    }

    private bool OnCancel()
    {
        _session.Cancel();
        return true;
    }

    public override void OnRenderGUI(float deltaTime)
    {
        if (capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg is not { } mapDlg
            || !mapDlg.IsOpened() || mapDlg.DialogType != EnumDialogType.Dialog)
        {
            _session.Cancel();
            return;
        }

        base.OnRenderGUI(deltaTime);
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();

        // Closed by Escape or with the map
        _session.Cancel();

        var worldMapDlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
        if (worldMapDlg != null && worldMapDlg.IsOpened())
            capi.Gui.RequestFocus(worldMapDlg);
    }
}
