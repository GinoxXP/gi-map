using System.Globalization;
using System.Text;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Notes;

/// <summary>
/// Notes are stored in waypoint titles, so they work on servers without the mod:
/// <c>{GIMAP_NOTE_LINE to="12,-5" style=dashed len=1 text="River"}</c>.
/// Points are block offsets from the waypoint position, booleans are written as 1 and only when set
/// </summary>
public static class MapNoteCodec
{
    public const string Prefix = "{GIMAP_NOTE_";

    private static readonly Dictionary<MapNoteType, string> TypeNames = new()
    {
        [MapNoteType.Text] = "TEXT",
        [MapNoteType.Line] = "LINE",
        [MapNoteType.Circle] = "CIRCLE",
        [MapNoteType.Rect] = "RECT",
        [MapNoteType.Poly] = "POLY",
    };

    private static readonly Dictionary<NoteLineStyle, string> StyleNames = new()
    {
        [NoteLineStyle.Solid] = "solid",
        [NoteLineStyle.Double] = "double",
        [NoteLineStyle.Triple] = "triple",
        [NoteLineStyle.Dashed] = "dashed",
        [NoteLineStyle.DashDot] = "dashdot",
        [NoteLineStyle.Wavy] = "wavy",
        [NoteLineStyle.Saw] = "saw",
    };

    public static bool IsNote(string? title)
        => title != null && title.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool IsNote(Waypoint? waypoint) => IsNote(waypoint?.Title);

    public static bool TryParse(Waypoint waypoint, out MapNote note)
    {
        note = null!;
        if (!TryParse(waypoint.Title, (int)Math.Floor(waypoint.Position.X), (int)Math.Floor(waypoint.Position.Z), out var parsed))
            return false;

        parsed.AnchorY = (int)Math.Floor(waypoint.Position.Y);
        parsed.Color = waypoint.Color;
        parsed.Waypoint = waypoint;
        note = parsed;
        return true;
    }

    public static bool TryParse(string? title, int anchorX, int anchorZ, out MapNote note)
    {
        note = null!;
        if (!IsNote(title))
            return false;

        var text = title!.Trim();
        if (!text.EndsWith('}'))
            return false;

        var body = text.Substring(Prefix.Length, text.Length - Prefix.Length - 1);
        var typeEnd = 0;
        while (typeEnd < body.Length && !char.IsWhiteSpace(body[typeEnd]))
            typeEnd++;

        var typeName = body[..typeEnd];
        var type = TypeNames.FirstOrDefault(pair => pair.Value.Equals(typeName, StringComparison.OrdinalIgnoreCase));
        if (type.Value == null)
            return false;

        var values = ParseParams(body[typeEnd..]);
        var anchor = new Vec2i(anchorX, anchorZ);
        var result = new MapNote { Type = type.Key };
        result.Vertices.Add(anchor);
        result.Text = values.GetValueOrDefault("text", "");

        switch (result.Type)
        {
            case MapNoteType.Text:
                result.FontSize = Math.Clamp(GetInt(values, "size", MapNote.DefaultFontSize), MapNote.MinFontSize, MapNote.MaxFontSize);
                result.Bold = GetBool(values, "bold");
                break;
            case MapNoteType.Line:
                result.Vertices.Add(ParsePoint(values.GetValueOrDefault("to"), anchor) ?? anchor.Copy());
                result.ShowLength = GetBool(values, "len");
                result.Style = StyleNames.FirstOrDefault(pair => pair.Value == values.GetValueOrDefault("style")).Key;
                break;
            case MapNoteType.Circle:
                result.Radius = Math.Max(0, GetInt(values, "r", 0));
                result.ShowRadius = GetBool(values, "showr");
                result.ShowArea = GetBool(values, "area");
                result.Fill = GetBool(values, "fill");
                break;
            case MapNoteType.Rect:
                result.Vertices.Add(ParsePoint(values.GetValueOrDefault("to"), anchor) ?? anchor.Copy());
                result.ShowDims = GetBool(values, "dims");
                result.ShowArea = GetBool(values, "area");
                result.Fill = GetBool(values, "fill");
                break;
            case MapNoteType.Poly:
                foreach (var point in (values.GetValueOrDefault("pts") ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (ParsePoint(point, anchor) is { } vertex && result.Vertices.Count < MapNote.MaxVertices)
                        result.Vertices.Add(vertex);
                }
                result.Closed = GetBool(values, "closed");
                result.ShowPerimeter = GetBool(values, "perim");
                result.ShowArea = GetBool(values, "area");
                result.Fill = GetBool(values, "fill");
                break;
        }

        note = result;
        return true;
    }

    public static string Format(MapNote note)
    {
        var sb = new StringBuilder(Prefix).Append(TypeNames[note.Type]);
        var anchor = note.Anchor;

        switch (note.Type)
        {
            case MapNoteType.Text:
                if (note.FontSize != MapNote.DefaultFontSize)
                    Append(sb, "size", note.FontSize.ToString(CultureInfo.InvariantCulture));
                AppendBool(sb, "bold", note.Bold);
                break;
            case MapNoteType.Line:
                Append(sb, "to", FormatPoint(note.End, anchor));
                if (note.Style != NoteLineStyle.Solid)
                    Append(sb, "style", StyleNames[note.Style]);
                AppendBool(sb, "len", note.ShowLength);
                break;
            case MapNoteType.Circle:
                Append(sb, "r", note.Radius.ToString(CultureInfo.InvariantCulture));
                AppendBool(sb, "showr", note.ShowRadius);
                AppendBool(sb, "area", note.ShowArea);
                AppendBool(sb, "fill", note.Fill);
                break;
            case MapNoteType.Rect:
                Append(sb, "to", FormatPoint(note.End, anchor));
                AppendBool(sb, "dims", note.ShowDims);
                AppendBool(sb, "area", note.ShowArea);
                AppendBool(sb, "fill", note.Fill);
                break;
            case MapNoteType.Poly:
                Append(sb, "pts", FormatPolyPoints(note));
                AppendBool(sb, "closed", note.Closed);
                AppendBool(sb, "perim", note.ShowPerimeter);
                AppendBool(sb, "area", note.ShowArea);
                AppendBool(sb, "fill", note.Fill);
                break;
        }

        if (!string.IsNullOrEmpty(note.Text))
            Append(sb, "text", note.Text);

        return sb.Append('}').ToString();
    }

    /// <summary>
    /// Vertices after the anchor as offsets from it: "dx,dz;dx,dz"
    /// </summary>
    public static string FormatPolyPoints(MapNote note, string separator = ";")
        => string.Join(separator, note.Vertices.Skip(1).Select(v => FormatPoint(v, note.Anchor)));

    public static List<Vec2i> ParsePolyPoints(string text, Vec2i anchor)
    {
        var result = new List<Vec2i>();
        foreach (var point in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            if (ParsePoint(point, anchor) is { } vertex)
                result.Add(vertex);
        }
        return result;
    }

    private static string FormatPoint(Vec2i point, Vec2i anchor)
        => string.Create(CultureInfo.InvariantCulture, $"{point.X - anchor.X},{point.Y - anchor.Y}");

    private static Vec2i? ParsePoint(string? text, Vec2i anchor)
    {
        if (text == null)
            return null;

        var parts = text.Split(',');
        if (parts.Length != 2
            || !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dx)
            || !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var dz))
            return null;

