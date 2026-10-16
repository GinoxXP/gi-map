using System.Globalization;
using Vintagestory.API.MathTools;

namespace GiMap;

public static class ColorUtilExtensions
{
    public static int HexToColor(string hex)
    {
        var span = hex.AsSpan().TrimStart('#');

        var r = int.Parse(span.Slice(0, 2), NumberStyles.HexNumber);
        var g = int.Parse(span.Slice(2, 2), NumberStyles.HexNumber);
        var b = int.Parse(span.Slice(4, 2), NumberStyles.HexNumber);
        var a = span.Length < 8 ?
            255 :
            int.Parse(span.Slice(6, 2), NumberStyles.HexNumber);

        return ColorUtil.ToRgba(a, b, g, r);
    }
}
