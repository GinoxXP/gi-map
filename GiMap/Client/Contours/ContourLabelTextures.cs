using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace GiMap.Client.Contours;

/// <summary>
/// Textures of contour labels and peak dots. Created on the main thread while rendering, keyed by value and peak flag
/// </summary>
public class ContourLabelTextures : IDisposable
{
    private readonly ICoreClientAPI _capi;
    private readonly CairoFont _contourLabelFont;
    private readonly CairoFont _peakLabelFont;
    private readonly double[] _peakColor;
    private readonly double[] _outlineColor;

    private readonly Dictionary<(int Value, bool IsPeak), LoadedTexture> _labelTextures = new();
    private LoadedTexture? _peakDotTexture;

    public ContourLabelTextures(ICoreClientAPI capi, string labelColor, string peakColor, string outlineColor)
    {
        _capi = capi;
        _peakColor = ColorUtilExtensions.HexToRgbaDoubles(peakColor);
        _outlineColor = ColorUtilExtensions.HexToRgbaDoubles(outlineColor);
        _contourLabelFont = CairoFont.WhiteDetailText()
            .WithColor(ColorUtilExtensions.HexToRgbaDoubles(labelColor))
            .WithStroke(_outlineColor, 2);
        _peakLabelFont = CairoFont.WhiteDetailText()
            .WithColor(_peakColor)
            .WithStroke(_outlineColor, 2)
            .WithWeight(FontWeight.Bold);
    }

    public LoadedTexture GetLabelTexture(int value, bool isPeak)
    {
        if (_labelTextures.TryGetValue((value, isPeak), out var texture))
            return texture;

        var background = new TextBackground { FillColor = new double[] { 0, 0, 0, 0 }, Padding = 1 };
        texture = _capi.Gui.TextTexture.GenTextTexture(value.ToString(), isPeak ? _peakLabelFont : _contourLabelFont, background);
        _labelTextures[(value, isPeak)] = texture;
        return texture;
    }

    public LoadedTexture GetPeakDotTexture()
    {
        if (_peakDotTexture != null)
            return _peakDotTexture;

        int size = (int)Math.Ceiling(8 * RuntimeEnv.GUIScale);
        double outlineWidth = Math.Max(1, RuntimeEnv.GUIScale * 1.5);

        using var surface = new ImageSurface(Format.Argb32, size, size);
        using var ctx = new Context(surface);
        ctx.Arc(size / 2.0, size / 2.0, size / 2.0 - outlineWidth / 2, 0, 2 * Math.PI);
        ctx.SetSourceRGBA(_peakColor[0], _peakColor[1], _peakColor[2], _peakColor[3]);
        ctx.FillPreserve();
        ctx.LineWidth = outlineWidth;
        ctx.SetSourceRGBA(_outlineColor[0], _outlineColor[1], _outlineColor[2], _outlineColor[3]);
        ctx.Stroke();

        _peakDotTexture = new LoadedTexture(_capi, _capi.Gui.LoadCairoTexture(surface, true), size, size);
        return _peakDotTexture;
    }

    public void Dispose()
    {
        foreach (var texture in _labelTextures.Values)
            texture.Dispose();
        _labelTextures.Clear();

        _peakDotTexture?.Dispose();
        _peakDotTexture = null;
    }
}
