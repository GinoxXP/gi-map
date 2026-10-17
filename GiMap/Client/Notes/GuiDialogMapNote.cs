using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Notes;

/// <summary>
/// Creates or edits a note: text, color, options of its type and its geometry as numbers
/// </summary>
public class GuiDialogMapNote : GuiDialogGeneric
{
    private const double LabelWidth = 150;
    private const double ControlX = 160;
    private const double ControlWidth = 240;
    private const double RowHeight = 25;
    private const double RowGap = 8;
    private const double ButtonWidth = 92;
    private const int MaxSize = 100000;

    private static readonly NoteLineStyle[] Styles = Enum.GetValues<NoteLineStyle>();

    private static GuiDialogMapNote? _instance;

    private readonly MapNote _note;
    // The saved note, null for a new one
    private readonly MapNote? _original;
    private int[] _colors = [];
    private int _colorIndex;

    public override bool PrefersUngrabbedMouse => true;
    public override bool DisableMouseGrab => true;
    public override double DrawOrder => 0.2;

    private bool IsNew => _original?.Waypoint == null;
    private bool CanSave => IsNew || _original!.OwnIndex >= 0;

    private GuiDialogMapNote(ICoreClientAPI capi, MapNote note, MapNote? original) : base("", capi)
    {
        _note = note;
        _original = original;
    }

    public static void Open(ICoreClientAPI capi, MapNote note, MapNote? original)
    {
        _instance?.TryClose();
        _instance = new GuiDialogMapNote(capi, note, original);
        _instance.TryOpen();
    }

    /// <summary>
    /// Starts a new note of the type at the clicked point: text opens the editor, shapes start drawing
    /// </summary>
    public static void StartNew(ICoreClientAPI capi, MapNoteType type, Vec3d worldPos)
    {
        var anchor = new Vec2i((int)Math.Floor(worldPos.X), (int)Math.Floor(worldPos.Z));
        var note = new MapNote
        {
            Type = type,
            AnchorY = (int)Math.Floor(worldPos.Y),
            Color = GetColors(capi).FirstOrDefault(unchecked((int)0xffffffff)),
            ShowLength = true,
            ShowRadius = true,
            ShowDims = true,
            ShowPerimeter = true,
            ShowArea = true,
        };

        if (type == MapNoteType.Text)
        {
            note.Vertices.Add(anchor);
            Open(capi, note, null);
            return;
        }

        NoteDrawingSession.Start(capi, note, null, anchor);
    }

    public override bool TryOpen()
    {
        Compose();
        return base.TryOpen();
    }

    public override bool CaptureAllInputs() => IsOpened();

    public override void OnMouseDown(MouseEvent args)
    {
        base.OnMouseDown(args);
        args.Handled = true;
    }

    public override void OnMouseUp(MouseEvent args)
    {
        base.OnMouseUp(args);
        args.Handled = true;
    }

    public override void OnMouseMove(MouseEvent args)
    {
        base.OnMouseMove(args);
        args.Handled = true;
    }

    public override void OnMouseWheel(MouseWheelEventArgs args)
    {
        base.OnMouseWheel(args);
        args.SetHandled();
    }

    private static int[] GetColors(ICoreClientAPI capi)
        => capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<WaypointMapLayer>().FirstOrDefault()?.WaypointColors.ToArray()
           ?? [];

