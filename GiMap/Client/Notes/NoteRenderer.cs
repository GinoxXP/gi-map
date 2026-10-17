using System.Globalization;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace GiMap.Client.Notes;

/// <summary>
/// Draws notes with Cairo in pixel coordinates of a surface. <paramref name="toPx"/> converts world block coordinates
/// (x, z) to surface pixels, <c>scale</c> is pixels per block
/// </summary>
public static class NoteRenderer
{
    private const double FillAlpha = 0.25;

    public delegate (double X, double Y) ToPx(double worldX, double worldZ);

    /// <summary>
    /// Screen rectangle of a drawn label, used to hit labels with the mouse
    /// </summary>
    public record struct LabelRect(MapNote Note, double X1, double Y1, double X2, double Y2);

    private static double Ui(double value) => value * RuntimeEnv.GUIScale;

    private static double LineWidthOf(MapNote note) => Math.Max(1, Ui(note.LineWidth));

    // Waves and teeth grow with thick lines
    private static double WaveAmplitude(double width) => Math.Max(Ui(4), width * 2);
    private static double WavePeriod(double width) => Math.Max(Ui(14), width * 7);

    public static void Draw(Context ctx, MapNote note, ToPx toPx, double scale, List<LabelRect>? labels)
    {
        var rgba = ColorUtil.ToRGBADoubles(note.Color);
        rgba[3] = 1;
        ctx.LineJoin = LineJoin.Round;
        ctx.LineCap = LineCap.Butt;

        switch (note.Type)
        {
            case MapNoteType.Text:
                DrawText(ctx, note, toPx, rgba, labels);
                break;
            case MapNoteType.Line:
                DrawLine(ctx, note, toPx, rgba, labels);
                break;
            case MapNoteType.Circle:
                DrawCircle(ctx, note, toPx, scale, rgba, labels);
                break;
            case MapNoteType.Rect:
                DrawRect(ctx, note, toPx, rgba, labels);
                break;
            case MapNoteType.Poly:
                DrawPoly(ctx, note, toPx, rgba, labels);
                break;
        }
    }

    /// <summary>
    /// Dots on the vertices of a note being drawn
    /// </summary>
    public static void DrawVertexDots(Context ctx, IEnumerable<Vec2i> vertices, ToPx toPx, double[] rgba)
    {
        foreach (var vertex in vertices)
        {
            var (x, y) = toPx(vertex.X + 0.5, vertex.Y + 0.5);
            ctx.Arc(x, y, Ui(3.5), 0, 2 * Math.PI);
            ctx.SetSourceRGBA(rgba[0], rgba[1], rgba[2], 1);
            ctx.FillPreserve();
            ctx.LineWidth = Ui(1);
            ctx.SetSourceRGBA(0, 0, 0, 0.8);
            ctx.Stroke();
        }
    }

    private static (double X, double Y) Center(Vec2i v, ToPx toPx) => toPx(v.X + 0.5, v.Y + 0.5);

    private static void DrawText(Context ctx, MapNote note, ToPx toPx, double[] rgba, List<LabelRect>? labels)
    {
        var (x, y) = Center(note.Anchor, toPx);
        var text = string.IsNullOrWhiteSpace(note.Text) ? "?" : note.Text;
        DrawLabels(ctx, note, [text], x, y, 0, note.FontSize, note.Bold, rgba, labels);
    }

    private static void DrawLine(Context ctx, MapNote note, ToPx toPx, double[] rgba, List<LabelRect>? labels)
    {
        var a = Center(note.Anchor, toPx);
        var b = Center(note.End, toPx);
        StrokeStyledSegment(ctx, a, b, note.Style, LineWidthOf(note), rgba);

        var angle = Math.Atan2(b.Y - a.Y, b.X - a.X);
        // Text is never upside down
        if (angle > Math.PI / 2 || angle < -Math.PI / 2)
            angle += Math.PI;

        var midX = (a.X + b.X) / 2;
        var midY = (a.Y + b.Y) / 2;
        var offset = StyleHalfWidth(note.Style, LineWidthOf(note)) + Ui(3);
        if (!string.IsNullOrEmpty(note.Text))
            DrawLabelsAt(ctx, note, [note.Text], midX, midY, angle, -offset, note.FontSize, false, rgba, labels, alignBottom: true);
        if (note.ShowLength)
            DrawLabelsAt(ctx, note, [Lang.Get("gimap-note-length", FormatNumber(note.Length))], midX, midY, angle, offset,
                note.FontSize, false, rgba, labels, alignBottom: false);
    }

