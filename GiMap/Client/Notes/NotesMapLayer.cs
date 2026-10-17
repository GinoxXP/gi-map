using System.Runtime.CompilerServices;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Notes;

/// <summary>
/// Draws notes stored in vanilla waypoints over every map mode. It has no tab (see WorldMapManagerPatch),
/// so it stays active, and draws only on the full-screen map
/// </summary>
public class NotesMapLayer : MapLayer
{
    // In GUI pixels: a click on the line of a note or on its label hits it
    private const double HitDistance = 6;
    // In GUI pixels: a mouse move longer than this while the left button is down drags the map instead of a click
    private const double DragDistance = 4;

    private readonly ICoreClientAPI _capi;
    private readonly NotesOverlay _notesOverlay;
    private readonly NotesOverlay _previewOverlay;

    private List<MapNote> _notes = new();
    private int _waypointsSignature;
    private int _notesVersion;

    private double _dragDistance;
    private double _lastMouseX;
    private double _lastMouseY;
    private NoteDrawingSession? _previewSession;

    public override string Title => "gimap-notes";
    public override string LayerGroupCode => MapTypes.Notes;
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    public override bool RequireChunkLoaded => false;

    public NotesMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        _capi = (ICoreClientAPI)api;
        _notesOverlay = new NotesOverlay(_capi, 0.25);
        _previewOverlay = new NotesOverlay(_capi, 0);
        Active = true;
    }

    public override void Render(GuiElementMap mapElem, float dt)
    {
        if (!IsFullMap())
            return;

        RefreshNotes();

        if (_notes.Count > 0)
        {
            _notesOverlay.Render(mapElem, _notesVersion, (ctx, toPx, scale) =>
            {
                foreach (var note in _notes)
                    NoteRenderer.Draw(ctx, note, toPx, scale, _notesOverlay.Labels);
            });
        }

        if (NoteDrawingSession.Current is { PointCount: > 0 } session)
        {
            // Versions of different sessions are not comparable
            if (session != _previewSession)
            {
                _previewSession = session;
                _previewOverlay.Invalidate();
            }

            _previewOverlay.Render(mapElem, session.Version, (ctx, toPx, scale) =>
            {
                var preview = session.BuildPreview();
                NoteRenderer.Draw(ctx, preview, toPx, scale, null);
                NoteRenderer.DrawVertexDots(ctx, session.Note.Vertices, toPx, ColorUtil.ToRGBADoubles(preview.Color));
            });
        }
    }

    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        if (!IsFullMap())
            return;

        if (mapElem.IsDragingMap)
            _dragDistance += Math.Sqrt((args.X - _lastMouseX) * (args.X - _lastMouseX) + (args.Y - _lastMouseY) * (args.Y - _lastMouseY));
        else
            _dragDistance = 0;
        _lastMouseX = args.X;
        _lastMouseY = args.Y;

        if (NoteDrawingSession.Current is { } session)
        {
            session.SetCursor(InsideMap(mapElem, args) ? ToBlock(mapElem, args, session) : null);
            return;
        }

        if (FindNoteAt(mapElem, args.X, args.Y) is { } note)
            hoverText.AppendLine(GetHoverText(note));
    }

    public override void OnMouseUpClient(MouseEvent args, GuiElementMap mapElem)
    {
        if (!IsFullMap())
            return;

        var wasDrag = _dragDistance > DragDistance * RuntimeEnv.GUIScale;
        _dragDistance = 0;

        if (NoteDrawingSession.Current is { } session)
        {
            if (session.IsOverPanel(args.X, args.Y))
                return;

            // A left click is not marked handled: the map dialog then skips GuiElementMap.OnMouseUp,
            // the map stays in the drag state and follows the cursor
            if (args.Button == EnumMouseButton.Left && !wasDrag)
            {
                session.AddPoint(ToBlock(mapElem, args, session));
            }
            else if (args.Button == EnumMouseButton.Right)
            {
                // Handled, so the map does not open the "Add waypoint" dialog
                session.Finish();
                args.Handled = true;
            }
            return;
        }

        if (args.Button != EnumMouseButton.Right || FindNoteAt(mapElem, args.X, args.Y) is not { } note)
            return;

        GuiDialogMapNote.Open(_capi, note.Clone(), note);
        args.Handled = true;
    }

    public override void OnMapClosedClient()
    {
        NoteDrawingSession.Current?.Cancel();
        _notesOverlay.Dispose();
        _previewOverlay.Dispose();
    }

    public override void Dispose()
    {
        NoteDrawingSession.Current?.Cancel();
        _notesOverlay.Dispose();
        _previewOverlay.Dispose();
        base.Dispose();
    }

    private bool IsFullMap()
        => _capi.ModLoader.GetModSystem<WorldMapManager>().worldMapDlg?.DialogType == EnumDialogType.Dialog;

    private static bool InsideMap(GuiElementMap mapElem, MouseEvent args)
        => mapElem.Bounds.PointInside(args.X, args.Y);

    // The block under the mouse; a click near the first vertex of a polygon snaps to it to close it
    private Vec2i ToBlock(GuiElementMap mapElem, MouseEvent args, NoteDrawingSession session)
    {
        var vertices = session.Note.Vertices;
        if (session.Note.Type == MapNoteType.Poly && vertices.Count >= 3)
        {
            var (x, y) = ToScreen(mapElem, vertices[0].X + 0.5, vertices[0].Y + 0.5);
            if (Math.Abs(args.X - x) <= HitDistance * RuntimeEnv.GUIScale && Math.Abs(args.Y - y) <= HitDistance * RuntimeEnv.GUIScale)
                return vertices[0].Copy();
        }

        var view = mapElem.CurrentBlockViewBounds;
        var worldX = view.X1 + (args.X - mapElem.Bounds.renderX) / mapElem.Bounds.InnerWidth * (view.X2 - view.X1);
        var worldZ = view.Z1 + (args.Y - mapElem.Bounds.renderY) / mapElem.Bounds.InnerHeight * (view.Z2 - view.Z1);
        return new Vec2i((int)Math.Floor(worldX), (int)Math.Floor(worldZ));
    }

    private static (double X, double Y) ToScreen(GuiElementMap mapElem, double worldX, double worldZ)
    {
        var view = mapElem.CurrentBlockViewBounds;
        return (mapElem.Bounds.renderX + (worldX - view.X1) / (view.X2 - view.X1) * mapElem.Bounds.InnerWidth,
            mapElem.Bounds.renderY + (worldZ - view.Z1) / (view.Z2 - view.Z1) * mapElem.Bounds.InnerHeight);
    }

    /// <summary>
    /// Parses notes again when the waypoints received from the server change
    /// </summary>
    private void RefreshNotes()
    {
        var waypoints = GetWaypoints();
        var signature = waypoints.Count;
        foreach (var waypoint in waypoints)
            signature = HashCode.Combine(signature, RuntimeHelpers.GetHashCode(waypoint), waypoint.Title, waypoint.Color);

        if (signature == _waypointsSignature)
            return;

        _waypointsSignature = signature;
        _notesVersion++;

        var playerUid = _capi.World.Player.PlayerUID;
        var notes = new List<MapNote>();
        var ownIndex = 0;
        foreach (var waypoint in waypoints)
        {
            var isOwn = waypoint.OwningPlayerUid == playerUid;
            if (MapNoteCodec.TryParse(waypoint, out var note))
            {
                note.OwnIndex = isOwn ? ownIndex : -1;
                notes.Add(note);
            }

            if (isOwn)
                ownIndex++;
        }

        _notes = notes;
    }

    private List<Waypoint> GetWaypoints()
        => _capi.ModLoader.GetModSystem<WorldMapManager>().MapLayers.OfType<WaypointMapLayer>().FirstOrDefault()?.ownWaypoints
           ?? new List<Waypoint>();

    // The topmost note under the mouse: its label, its outline or the inside of a filled shape
    private MapNote? FindNoteAt(GuiElementMap mapElem, double mouseX, double mouseY)
    {
        if (_notes.Count == 0 || !mapElem.Bounds.PointInside(mouseX, mouseY))
            return null;

        var hitDistance = HitDistance * RuntimeEnv.GUIScale;
        for (var i = _notesOverlay.Labels.Count - 1; i >= 0; i--)
        {
            var label = _notesOverlay.Labels[i];
            var (x1, y1, x2, y2) = _notesOverlay.LabelToScreen(mapElem, label);
            if (mouseX >= x1 && mouseX <= x2 && mouseY >= y1 && mouseY <= y2)
                return label.Note;
        }

        for (var i = _notes.Count - 1; i >= 0; i--)
        {
            if (HitsShape(mapElem, _notes[i], mouseX, mouseY, hitDistance))
                return _notes[i];
        }

        return null;
    }

    private static bool HitsShape(GuiElementMap mapElem, MapNote note, double mx, double my, double hitDistance)
    {
        switch (note.Type)
        {
            case MapNoteType.Text:
            {
                var (x, y) = ToScreen(mapElem, note.Anchor.X + 0.5, note.Anchor.Y + 0.5);
                return Math.Abs(mx - x) <= hitDistance && Math.Abs(my - y) <= hitDistance;
            }
            case MapNoteType.Line:
                return DistanceToSegment(mx, my, Center(mapElem, note.Anchor), Center(mapElem, note.End)) <= hitDistance;
            case MapNoteType.Circle:
            {
                var (x, y) = Center(mapElem, note.Anchor);
                var radius = note.Radius * mapElem.Bounds.InnerWidth / (mapElem.CurrentBlockViewBounds.X2 - mapElem.CurrentBlockViewBounds.X1);
                var distance = Math.Sqrt((mx - x) * (mx - x) + (my - y) * (my - y));
                return Math.Abs(distance - radius) <= hitDistance || note.Fill && distance <= radius;
            }
            case MapNoteType.Rect:
            {
                var a = note.Anchor;
                var b = note.End;
                var (x1, y1) = ToScreen(mapElem, Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                var (x2, y2) = ToScreen(mapElem, Math.Max(a.X, b.X) + 1, Math.Max(a.Y, b.Y) + 1);
                var inside = mx >= x1 && mx <= x2 && my >= y1 && my <= y2;
                var nearOutside = mx >= x1 - hitDistance && mx <= x2 + hitDistance && my >= y1 - hitDistance && my <= y2 + hitDistance;
                var nearEdge = nearOutside && (Math.Abs(mx - x1) <= hitDistance || Math.Abs(mx - x2) <= hitDistance
                                                || Math.Abs(my - y1) <= hitDistance || Math.Abs(my - y2) <= hitDistance);
                return nearEdge || note.Fill && inside;
            }
            case MapNoteType.Poly:
            {
                var points = note.Vertices.Select(v => Center(mapElem, v)).ToList();
                if (points.Count == 1)
                    return Math.Abs(mx - points[0].X) <= hitDistance && Math.Abs(my - points[0].Y) <= hitDistance;

                var segments = note.IsClosedPoly ? points.Count : points.Count - 1;
                for (var i = 0; i < segments; i++)
                {
                    if (DistanceToSegment(mx, my, points[i], points[(i + 1) % points.Count]) <= hitDistance)
                        return true;
                }

                return note.Fill && note.IsClosedPoly && IsInsidePolygon(mx, my, points);
            }
        }

        return false;
    }

    private static (double X, double Y) Center(GuiElementMap mapElem, Vec2i v) => ToScreen(mapElem, v.X + 0.5, v.Y + 0.5);

    private static double DistanceToSegment(double px, double py, (double X, double Y) a, (double X, double Y) b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSq = dx * dx + dy * dy;
        var t = lengthSq < 1e-9 ? 0 : Math.Clamp(((px - a.X) * dx + (py - a.Y) * dy) / lengthSq, 0, 1);
        var x = a.X + dx * t - px;
        var y = a.Y + dy * t - py;
        return Math.Sqrt(x * x + y * y);
    }

    private static bool IsInsidePolygon(double px, double py, List<(double X, double Y)> points)
    {
        var inside = false;
        for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
        {
            if (points[i].Y > py != points[j].Y > py
                && px < (points[j].X - points[i].X) * (py - points[i].Y) / (points[j].Y - points[i].Y) + points[i].X)
                inside = !inside;
        }
        return inside;
    }

    private static string GetHoverText(MapNote note)
    {
        var sb = new StringBuilder(Lang.Get("gimap-note-type-" + note.Type.ToString().ToLowerInvariant()));
        if (!string.IsNullOrEmpty(note.Text))
            sb.Append(": ").Append(note.Text);

        switch (note.Type)
        {
            case MapNoteType.Line:
                sb.Append('\n').Append(Lang.Get("gimap-note-length", NoteRenderer.FormatNumber(note.Length)));
                break;
            case MapNoteType.Circle:
                sb.Append('\n').Append(Lang.Get("gimap-note-radius", NoteRenderer.FormatNumber(note.Radius)))
                    .Append('\n').Append(Lang.Get("gimap-note-area", NoteRenderer.FormatNumber(note.Area)));
                break;
            case MapNoteType.Rect:
                sb.Append('\n').Append(Lang.Get("gimap-note-dims", NoteRenderer.FormatNumber(note.RectWidth), NoteRenderer.FormatNumber(note.RectHeight)))
                    .Append('\n').Append(Lang.Get("gimap-note-area", NoteRenderer.FormatNumber(note.Area)));
                break;
            case MapNoteType.Poly:
                sb.Append('\n').Append(Lang.Get(note.IsClosedPoly ? "gimap-note-perimeter" : "gimap-note-length",
                    NoteRenderer.FormatNumber(note.Perimeter)));
                if (note.IsClosedPoly)
                    sb.Append('\n').Append(Lang.Get("gimap-note-area", NoteRenderer.FormatNumber(note.Area)));
                break;
        }

        return sb.ToString();
    }
}
