using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public abstract class AChunkMapComponent<TMapLayer> : MultiChunkMapComponent
where TMapLayer : MapLayer
{
    protected TMapLayer _mapLayer;
    protected int[][] _pixelsToSet;
    protected Vec3d _chunkWorldPos;
    protected Vec2f _viewPos;
    
    public AChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, TMapLayer mapLayer) : base(capi, baseChunkCord)
    {
        _mapLayer = mapLayer;
        _chunkWorldPos = new Vec3d(baseChunkCord.X * 32, 0.0, baseChunkCord.Y * 32);
        _viewPos = new Vec2f();
    }
    
    public virtual new void setChunk(int dx, int dz, int[] pixels)
    {
        base.setChunk(dx, dz, pixels);

        if (_pixelsToSet == null)
            _pixelsToSet = new int[9][];

        _pixelsToSet[dz * 3 + dx] = pixels;
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
            renderZ);
    }
    
    public override void Dispose()
    {
        base.Dispose();
        _mapLayer = null;
        _pixelsToSet = null;
    }
}