    private static void DrawCircle(Context ctx, MapNote note, ToPx toPx, double scale, double[] rgba, List<LabelRect>? labels)
    {
        var (x, y) = Center(note.Anchor, toPx);
        var radius = Math.Max(0.5, note.Radius * scale);

        ctx.NewPath();
        ctx.Arc(x, y, radius, 0, 2 * Math.PI);
        FillAndStroke(ctx, note, rgba);

        var lines = new List<string>();
        if (!string.IsNullOrEmpty(note.Text))
            lines.Add(note.Text);
        if (note.ShowRadius)
            lines.Add(Lang.Get("gimap-note-radius", FormatNumber(note.Radius)));
        if (note.ShowArea)
            lines.Add(Lang.Get("gimap-note-area", FormatNumber(note.Area)));
        DrawLabels(ctx, note, lines, x, y, 0, note.FontSize, false, rgba, labels);
    }

    private static void DrawRect(Context ctx, MapNote note, ToPx toPx, double[] rgba, List<LabelRect>? labels)
    {
        var a = note.Anchor;
        var b = note.End;
        var (x1, y1) = toPx(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        var (x2, y2) = toPx(Math.Max(a.X, b.X) + 1, Math.Max(a.Y, b.Y) + 1);

        ctx.NewPath();
        ctx.Rectangle(x1, y1, x2 - x1, y2 - y1);
        FillAndStroke(ctx, note, rgba);

        var lines = new List<string>();
        if (!string.IsNullOrEmpty(note.Text))
            lines.Add(note.Text);
        if (note.ShowArea)
            lines.Add(Lang.Get("gimap-note-area", FormatNumber(note.Area)));
        DrawLabels(ctx, note, lines, (x1 + x2) / 2, (y1 + y2) / 2, 0, note.FontSize, false, rgba, labels);

        if (note.ShowDims)
        {
            var offset = Ui(3);
            DrawLabelsAt(ctx, note, [FormatNumber(note.RectWidth)], (x1 + x2) / 2, y1, 0, -offset, note.FontSize, false, rgba,
                labels, alignBottom: true);
            DrawLabelsAt(ctx, note, [FormatNumber(note.RectHeight)], x2, (y1 + y2) / 2, -Math.PI / 2, offset, note.FontSize,
                false, rgba, labels, alignBottom: false);
        }
    }

    private static void DrawPoly(Context ctx, MapNote note, ToPx toPx, double[] rgba, List<LabelRect>? labels)
    {
        if (note.Vertices.Count == 0)
            return;

        var points = note.Vertices.Select(v => Center(v, toPx)).ToList();
        ctx.NewPath();
        ctx.MoveTo(points[0].X, points[0].Y);
        foreach (var (x, y) in points.Skip(1))
            ctx.LineTo(x, y);

        if (note.IsClosedPoly)
        {
            ctx.ClosePath();
            FillAndStroke(ctx, note, rgba);
        }
        else
        {
            StrokeOutlined(ctx, rgba, LineWidthOf(note));
        }

        var lines = new List<string>();
        if (!string.IsNullOrEmpty(note.Text))
            lines.Add(note.Text);
        if (note.ShowPerimeter)
            lines.Add(Lang.Get(note.IsClosedPoly ? "gimap-note-perimeter" : "gimap-note-length", FormatNumber(note.Perimeter)));
        if (note.ShowArea && note.IsClosedPoly)
            lines.Add(Lang.Get("gimap-note-area", FormatNumber(note.Area)));

        var (labelX, labelY) = note.IsClosedPoly ? Centroid(points) : MiddleOfPath(points);
        DrawLabels(ctx, note, lines, labelX, labelY, 0, note.FontSize, false, rgba, labels);
    }

    private static void FillAndStroke(Context ctx, MapNote note, double[] rgba)
    {
        if (note.Fill)
        {
            ctx.SetSourceRGBA(rgba[0], rgba[1], rgba[2], FillAlpha);
            ctx.FillPreserve();
        }
        StrokeOutlined(ctx, rgba, LineWidthOf(note));
    }

    // A dark halo under the line keeps it visible on any map mode
    private static void StrokeOutlined(Context ctx, double[] rgba, double width)
    {
        ctx.LineWidth = width + Ui(1.5);
        ctx.SetSourceRGBA(0, 0, 0, 0.45);
        ctx.StrokePreserve();
        ctx.LineWidth = width;
        ctx.SetSourceRGBA(rgba[0], rgba[1], rgba[2], 1);
        ctx.Stroke();
    }

    private static double StyleHalfWidth(NoteLineStyle style, double width) => style switch
    {
        NoteLineStyle.Double => width * 2,
        NoteLineStyle.Triple => width * 3,
        NoteLineStyle.Wavy or NoteLineStyle.Saw => WaveAmplitude(width) + width / 2,
        _ => width / 2,
    };

    private static void StrokeStyledSegment(Context ctx, (double X, double Y) a, (double X, double Y) b, NoteLineStyle style, double width,
        double[] rgba)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.001)
            return;