        return new Vec2i(anchor.X + dx, anchor.Y + dz);
    }

    private static void Append(StringBuilder sb, string key, string value)
    {
        sb.Append(' ').Append(key).Append('=');
        if (value.Length > 0 && value.All(c => !char.IsWhiteSpace(c) && c is not ('"' or '\\' or '{' or '}')))
        {
            sb.Append(value);
            return;
        }

        sb.Append('"');
        foreach (var c in value)
        {
            if (c is '"' or '\\')
                sb.Append('\\');
            // Titles are one line
            sb.Append(c is '\n' or '\r' ? ' ' : c);
        }
        sb.Append('"');
    }

    private static void AppendBool(StringBuilder sb, string key, bool value)
    {
        if (value)
            Append(sb, key, "1");
    }

    private static Dictionary<string, string> ParseParams(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var i = 0;
        while (i < text.Length)
        {
            while (i < text.Length && char.IsWhiteSpace(text[i]))
                i++;

            var keyStart = i;
            while (i < text.Length && text[i] != '=' && !char.IsWhiteSpace(text[i]))
                i++;
            var key = text[keyStart..i];

            if (i >= text.Length || text[i] != '=')
            {
                // A key without a value is a set flag
                if (key.Length > 0)
                    result[key] = "1";
                continue;
            }

            i++;
            var value = new StringBuilder();
            if (i < text.Length && text[i] == '"')
            {
                i++;
                while (i < text.Length && text[i] != '"')
                {
                    if (text[i] == '\\' && i + 1 < text.Length)
                        i++;
                    value.Append(text[i++]);
                }
                i++;
            }
            else
            {
                while (i < text.Length && !char.IsWhiteSpace(text[i]))
                    value.Append(text[i++]);
            }

            if (key.Length > 0)
                result[key] = value.ToString();
        }

        return result;
    }

    private static int GetInt(Dictionary<string, string> values, string key, int fallback)
        => values.TryGetValue(key, out var text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;

    private static bool GetBool(Dictionary<string, string> values, string key)
        => values.TryGetValue(key, out var text) && text is "1" or "true";
}