    private void Compose()
    {
        _colors = GetColors(capi);
        var colorIndex = Array.FindIndex(_colors, c => (c | unchecked((int)0xff000000)) == (_note.Color | unchecked((int)0xff000000)));
        if (colorIndex < 0)
        {
            _colors = _colors.Prepend(_note.Color).ToArray();
            colorIndex = 0;
        }

        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

        var typeName = Lang.Get("gimap-note-type-" + _note.Type.ToString().ToLowerInvariant());
        var title = Lang.Get(IsNew ? "gimap-note-add-title" : "gimap-note-edit-title", typeName);

        var composer = capi.Gui.CreateCompo("gimap-mapnote", dialogBounds)
            .AddShadedDialogBG(bgBounds, false)
            .AddDialogTitleBar(title, () => TryClose())
            .BeginChildElements(bgBounds);

        double y = 30;

        AddLabel(composer, "gimap-note-text", y);
        composer.AddTextInput(Control(y), null, CairoFont.TextInput(), "text");
        y += RowHeight + RowGap;

        AddLabel(composer, "waypoint-color", y);
        composer.AddColorListPicker(_colors, index => _colorIndex = index, ElementBounds.Fixed(ControlX, y, 22, 22), (int)ControlWidth, "colorPicker");
        // The picker wraps its swatches into rows
        var swatchesPerRow = Math.Max(1, (int)(ControlWidth / 27));
        y += (int)Math.Ceiling(_colors.Length / (double)swatchesPerRow) * 27 + RowGap;

        switch (_note.Type)
        {
            case MapNoteType.Text:
                AddLabel(composer, "gimap-note-fontsize", y);
                composer.AddSlider(_ => true, Control(y), "fontSize");
                y += RowHeight + RowGap;
                y = AddSwitchRow(composer, "gimap-note-bold", "bold", y);
                break;
            case MapNoteType.Line:
                y = AddNumberRow(composer, "gimap-note-linelength", "length", y);
                y = AddNumberRow(composer, "gimap-note-angle", "angle", y);
                AddLabel(composer, "gimap-note-style", y);
                composer.AddDropDown(Styles.Select(s => s.ToString()).ToArray(),
                    Styles.Select(s => Lang.Get("gimap-note-style-" + s.ToString().ToLowerInvariant())).ToArray(),
                    Array.IndexOf(Styles, _note.Style), (_, _) => { }, Control(y), "style");
                y += RowHeight + RowGap;
                y = AddSwitchRow(composer, "gimap-note-showlength", "showLength", y);
                break;
            case MapNoteType.Circle:
                y = AddNumberRow(composer, "gimap-note-circleradius", "radius", y);
                y = AddSwitchRow(composer, "gimap-note-showradius", "showRadius", y);
                y = AddSwitchRow(composer, "gimap-note-showarea", "showArea", y);
                y = AddSwitchRow(composer, "gimap-note-fill", "fill", y);
                break;
            case MapNoteType.Rect:
                y = AddNumberRow(composer, "gimap-note-width", "width", y);
                y = AddNumberRow(composer, "gimap-note-height", "height", y);
                y = AddSwitchRow(composer, "gimap-note-showdims", "showDims", y);
                y = AddSwitchRow(composer, "gimap-note-showarea", "showArea", y);
                y = AddSwitchRow(composer, "gimap-note-fill", "fill", y);
                break;
            case MapNoteType.Poly:
                AddLabel(composer, "gimap-note-vertices", y, "gimap-note-vertices-hover");
                composer.AddTextInput(Control(y), null, CairoFont.TextInput(), "vertices");
                y += RowHeight + RowGap;
                y = AddSwitchRow(composer, "gimap-note-closed", "closed", y);
                y = AddSwitchRow(composer, "gimap-note-showperimeter", "showPerimeter", y);
                y = AddSwitchRow(composer, "gimap-note-showarea", "showArea", y);
                y = AddSwitchRow(composer, "gimap-note-fill", "fill", y);
                break;
        }

        if (!CanSave)
        {
            composer.AddStaticText(Lang.Get("gimap-note-readonly"), CairoFont.WhiteDetailText(), ElementBounds.Fixed(0, y, ControlX + ControlWidth, RowHeight));
            y += RowHeight + RowGap;
        }

        // Buttons: Cancel on the left, Save on the right, Delete and Redraw between them
        var buttonBounds = ElementBounds.Fixed(0, y + 5, ButtonWidth, RowHeight);
        composer.AddSmallButton(Lang.Get("Cancel"), OnCancel, buttonBounds);
        if (!IsNew && CanSave)
            composer.AddSmallButton(Lang.Get("Delete"), OnDelete, buttonBounds.FlatCopy().WithFixedPosition(ButtonWidth + 10, buttonBounds.fixedY));
        if (_note.Type != MapNoteType.Text && CanSave)
            composer.AddSmallButton(Lang.Get("gimap-note-redraw"), OnRedraw, buttonBounds.FlatCopy().WithFixedPosition(2 * (ButtonWidth + 10), buttonBounds.fixedY));
        composer.AddSmallButton(Lang.Get("Save"), OnSave, buttonBounds.FlatCopy().WithFixedPosition(ControlX + ControlWidth - ButtonWidth, buttonBounds.fixedY),
            EnumButtonStyle.Normal, "saveButton");

        SingleComposer = composer.EndChildElements().Compose();
        SingleComposer.GetButton("saveButton").Enabled = CanSave;
        SingleComposer.ColorListPickerSetValue("colorPicker", colorIndex);
        _colorIndex = colorIndex;
        FillValues();
    }

