using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public abstract class ABlockMapLayer : AMapLayer
{
    protected ABlockMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var vec2i = new Vec2i();
        var pos = new BlockPos(0);
        int baseWorldX = chunkPos.X * 32;
        int baseWorldZ = chunkPos.Y * 32;

        var resultPixelArray = new int[1024];
        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int topBlockHeight = mc.RainHeightMap[k];
            int topChunkIndex = topBlockHeight / 32;
            if (topChunkIndex >= _chunksTmp.Length)
                continue;

            MapUtil.PosInt2d(k, 32L, vec2i);
            int index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
            Block block = api.World.Blocks[index];

            while (topBlockHeight > 0 && !IsBlockValid(block))
            {
                topBlockHeight--;
                topChunkIndex = topBlockHeight / 32;
                index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
                block = api.World.Blocks[index];
            }

            if (topBlockHeight == 0)
            {
                resultPixelArray[k] = ColorUtil.ColorFromRgba(0, 0, 0, 0);
                continue;
            }

            int localX = k % 32;
            int localZ = k / 32;
            pos.Set(baseWorldX + localX, topBlockHeight, baseWorldZ + localZ);
            resultPixelArray[k] = GetColor(pos);
        }

        ClearChunks();
        return resultPixelArray;
    }

    protected abstract int GetColor(BlockPos pos);
    
    protected virtual bool IsBlockValid(Block block)
        => block != null && block.Code != null;
}