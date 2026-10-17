using GiMap.Client.Notes;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GiMap.Client.ContextMenu;

/// <summary>
/// Context menu opened by a right click on the world map at the cursor: adding a waypoint or a note and teleporting to the point
/// </summary>
public class GuiDialogMapContextMenu : GuiDialog
{
    private const double Width = 180;
    private const double ButtonHeight = 26;
    private const double Gap = 4;

    private static GuiDialogMapContextMenu? _instance;

    private readonly List<(string Name, Action OnClick)> _items = new();

    public override string? ToggleKeyCombinationCode => null;

    // Above the full-screen world map, below its other dialogs
    public override double DrawOrder => 0.2;

    private GuiDialogMapContextMenu(ICoreClientAPI capi, int mouseX, int mouseY,
        IEnumerable<(string Name, Action OnClick)> items) : base(capi)
    {
        _items.AddRange(items);
        Compose(mouseX, mouseY);
    }

    public static void Open(ICoreClientAPI capi, int mouseX, int mouseY, Vec3d worldPos,
        GuiDialogAddWayPoint addWaypointDialog)
    {
        var items = new List<(string Name, Action OnClick)>
        {
            (Lang.Get("worldmap-contextmenu-addwaypoint"), () => addWaypointDialog.TryOpen()),
        };

        if (ConfigManager.ConfigInstance.NotesMode.isEnabled)
            items.Add((Lang.Get("worldmap-contextmenu-addnote"), () => OpenNoteTypes(capi, mouseX, mouseY, worldPos)));

        if (capi.World.Player.HasPrivilege(Privilege.tp))
            items.Add((Lang.Get("worldmap-contextmenu-teleport"), () => MapTeleport.Teleport(capi, worldPos)));

        Show(capi, mouseX, mouseY, items);
    }

    // Second page of the menu: the type of the new note
    private static void OpenNoteTypes(ICoreClientAPI capi, int mouseX, int mouseY, Vec3d worldPos)
    {
        var items = Enum.GetValues<MapNoteType>()
            .Select(type => (Lang.Get("gimap-note-type-" + type.ToString().ToLowerInvariant()),
                (Action)(() => GuiDialogMapNote.StartNew(capi, type, worldPos))));

        // Opened after this menu has closed
        capi.Event.EnqueueMainThreadTask(() => Show(capi, mouseX, mouseY, items), "gimap-contextmenu");
    }

    private static void Show(ICoreClientAPI capi, int mouseX, int mouseY, IEnumerable<(string Name, Action OnClick)> items)
    {
        _instance?.TryClose();
        _instance = new GuiDialogMapContextMenu(capi, mouseX, mouseY, items);
        _instance.TryOpen();
    }

    private void Compose(int mouseX, int mouseY)
    {
        var padding = GuiStyle.ElementToDialogPadding;
        var width = Width + 2 * padding;
        var height = _items.Count * (ButtonHeight + Gap) - Gap + 2 * padding;

        // Keeps the menu on the screen near the edges
        var frameWidth = capi.Render.FrameWidth / RuntimeEnv.GUIScale;
        var frameHeight = capi.Render.FrameHeight / RuntimeEnv.GUIScale;
        var x = Math.Max(0, Math.Min(mouseX / RuntimeEnv.GUIScale, frameWidth - width));
        var y = Math.Max(0, Math.Min(mouseY / RuntimeEnv.GUIScale, frameHeight - height));

        // Both are sized to the buttons: a fixed dialog with a padded Fill background makes the background wider
        // than the dialog surface, and its blur then writes outside the surface and corrupts the native heap
        var dialogBounds = ElementStdBounds.AutosizedMainDialog
            .WithAlignment(EnumDialogArea.None)
            .WithFixedPosition(x, y);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(padding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var composer = capi.Gui
            .CreateCompo("gimap-contextmenu", dialogBounds)
            .AddShadedDialogBG(bgBounds, false)
            .BeginChildElements(bgBounds);

        double buttonY = 0;
        foreach (var (name, onClick) in _items)
        {
            composer.AddSmallButton(name, () => OnItemClicked(onClick),
                ElementBounds.Fixed(0, buttonY, Width, ButtonHeight), EnumButtonStyle.Small);
            buttonY += ButtonHeight + Gap;
        }

        SingleComposer = composer.EndChildElements().Compose();
    }

    private bool OnItemClicked(Action onClick)
    {
        TryClose();
        onClick();
        return true;
    }

    public override void OnMouseDown(MouseEvent args)
    {
        // A click outside closes the menu and still reaches the map
        if (!SingleComposer.Bounds.PointInside(args.X, args.Y))
        {
            TryClose();
            return;
        }

        base.OnMouseDown(args);
    }

    public override void OnRenderGUI(float deltaTime)
    {
        if (!IsWorldMapOpened())
        {
            TryClose();
            return;
        }

        base.OnRenderGUI(deltaTime);
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

    private bool IsWorldMapOpened()
        => capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg?.IsOpened() == true;
}