    private static ElementBounds Control(double y) => ElementBounds.Fixed(ControlX, y, ControlWidth, RowHeight);

    private static void AddLabel(GuiComposer composer, string langKey, double y, string? hoverKey = null)
    {
        var bounds = ElementBounds.Fixed(0, y + 3, LabelWidth, RowHeight);
        composer.AddStaticText(Lang.Get(langKey), CairoFont.WhiteSmallText(), bounds);
        if (hoverKey != null)
            composer.AddHoverText(Lang.Get(hoverKey), CairoFont.WhiteSmallText(), 300, bounds.FlatCopy());
    }

    private static double AddNumberRow(GuiComposer composer, string langKey, string key, double y)
    {
        AddLabel(composer, langKey, y);
        composer.AddNumberInput(Control(y).WithFixedWidth(100), null, CairoFont.TextInput(), key);
        return y + RowHeight + RowGap;
    }

    private static double AddSwitchRow(GuiComposer composer, string langKey, string key, double y)
    {
        AddLabel(composer, langKey, y);
        composer.AddSwitch(_ => { }, ElementBounds.Fixed(ControlX, y, RowHeight, RowHeight), key);
        return y + RowHeight + RowGap;
    }

    private void FillValues()
    {
        var c = SingleComposer;
        c.GetTextInput("text").SetValue(_note.Text);

        switch (_note.Type)
        {
            case MapNoteType.Text:
                c.GetSlider("fontSize").SetValues(_note.FontSize, MapNote.MinFontSize, MapNote.MaxFontSize, 1);
                c.GetSwitch("bold").SetValue(_note.Bold);
                break;
            case MapNoteType.Line:
                var dx = _note.End.X - _note.Anchor.X;
                var dz = _note.End.Y - _note.Anchor.Y;
                SetNumber("length", Math.Round(_note.Length, 1));
                SetNumber("angle", Math.Round(NormalizeDegrees(Math.Atan2(dz, dx) * GameMath.RAD2DEG), 1));
                c.GetSwitch("showLength").SetValue(_note.ShowLength);
                break;
            case MapNoteType.Circle:
                SetNumber("radius", _note.Radius);
                c.GetSwitch("showRadius").SetValue(_note.ShowRadius);
                c.GetSwitch("showArea").SetValue(_note.ShowArea);
                c.GetSwitch("fill").SetValue(_note.Fill);
                break;
            case MapNoteType.Rect:
                SetNumber("width", _note.RectWidth);
                SetNumber("height", _note.RectHeight);
                c.GetSwitch("showDims").SetValue(_note.ShowDims);
                c.GetSwitch("showArea").SetValue(_note.ShowArea);
                c.GetSwitch("fill").SetValue(_note.Fill);
                break;
            case MapNoteType.Poly:
                c.GetTextInput("vertices").SetValue(MapNoteCodec.FormatPolyPoints(_note, "; "));
                c.GetSwitch("closed").SetValue(_note.Closed);
                c.GetSwitch("showPerimeter").SetValue(_note.ShowPerimeter);
                c.GetSwitch("showArea").SetValue(_note.ShowArea);
                c.GetSwitch("fill").SetValue(_note.Fill);
                break;
        }
    }

    private void SetNumber(string key, double value)
        => SingleComposer.GetNumberInput(key).SetValue(value.ToString(CultureInfo.InvariantCulture));

    private double GetNumber(string key) => SingleComposer.GetNumberInput(key).GetValue();

    private static double NormalizeDegrees(double degrees) => (degrees % 360 + 360) % 360;

