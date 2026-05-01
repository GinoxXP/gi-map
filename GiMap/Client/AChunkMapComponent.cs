using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public abstract class AChunkMapComponent : MultiChunkMapComponent
{
    protected AMapLayer _mapLayer;
    protected int[][] _pixelsToSet;
    protected Vec3d _chunkWorldPos;
    protected Vec2f _viewPos;
    
    protected Vec3d _mouseWorldPos = new Vec3d();
    private int _sideLength = 96;
    
    public AChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, AMapLayer mapLayer) : base(capi, baseChunkCord)
    {
        _mapLayer = mapLayer;
        _chunkWorldPos = new Vec3d(baseChunkCord.X * 32, 0.0, baseChunkCord.Y * 32);
        _viewPos = new Vec2f();
    }

    public override void Render(GuiElementMap map, float dt)
    {
        map.TranslateWorldPosToViewPos(_chunkWorldPos, ref _viewPos);
        capi.Render.Render2DTexture(
            Texture.TextureId,
            (int)(map.Bounds.renderX + (double)_viewPos.X),
            (int)(map.Bounds.renderY + (double)_viewPos.Y),
            (int)((float)Texture.Width * map.ZoomLevel),
            (int)((float)Texture.Height * map.ZoomLevel),
            renderZ,
            _mapLayer.OverlayColor);
    }
    
    public new void setChunk(int dx, int dz, int[] pixels)
    {
        base.setChunk(dx, dz, pixels);

        if (_pixelsToSet == null)
        {
            _pixelsToSet = new int[9][];
        }

        _pixelsToSet[dz * 3 + dx] = pixels;
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _mapLayer = null;
        _pixelsToSet = null;
    }
    
    public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        _viewPos.X = args.X - (float)mapElem.Bounds.renderX;
        _viewPos.Y = args.Y - (float)mapElem.Bounds.renderY;
        mapElem.TranslateViewPosToWorldPos(_viewPos, ref _mouseWorldPos);

        if (_mouseWorldPos.X < _chunkWorldPos.X ||
            _mouseWorldPos.X >= _chunkWorldPos.X + _sideLength ||
            _mouseWorldPos.Z < _chunkWorldPos.Z ||
            _mouseWorldPos.Z >= _chunkWorldPos.Z + _sideLength)
            return;
        
        SetHoverText(hoverText);
    }

    protected virtual void SetHoverText(StringBuilder hoverText)
    {
        var posInChunk = _mouseWorldPos - _chunkWorldPos;
        var chunk = _pixelsToSet[posInChunk.XInt / 32 + (posInChunk.ZInt / 32) * 3];
        if (chunk == null)
            return;
        
        var color = chunk[posInChunk.XInt % 32 + (posInChunk.ZInt % 32) * 32];
        var localizedString = _mapLayer.GetLocalizedStringByColor(color);

        hoverText.Append(localizedString);
    }
}