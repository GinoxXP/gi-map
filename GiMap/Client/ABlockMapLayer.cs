using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public abstract class ABlockMapLayer : AMapLayer
{
    private const int SkippedPixel = -1;

    private readonly int[] _topBlockHeights = new int[1024];
    private readonly Block[] _topBlocks = new Block[1024];

    protected ABlockMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var vec2i = new Vec2i();
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        for (var k = 0; k < _topBlockHeights.Length; k++)
        {
            int topBlockHeight = mc.RainHeightMap[k];
            int topChunkIndex = topBlockHeight / 32;
            if (topChunkIndex >= _chunksTmp.Length)
            {
                _topBlockHeights[k] = SkippedPixel;
                continue;
            }

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

            _topBlockHeights[k] = topBlockHeight;
            _topBlocks[k] = block;

            if (topBlockHeight == 0)
                continue;

            minY = Math.Min(minY, topBlockHeight);
            maxY = Math.Max(maxY, topBlockHeight);
        }

        ClearChunks();

        bool isUniform = false;
        int uniformColor = 0;
        if (minY <= maxY)
            isUniform = TryGetUniformColor(chunkPos, minY, maxY, out uniformColor);

        var pos = new BlockPos(0);
        int baseWorldX = chunkPos.X * 32;
        int baseWorldZ = chunkPos.Y * 32;

        var resultPixelArray = new int[1024];
        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int topBlockHeight = _topBlockHeights[k];
            if (topBlockHeight == SkippedPixel)
                continue;

            if (topBlockHeight == 0)
            {
                resultPixelArray[k] = ColorUtil.ColorFromRgba(0, 0, 0, 0);
                continue;
            }

            if (isUniform)
            {
                resultPixelArray[k] = uniformColor;
                continue;
            }

            int localX = k % 32;
            int localZ = k / 32;
            pos.Set(baseWorldX + localX, topBlockHeight, baseWorldZ + localZ);
            resultPixelArray[k] = GetColor(pos, _topBlocks[k]);
        }

        Array.Clear(_topBlocks);
        return resultPixelArray;
    }

    protected abstract int GetColor(BlockPos pos, Block block);

    /// <summary>
    /// Fast path: return true if every non-transparent pixel of the chunk is guaranteed
    /// to get the same color from <see cref="GetColor"/>. minY/maxY bound the Y of those pixels.
    /// </summary>
    protected virtual bool TryGetUniformColor(FastVec2i chunkPos, int minY, int maxY, out int color)
    {
        color = 0;
        return false;
    }

    protected virtual bool IsBlockValid(Block block)
        => block != null && block.Code != null;
}