        // Unit direction and normal
        var ux = dx / length;
        var uy = dy / length;
        var nx = -uy;
        var ny = ux;
        var w = width;

        switch (style)
        {
            case NoteLineStyle.Double:
                foreach (var shift in new[] { -w, w })
                    StrokeSegment(ctx, a, b, nx * shift, ny * shift, rgba, w * 0.75);
                break;
            case NoteLineStyle.Triple:
                foreach (var shift in new[] { -2 * w, 0, 2 * w })
                    StrokeSegment(ctx, a, b, nx * shift, ny * shift, rgba, w * 0.75);
                break;
            case NoteLineStyle.Dashed:
                ctx.SetDash([w * 4, w * 3], 0);
                StrokeSegment(ctx, a, b, 0, 0, rgba, w);
                ctx.SetDash([], 0);
                break;
            case NoteLineStyle.DashDot:
                ctx.SetDash([w * 5, w * 2, w, w * 2], 0);
                StrokeSegment(ctx, a, b, 0, 0, rgba, w);
                ctx.SetDash([], 0);
                break;
            case NoteLineStyle.Wavy:
            case NoteLineStyle.Saw:
                StrokePeriodic(ctx, a, ux, uy, nx, ny, length, style == NoteLineStyle.Wavy, rgba, w);
                break;
            default:
                StrokeSegment(ctx, a, b, 0, 0, rgba, w);
                break;
        }
    }

    private static void StrokeSegment(Context ctx, (double X, double Y) a, (double X, double Y) b, double shiftX, double shiftY,
        double[] rgba, double width)
    {
        ctx.NewPath();
        ctx.MoveTo(a.X + shiftX, a.Y + shiftY);
        ctx.LineTo(b.X + shiftX, b.Y + shiftY);
        StrokeOutlined(ctx, rgba, width);
    }

    // Wave or saw teeth along the segment. Only the part inside the surface is built, so a long line at a high zoom
    // does not produce millions of points
    private static void StrokePeriodic(Context ctx, (double X, double Y) a, double ux, double uy, double nx, double ny,
        double length, bool wavy, double[] rgba, double width)
    {
        var amplitude = WaveAmplitude(width);
        var period = WavePeriod(width);
        var step = wavy ? period / 16 : period / 2;

        var (from, to) = VisibleRange(ctx, a, ux, uy, length, amplitude);
        if (from >= to)
            return;

        // Starts on a whole period so the pattern does not shift while the map moves
        var t = Math.Floor(from / period) * period;
        ctx.NewPath();
        var first = true;
        for (; t <= to + step; t += step)
        {
            var clamped = Math.Min(t, length);
            double offset;
            if (wavy)
            {
                offset = Math.Sin(clamped / period * 2 * Math.PI) * amplitude;
            }
            else
            {
                // Teeth: alternate sides every half period
                var index = (long)Math.Round(clamped / step);
                offset = clamped >= length ? 0 : (index % 2 == 0 ? -amplitude : amplitude);
            }

            var x = a.X + ux * clamped + nx * offset;
            var y = a.Y + uy * clamped + ny * offset;
            if (first)
                ctx.MoveTo(x, y);
            else
                ctx.LineTo(x, y);
            first = false;

            if (clamped >= length)
                break;
        }

        StrokeOutlined(ctx, rgba, width * 0.85);
    }

    // Range of the segment parameter inside the surface clip extended by the margin
    private static (double From, double To) VisibleRange(Context ctx, (double X, double Y) a, double ux, double uy,
        double length, double margin)
    {
        var surface = (ImageSurface)ctx.GetTarget();
        double minX = -margin, minY = -margin, maxX = surface.Width + margin, maxY = surface.Height + margin;
        double from = 0, to = length;

        if (!Clip(-ux, a.X - minX, ref from, ref to) || !Clip(ux, maxX - a.X, ref from, ref to)
            || !Clip(-uy, a.Y - minY, ref from, ref to) || !Clip(uy, maxY - a.Y, ref from, ref to))
            return (0, 0);

        return (from, to);

        // Liang-Barsky
        static bool Clip(double p, double q, ref double from, ref double to)
        {
            if (Math.Abs(p) < 1e-12)
                return q >= 0;

            var r = q / p;
            if (p < 0)
            {
                if (r > to) return false;
                if (r > from) from = r;
            }
            else
            {
                if (r < from) return false;
                if (r < to) to = r;
            }
            return true;
        }
    }

    private static (double X, double Y) Centroid(List<(double X, double Y)> points)
    {
        double area = 0, cx = 0, cy = 0;
        for (var i = 0; i < points.Count; i++)
        {
            var p = points[i];
            var q = points[(i + 1) % points.Count];
            var cross = p.X * q.Y - q.X * p.Y;
            area += cross;
            cx += (p.X + q.X) * cross;
            cy += (p.Y + q.Y) * cross;
        }

        if (Math.Abs(area) < 1e-6)
            return (points.Average(p => p.X), points.Average(p => p.Y));

        return (cx / (3 * area), cy / (3 * area));
    }

    private static (double X, double Y) MiddleOfPath(List<(double X, double Y)> points)
    {
        double total = 0;
        for (var i = 1; i < points.Count; i++)
            total += Distance(points[i - 1], points[i]);

        var half = total / 2;
        for (var i = 1; i < points.Count; i++)
        {
            var segment = Distance(points[i - 1], points[i]);
            if (half <= segment && segment > 0)
            {
                var t = half / segment;
                return (points[i - 1].X + (points[i].X - points[i - 1].X) * t, points[i - 1].Y + (points[i].Y - points[i - 1].Y) * t);
            }
            half -= segment;
        }

        return points[0];
    }

    private static double Distance((double X, double Y) a, (double X, double Y) b)
        => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

    // Lines of text centered on the point
    private static void DrawLabels(Context ctx, MapNote note, IReadOnlyList<string> lines, double x, double y, double angle,
        int fontSize, bool bold, double[] rgba, List<LabelRect>? labels)
    {
        if (lines.Count == 0)
            return;

        var font = CreateFont(fontSize, bold);
        font.SetupContext(ctx);
        var lineHeight = ctx.FontExtents.Height;
        var top = -lineHeight * lines.Count / 2;
        for (var i = 0; i < lines.Count; i++)
            DrawTextLine(ctx, note, lines[i], x, y, angle, top + lineHeight * i, lineHeight, rgba, labels);
    }

    // Lines of text above (alignBottom) or below a point on a line, at the normal offset
    private static void DrawLabelsAt(Context ctx, MapNote note, IReadOnlyList<string> lines, double x, double y, double angle,
        double offset, int fontSize, bool bold, double[] rgba, List<LabelRect>? labels, bool alignBottom)
    {
        var font = CreateFont(fontSize, bold);
        font.SetupContext(ctx);
        var lineHeight = ctx.FontExtents.Height;
        var top = alignBottom ? offset - lineHeight * lines.Count : offset;
        for (var i = 0; i < lines.Count; i++)
            DrawTextLine(ctx, note, lines[i], x, y, angle, top + lineHeight * i, lineHeight, rgba, labels);
    }

    private static void DrawTextLine(Context ctx, MapNote note, string text, double x, double y, double angle, double top,
        double lineHeight, double[] rgba, List<LabelRect>? labels)
    {
        var extents = ctx.TextExtents(text);
        var width = extents.XAdvance;

        ctx.Save();
        ctx.Translate(x, y);
        ctx.Rotate(angle);
        ctx.NewPath();
        ctx.MoveTo(-width / 2, top + ctx.FontExtents.Ascent);
        ctx.TextPath(text);

        // Outline in the contrasting color
        var luminance = 0.299 * rgba[0] + 0.587 * rgba[1] + 0.114 * rgba[2];
        var outline = luminance > 0.5 ? 0.0 : 1.0;
        ctx.LineWidth = Math.Max(Ui(3), ctx.FontExtents.Height * 0.12);
        ctx.SetSourceRGBA(outline, outline, outline, 0.75);
        ctx.StrokePreserve();
        ctx.SetSourceRGBA(rgba[0], rgba[1], rgba[2], 1);
        ctx.Fill();
        ctx.Restore();

        if (labels != null)
        {
            // Bounding box of the rotated rectangle
            var corners = new[] { (-width / 2, top), (width / 2, top), (-width / 2, top + lineHeight), (width / 2, top + lineHeight) };
            var cos = Math.Cos(angle);
            var sin = Math.Sin(angle);
            var xs = corners.Select(c => x + c.Item1 * cos - c.Item2 * sin).ToArray();
            var ys = corners.Select(c => y + c.Item1 * sin + c.Item2 * cos).ToArray();
            labels.Add(new LabelRect(note, xs.Min(), ys.Min(), xs.Max(), ys.Max()));
        }
    }

    private static CairoFont CreateFont(int fontSize, bool bold)
    {
        var font = CairoFont.WhiteSmallText().WithFontSize(fontSize);
        if (bold)
            font = font.WithWeight(FontWeight.Bold);
        return font;
    }

    public static string FormatNumber(double value)
        => value >= 100 ? Math.Round(value).ToString("N0", CultureInfo.CurrentCulture) : value.ToString("0.#", CultureInfo.CurrentCulture);
}
