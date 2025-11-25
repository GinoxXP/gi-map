using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class TopographicMultiChunkMapComponent : MultiChunkMapComponent
{
    private TopographicMapLayer mapLayer;
    private int[][] pixelsToSet;
    private int sideLength = 96;
    private Vec3d chunkWorldPos;
    private Vec2f viewPos;
    
    public TopographicMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, TopographicMapLayer mapLayer) : base(capi, baseChunkCord)
    {
        this.mapLayer = mapLayer;
        chunkWorldPos = new Vec3d(baseChunkCord.X * 32, 0.0, baseChunkCord.Y * 32);
        viewPos = new Vec2f();
    }

    public new void setChunk(int dx, int dz, int[] pixels)
    {
        base.setChunk(dx, dz, pixels);

        if (pixelsToSet == null)
        {
            pixelsToSet = new int[9][];
        }

        pixelsToSet[dz * 3 + dx] = pixels;
    }

    public override void Render(GuiElementMap map, float dt)
    {
        map.TranslateWorldPosToViewPos(chunkWorldPos, ref viewPos);
        capi.Render.Render2DTexture(Texture.TextureId, (int)(map.Bounds.renderX + (double)viewPos.X), (int)(map.Bounds.renderY + (double)viewPos.Y), (int)((float)Texture.Width * map.ZoomLevel), (int)((float)Texture.Height * map.ZoomLevel), renderZ, mapLayer.OverlayColor);
    }

    public override void Dispose()
    {
        base.Dispose();
        mapLayer = null;
        pixelsToSet = null;
    }
}