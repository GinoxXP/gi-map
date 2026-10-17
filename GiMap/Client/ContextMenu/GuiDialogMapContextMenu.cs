using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace GiMap.Client.ContextMenu;

/// <summary>
/// Context menu opened by a right click on the world map at the cursor: adding a waypoint and teleporting to the point
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

    private GuiDialogMapContextMenu(ICoreClientAPI capi, int mouseX, int mouseY, Vec3d worldPos,
        GuiDialogAddWayPoint addWaypointDialog) : base(capi)
    {
        _items.Add((Lang.Get("worldmap-contextmenu-addwaypoint"), () => addWaypointDialog.TryOpen()));

        if (capi.World.Player.HasPrivilege(Privilege.tp))
            _items.Add((Lang.Get("worldmap-contextmenu-teleport"), () => MapTeleport.Teleport(capi, worldPos)));

        Compose(mouseX, mouseY);
    }

    public static void Open(ICoreClientAPI capi, int mouseX, int mouseY, Vec3d worldPos,
        GuiDialogAddWayPoint addWaypointDialog)
    {
        _instance?.TryClose();
        _instance = new GuiDialogMapContextMenu(capi, mouseX, mouseY, worldPos, addWaypointDialog);
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
