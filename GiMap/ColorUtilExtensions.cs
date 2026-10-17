using System.Globalization;
using Vintagestory.API.MathTools;

namespace GiMap;

public static class ColorUtilExtensions
{
    public static int HexToColor(string hex)
    {
        var (r, g, b, a) = ParseHex(hex);
        return ColorUtil.ToRgba(a, b, g, r);
    }

    /// <summary>
    /// Color as { r, g, b, a } in 0..1, the format used by Cairo fonts
    /// </summary>
    public static double[] HexToRgbaDoubles(string hex)
    {
        var (r, g, b, a) = ParseHex(hex);
        return new[] { r / 255.0, g / 255.0, b / 255.0, a / 255.0 };
    }

    /// <summary>
    /// Mixes two colors channel by channel, t = 0 gives a, t = 1 gives b
    /// </summary>
    public static int Lerp(int a, int b, float t)
    {
        var result = 0;
        for (var shift = 0; shift < 32; shift += 8)
        {
            int channelA = (a >> shift) & 0xff;
            int channelB = (b >> shift) & 0xff;
            result |= (int)Math.Round(channelA + (channelB - channelA) * t) << shift;
        }

        return result;
    }

    private static (int R, int G, int B, int A) ParseHex(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');

        var r = int.Parse(span.Slice(0, 2), NumberStyles.HexNumber);
        var g = int.Parse(span.Slice(2, 2), NumberStyles.HexNumber);
        var b = int.Parse(span.Slice(4, 2), NumberStyles.HexNumber);
        var a = span.Length < 8 ?
            255 :
            int.Parse(span.Slice(6, 2), NumberStyles.HexNumber);

        return (r, g, b, a);
    }
}