    /// <summary>
    /// Reads the controls into the note
    /// </summary>
    private void ReadValues()
    {
        var c = SingleComposer;
        _note.Text = c.GetTextInput("text").GetText().Trim();

        if (_colorIndex >= 0 && _colorIndex < _colors.Length)
            _note.Color = _colors[_colorIndex];

        var anchor = _note.Anchor;
        switch (_note.Type)
        {
            case MapNoteType.Text:
                _note.FontSize = Math.Clamp(c.GetSlider("fontSize").GetValue(), MapNote.MinFontSize, MapNote.MaxFontSize);
                _note.Bold = c.GetSwitch("bold").On;
                break;
            case MapNoteType.Line:
            {
                var length = Math.Clamp(GetNumber("length"), 0, MaxSize);
                var angle = GetNumber("angle") * GameMath.DEG2RAD;
                var end = new Vec2i(anchor.X + (int)Math.Round(length * Math.Cos(angle)), anchor.Y + (int)Math.Round(length * Math.Sin(angle)));
                SetVertices(anchor, end);
                _note.Style = Styles.FirstOrDefault(s => s.ToString() == c.GetDropDown("style").SelectedValue);
                _note.ShowLength = c.GetSwitch("showLength").On;
                break;
            }
            case MapNoteType.Circle:
                _note.Radius = (int)Math.Clamp(Math.Round(GetNumber("radius")), 1, MaxSize);
                _note.ShowRadius = c.GetSwitch("showRadius").On;
                _note.ShowArea = c.GetSwitch("showArea").On;
                _note.Fill = c.GetSwitch("fill").On;
                break;
            case MapNoteType.Rect:
            {
                var width = (int)Math.Clamp(Math.Round(GetNumber("width")), 1, MaxSize);
                var height = (int)Math.Clamp(Math.Round(GetNumber("height")), 1, MaxSize);
                // Keeps the direction the rectangle was drawn in
                var signX = _note.End.X < anchor.X ? -1 : 1;
                var signZ = _note.End.Y < anchor.Y ? -1 : 1;
                SetVertices(anchor, new Vec2i(anchor.X + signX * (width - 1), anchor.Y + signZ * (height - 1)));
                _note.ShowDims = c.GetSwitch("showDims").On;
                _note.ShowArea = c.GetSwitch("showArea").On;
                _note.Fill = c.GetSwitch("fill").On;
                break;
            }
            case MapNoteType.Poly:
                var vertices = MapNoteCodec.ParsePolyPoints(c.GetTextInput("vertices").GetText(), anchor);
                _note.Vertices = new List<Vec2i> { anchor }.Concat(vertices).Take(MapNote.MaxVertices).ToList();
                _note.Closed = c.GetSwitch("closed").On;
                _note.ShowPerimeter = c.GetSwitch("showPerimeter").On;
                _note.ShowArea = c.GetSwitch("showArea").On;
                _note.Fill = c.GetSwitch("fill").On;
                break;
        }
    }

    private void SetVertices(Vec2i anchor, Vec2i end)
        => _note.Vertices = [anchor, end];

    private bool OnSave()
    {
        ReadValues();
        if (_note.Type == MapNoteType.Text && string.IsNullOrEmpty(_note.Text))
        {
            capi.TriggerIngameError(this, "notext", Lang.Get("gimap-note-error-notext"));
            return true;
        }

        if (IsNew)
        {
            MapNoteCommands.Add(capi, _note);
        }
        else if (_note.Anchor.Equals(_original!.Anchor))
        {
            MapNoteCommands.Modify(capi, _note, _original.OwnIndex);
        }
        else
        {
            // Drawn again from another point: the waypoint is moved by adding a new one
            _note.AnchorY = capi.World.BlockAccessor.GetRainMapHeightAt(_note.Anchor.X, _note.Anchor.Y);
            MapNoteCommands.Remove(capi, _original.OwnIndex);
            MapNoteCommands.Add(capi, _note);
        }

        TryClose();
        return true;
    }

    private bool OnDelete()
    {
        if (_original is { OwnIndex: >= 0 })
            MapNoteCommands.Remove(capi, _original.OwnIndex);

        TryClose();
        return true;
    }

    private bool OnRedraw()
    {
        ReadValues();
        TryClose();
        NoteDrawingSession.Start(capi, _note, _original, null);
        return true;
    }

    private bool OnCancel()
    {
        TryClose();
        return true;
    }

    public override void OnGuiClosed()
    {
        base.OnGuiClosed();

        if (_instance == this)
            _instance = null;

        var worldMapDlg = capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg;
        if (worldMapDlg != null && worldMapDlg.IsOpened())
            capi.Gui.RequestFocus(worldMapDlg);
    }
}
