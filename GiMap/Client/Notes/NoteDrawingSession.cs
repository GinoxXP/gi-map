using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client.Notes;

/// <summary>
/// Placing the points of a note by clicks on the world map. Line, Rect and Circle take two clicks,
/// Poly takes clicks until the first vertex is clicked again (closed) or "Done" is pressed (polyline).
/// After that the note editor opens
/// </summary>
public class NoteDrawingSession
{
    // Default geometry when "Done" is pressed after the first point
    private const int DefaultSize = 10;

    private readonly ICoreClientAPI _capi;
    private GuiDialogNoteDrawing? _panel;
    private bool _isFinished;

    public static NoteDrawingSession? Current { get; private set; }

    // Placed points are its vertices
    public MapNote Note { get; }
    // The saved note being drawn again, null for a new one
    public MapNote? Original { get; }
    public Vec2i? Cursor { get; private set; }
    // Changes with every point and cursor move, redraws the preview
    public int Version { get; private set; }

    private NoteDrawingSession(ICoreClientAPI capi, MapNote note, MapNote? original)
    {
        _capi = capi;
        Note = note;
        Original = original;
    }

    public static void Start(ICoreClientAPI capi, MapNote template, MapNote? original, Vec2i? firstPoint)
    {
        Current?.Cancel();

        var note = template.Clone();
        note.Vertices.Clear();
        note.Waypoint = original?.Waypoint;
        if (note.Type == MapNoteType.Poly)
            note.Closed = false;

        var session = new NoteDrawingSession(capi, note, original);
        Current = session;
        session._panel = new GuiDialogNoteDrawing(capi, session);
        session._panel.TryOpen();

        if (firstPoint != null)
            session.AddPoint(firstPoint);
    }

    public int PointCount => Note.Vertices.Count;

    public bool IsOverPanel(double x, double y) => _panel?.SingleComposer?.Bounds.PointInside(x, y) == true;

    private bool IsTwoPoint => Note.Type is MapNoteType.Line or MapNoteType.Rect or MapNoteType.Circle;

    public void SetCursor(Vec2i? cursor)
    {
        if (cursor == null && Cursor == null || cursor != null && Cursor != null && cursor.Equals(Cursor))
            return;

        Cursor = cursor;
        Version++;
    }

    /// <summary>
    /// Adds a point, finishes the note when it is complete. A click on the first vertex of a polygon closes it
    /// </summary>
    public void AddPoint(Vec2i point)
    {
        if (Note.Type == MapNoteType.Poly && Note.Vertices.Count >= 3 && point.Equals(Note.Vertices[0]))
        {
            Note.Closed = true;
            Finish();
            return;
        }

        // A double click does not add the same point twice
        if (Note.Vertices.Count > 0 && point.Equals(Note.Vertices[^1]))
            return;

        Note.Vertices.Add(point.Copy());
        Version++;
        _panel?.UpdateState();

        if (IsTwoPoint && Note.Vertices.Count >= 2 || Note.Vertices.Count >= MapNote.MaxVertices)
            Finish();
    }

    public void Undo()
    {
        if (Note.Vertices.Count == 0)
            return;

        Note.Vertices.RemoveAt(Note.Vertices.Count - 1);
        Version++;
        _panel?.UpdateState();
    }

    /// <summary>
    /// Ends drawing and opens the editor. Missing points get a default geometry that can be changed there
    /// </summary>
    public void Finish()
    {
        if (_isFinished)
            return;

        if (Note.Vertices.Count == 0)
        {
            Cancel();
            return;
        }

        _isFinished = true;
        var note = Note.Clone();
        var anchor = note.Anchor;
        switch (note.Type)
        {
            case MapNoteType.Line:
                if (note.Vertices.Count < 2)
                    note.Vertices.Add(new Vec2i(anchor.X + DefaultSize, anchor.Y));
                break;
            case MapNoteType.Rect:
                if (note.Vertices.Count < 2)
                    note.Vertices.Add(new Vec2i(anchor.X + DefaultSize - 1, anchor.Y + DefaultSize - 1));
                break;
            case MapNoteType.Circle:
                note.Radius = note.Vertices.Count < 2 ? Math.Max(note.Radius, DefaultSize) : RadiusTo(note.Vertices[1]);
                note.Vertices.RemoveRange(1, note.Vertices.Count - 1);
                break;
        }

        End();
        GuiDialogMapNote.Open(_capi, note, Original);
    }

    public void Cancel()
    {
        if (_isFinished)
            return;

        _isFinished = true;
        End();
    }

    private void End()
    {
        if (Current == this)
            Current = null;

        var panel = _panel;
        _panel = null;
        panel?.TryClose();
    }

    /// <summary>
    /// The note as it would look with the cursor as the next point
    /// </summary>
    public MapNote BuildPreview()
    {
        var preview = Note.Clone();
        if (Cursor != null && preview.Vertices.Count > 0 && !Cursor.Equals(preview.Vertices[^1]))
            preview.Vertices.Add(Cursor.Copy());

        if (preview.Type == MapNoteType.Circle)
        {
            preview.Radius = preview.Vertices.Count >= 2 ? RadiusTo(preview.Vertices[1]) : 0;
            if (preview.Vertices.Count > 1)
                preview.Vertices.RemoveRange(1, preview.Vertices.Count - 1);
        }

        return preview;
    }

    private int RadiusTo(Vec2i point)
    {
        var anchor = Note.Vertices[0];
        return Math.Max(1, (int)Math.Round(Math.Sqrt((double)(point.X - anchor.X) * (point.X - anchor.X)
                                                      + (double)(point.Y - anchor.Y) * (point.Y - anchor.Y))));
    }
}
