using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Chunk;

public class ChunkGridMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.ChunkGrid;

    private readonly int _borderColor;
    private readonly int _smallBorderColor;
    private readonly int _verySmallBorderColor;
    private readonly int _backgroundColor;
    private readonly int[] _gridPixels = new int[1024];

    public ChunkGridMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.ChunkGridMode;
        _borderColor = ColorUtilExtensions.HexToColor(config.borderColor);
        _smallBorderColor = ColorUtilExtensions.HexToColor(config.smallBorderColor);
        _verySmallBorderColor = ColorUtilExtensions.HexToColor(config.verySmallBorderColor);
        _backgroundColor = ColorUtilExtensions.HexToColor(config.backgroundColor);

        var pos = new BlockPos(0);
        for (var k = 0; k < _gridPixels.Length; k++)
        {
            pos.Set(k % 32, 0, k / 32);
            _gridPixels[k] = GetColor(pos, null);
        }
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new ChunkGridMultiChunkComponent(_capi, baseCord, this);

    // The grid does not depend on the world, so every chunk gets the same precomputed image
    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
        => (int[])_gridPixels.Clone();

    protected override int GetColor(BlockPos pos, Block block)
    {
        if (pos.X % 32 == 0 || pos.Z % 32 == 0)
            return _borderColor;

        if (pos.X % 16 == 0 || pos.Z % 16 == 0)
            return _smallBorderColor;

        if (pos.X % 8 == 0 || pos.Z % 8 == 0)
            return _verySmallBorderColor;

        return _backgroundColor;
    }
}
