using Cairo;
using Vintagestory.API.Client;
using Vintagestory.GameContent;

namespace GiMap.Client.Notes;

/// <summary>
/// One Cairo texture with notes over the visible part of the map plus a margin. It is drawn again only when the zoom
/// changes, the view leaves the drawn area or the content changes, so moving the map just shifts the texture
/// </summary>
public class NotesOverlay : IDisposable
{
    private const float RenderZ = 55;

    private readonly ICoreClientAPI _capi;
    // Part of the view size added on every side
    private readonly double _margin;
    private LoadedTexture? _texture;

    // World coordinates of the texture pixel (0, 0) and pixels per block it was drawn with
    private double _originX;
    private double _originZ;
    private double _scale;
    private int _width;
    private int _height;
    private int _drawnVersion = -1;

    public readonly List<NoteRenderer.LabelRect> Labels = new();

    public NotesOverlay(ICoreClientAPI capi, double margin)
    {
        _capi = capi;
        _margin = margin;
    }

    /// <summary>
    /// Renders the overlay, <paramref name="draw"/> is called to draw it again when needed; a changed
    /// <paramref name="version"/> means the content changed
    /// </summary>
    public void Render(GuiElementMap map, int version, Action<Context, NoteRenderer.ToPx, double> draw)
    {
        var view = map.CurrentBlockViewBounds;
        var viewWidth = view.X2 - view.X1;
        var viewHeight = view.Z2 - view.Z1;
        if (viewWidth <= 0 || viewHeight <= 0)
            return;

        var scale = map.Bounds.InnerWidth / viewWidth;
        var needsRedraw = _texture == null
                          || version != _drawnVersion
                          || Math.Abs(scale - _scale) > 1e-9 * Math.Max(1, scale)
                          || view.X1 < _originX || view.Z1 < _originZ
                          || view.X2 > _originX + _width / _scale || view.Z2 > _originZ + _height / _scale;

        if (needsRedraw)
            Redraw(map, version, scale, viewWidth, viewHeight, draw);

        if (_texture == null || _texture.TextureId == 0)
            return;

        var x = (float)(map.Bounds.renderX + (_originX - view.X1) * scale);
        var y = (float)(map.Bounds.renderY + (_originZ - view.Z1) * scale);
        _capi.Render.Render2DTexturePremultipliedAlpha(_texture.TextureId, x, y, _width, _height, RenderZ);
    }

    /// <summary>
    /// Converts a label rectangle of the texture to screen coordinates of the map
    /// </summary>
    public (double X1, double Y1, double X2, double Y2) LabelToScreen(GuiElementMap map, NoteRenderer.LabelRect label)
    {
        var view = map.CurrentBlockViewBounds;
        var offsetX = map.Bounds.renderX + (_originX - view.X1) * _scale;
        var offsetY = map.Bounds.renderY + (_originZ - view.Z1) * _scale;
        return (label.X1 + offsetX, label.Y1 + offsetY, label.X2 + offsetX, label.Y2 + offsetY);
    }

    public void Invalidate() => _drawnVersion = -1;

    private void Redraw(GuiElementMap map, int version, double scale, double viewWidth, double viewHeight,
        Action<Context, NoteRenderer.ToPx, double> draw)
    {
        _drawnVersion = version;
        _scale = scale;
        _originX = map.CurrentBlockViewBounds.X1 - viewWidth * _margin;
        _originZ = map.CurrentBlockViewBounds.Z1 - viewHeight * _margin;
        _width = Math.Max(1, (int)Math.Ceiling(map.Bounds.InnerWidth * (1 + 2 * _margin)));
        _height = Math.Max(1, (int)Math.Ceiling(map.Bounds.InnerHeight * (1 + 2 * _margin)));
        Labels.Clear();

        var originX = _originX;
        var originZ = _originZ;
        using var surface = new ImageSurface(Format.Argb32, _width, _height);
        using (var ctx = new Context(surface))
        {
            ctx.Antialias = Antialias.Best;
            draw(ctx, (worldX, worldZ) => ((worldX - originX) * scale, (worldZ - originZ) * scale), scale);
        }

        _texture ??= new LoadedTexture(_capi);
        _capi.Gui.LoadOrUpdateCairoTexture(surface, true, ref _texture);
    }

    public void Dispose()
    {
        _texture?.Dispose();
        _texture = null;
        Labels.Clear();
    }
}
