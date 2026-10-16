namespace GiMap.Client.Lod;

public static class LodColors
{
    /// <summary>
    /// Most frequent opaque color of a 2x2 block, transparent only when all four are transparent.
    /// Keeps exact legend colors so hover names still resolve, and keeps sparse features like ores visible.
    /// </summary>
    public static int Dominant(int a, int b, int c, int d)
    {
        Span<int> colors = stackalloc int[] { a, b, c, d };
        var best = 0;
        var bestCount = 0;

        for (var i = 0; i < colors.Length; i++)
        {
            var color = colors[i];
            if (IsTransparent(color))
                continue;

            var count = 0;
            for (var j = 0; j < colors.Length; j++)
                if (colors[j] == color)
                    count++;

            if (count > bestCount)
            {
                best = color;
                bestCount = count;
            }
        }

        return best;
    }

    /// <summary>
    /// Per-channel average of a 2x2 block, for shaded maps where neighbour pixels rarely repeat
    /// </summary>
    public static int Average(int a, int b, int c, int d)
    {
        var result = 0;
        for (var shift = 0; shift < 32; shift += 8)
        {
            var sum = ((a >>> shift) & 0xff) + ((b >>> shift) & 0xff) + ((c >>> shift) & 0xff) + ((d >>> shift) & 0xff);
            result |= (sum / 4) << shift;
        }

        return result;
    }

    // Pixels are packed as a << 24 | b << 16 | g << 8 | r
    public static bool IsTransparent(int color)
        => (color >>> 24) == 0;
}
