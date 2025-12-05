using Vintagestory.API.MathTools;

namespace GiMap;

public static class ColorUtilExtensions
{
    public static int HexToColor(string hex)
    {
        hex = hex.Replace("#", string.Empty);
        
        var r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
        var g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
        var b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
        var a = hex.Length < 8 ?
            255 :
            int.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);

        return ColorUtil.ToRgba(a, b, g, r);
    }
}