using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Notes;

public enum MapNoteType
{
    Text,
    Line,
    Circle,
    Rect,
    Poly,
}

public enum NoteLineStyle
{
    Solid,
    Double,
    Triple,
    Dashed,
    DashDot,
    Wavy,
    Saw,
}

/// <summary>
/// Note drawn on the world map, stored in the title of a vanilla waypoint (see <see cref="MapNoteCodec"/>).
/// Vertices are block coordinates, the first one is the anchor (the waypoint position)
/// </summary>
public class MapNote
{
    public const int MinFontSize = 8;
    public const int MaxFontSize = 48;
    // In GUI pixels, constant on the screen at any zoom
    public const int DefaultLineWidth = 2;
    public const int MinLineWidth = 1;
    public const int MaxLineWidth = 12;
    public const int MaxVertices = 100;

    // Text notes are bigger than the labels of shapes
    public static int DefaultFontSize(MapNoteType type) => type == MapNoteType.Text ? 16 : 14;

    public MapNoteType Type;
    // Vertices in world block coordinates (x, z): Text and Circle use the anchor only,
    // Line and Rect use the anchor and the end (opposite corner), Poly uses all of them
    public List<Vec2i> Vertices = new();
    public int AnchorY;
    public int Radius;

    public string Text = "";
    public int FontSize = DefaultFontSize(MapNoteType.Text);
    public bool Bold;
    // Lines and outlines of shapes
    public int LineWidth = DefaultLineWidth;
    public NoteLineStyle Style;

    public bool ShowLength;
    public bool ShowRadius;
    public bool ShowDims;
    public bool ShowPerimeter;
    public bool ShowArea;
    public bool Closed;
    public bool Fill;

    // ARGB, the waypoint color
    public int Color = unchecked((int)0xffffffff);

    // The waypoint the note is stored in, null for a note that is not saved yet
    public Waypoint? Waypoint;
    // Index for /waypoint modify and remove: among the waypoints of the player, -1 for waypoints of other group members
    public int OwnIndex = -1;

    public Vec2i Anchor => Vertices.Count > 0 ? Vertices[0] : new Vec2i();

    public Vec2i End => Vertices.Count > 1 ? Vertices[1] : Anchor;

    public bool IsClosedShape => Type is MapNoteType.Circle or MapNoteType.Rect || Type == MapNoteType.Poly && IsClosedPoly;

    public bool IsClosedPoly => Closed && Vertices.Count >= 3;

    public MapNote Clone()
    {
        var clone = (MapNote)MemberwiseClone();
        clone.Vertices = Vertices.Select(v => v.Copy()).ToList();
        return clone;
    }

    public double Length
    {
        get
        {
            var a = Anchor;
            var b = End;
            return Math.Sqrt((double)(b.X - a.X) * (b.X - a.X) + (double)(b.Y - a.Y) * (b.Y - a.Y));
        }
    }

    // Rect covers the blocks of both corners
    public int RectWidth => Math.Abs(End.X - Anchor.X) + 1;
    public int RectHeight => Math.Abs(End.Y - Anchor.Y) + 1;

    public double Perimeter
    {
        get
        {
            double sum = 0;
            for (var i = 1; i < Vertices.Count; i++)
                sum += Distance(Vertices[i - 1], Vertices[i]);
            if (IsClosedPoly)
                sum += Distance(Vertices[^1], Vertices[0]);
            return sum;
        }
    }

    public double Area => Type switch
    {
        MapNoteType.Circle => Math.PI * Radius * Radius,
        MapNoteType.Rect => (double)RectWidth * RectHeight,
        MapNoteType.Poly when IsClosedPoly => PolygonArea(),
        _ => 0,
    };

    // Shoelace formula over block centers
    private double PolygonArea()
    {
        double sum = 0;
        for (var i = 0; i < Vertices.Count; i++)
        {
            var a = Vertices[i];
            var b = Vertices[(i + 1) % Vertices.Count];
            sum += (double)a.X * b.Y - (double)b.X * a.Y;
        }
        return Math.Abs(sum) / 2;
    }

    private static double Distance(Vec2i a, Vec2i b)
        => Math.Sqrt((double)(b.X - a.X) * (b.X - a.X) + (double)(b.Y - a.Y) * (b.Y - a.Y));
}
