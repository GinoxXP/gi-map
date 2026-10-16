using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Topographic;

public class TopographicMapLayer : AMapLayer
{
    public override string Title => MapTypes.Topographic;

    private const int SkippedPixel = -1;
    private const int PaddedSize = 34;

    private readonly string[] _roadBlocks = new[]
    {
        "game:stonepath-",
        "game:stonepathslab-",
        "game:stonepathstairs-",
        "game:woodenpath-",
    };

    private readonly int _gravelColor;
    private readonly int _iceColor;
    private readonly int _roadColor;
    private readonly int _sandColor;
    private readonly int _snowColor;
    private readonly int _soilColor;
    private readonly int _stoneColor;
    private readonly int _waterColor;
    private readonly int _errorColor;

    private readonly bool _showContours;
    private readonly int _contourInterval;
    private readonly int _majorContourEvery;
    private readonly int _contourColor;
    private readonly int _majorContourColor;

    // Ground heights of the chunk with a one pixel border taken from neighbor chunks
    private readonly int[] _heights = new int[PaddedSize * PaddedSize];
    private readonly bool[] _isWater = new bool[PaddedSize * PaddedSize];
    private IWorldChunk[] _neighborChunksTmp = Array.Empty<IWorldChunk>();

    private int[] _colorByBlockId = Array.Empty<int>();

    public TopographicMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.TopographicMode;
        _gravelColor = ColorUtilExtensions.HexToColor(config.gravelColor);
        _iceColor = ColorUtilExtensions.HexToColor(config.iceColor);
        _roadColor = ColorUtilExtensions.HexToColor(config.roadColor);
        _sandColor = ColorUtilExtensions.HexToColor(config.sandColor);
        _snowColor = ColorUtilExtensions.HexToColor(config.snowColor);
        _soilColor = ColorUtilExtensions.HexToColor(config.soilColor);
        _stoneColor = ColorUtilExtensions.HexToColor(config.stoneColor);
        _waterColor = ColorUtilExtensions.HexToColor(config.waterColor);
        _errorColor = ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
        _showContours = config.showContours;
        _contourInterval = Math.Max(1, config.contourInterval);
        _majorContourEvery = Math.Max(1, config.majorContourEvery);
        _contourColor = ColorUtilExtensions.HexToColor(config.contourColor);
        _majorContourColor = ColorUtilExtensions.HexToColor(config.majorContourColor);
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_gravelColor, Lang.Get("topographic-gravel"));
        FillDictionary(_iceColor, Lang.Get("topographic-ice"));
        FillDictionary(_roadColor, Lang.Get("topographic-road"));
        FillDictionary(_sandColor, Lang.Get("topographic-sand"));
        FillDictionary(_snowColor, Lang.Get("topographic-snow"));
        FillDictionary(_soilColor, Lang.Get("topographic-soil"));
        FillDictionary(_stoneColor, Lang.Get("topographic-stone"));
        FillDictionary(_waterColor, Lang.Get("water"));

        if (_showContours)
        {
            FillDictionary(_contourColor, Lang.Get("topographic-contour"));
            FillDictionary(_majorContourColor, Lang.Get("topographic-contour-major"));
        }
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        _neighborChunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / 32];

        var blocks = api.World.Blocks;
        _colorByBlockId = new int[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            _colorByBlockId[i] = blocks[i] == null ? _errorColor : GetMaterialColor(blocks[i]);
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new TopographicMultiChunkMapComponent(_capi, baseCord, this);

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var resultPixelArray = new int[1024];
        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int localX = k % 32;
            int localZ = k / 32;
            int paddedIndex = PaddedIndex(localX, localZ);

            int topBlockHeight = GetGroundHeight(_chunksTmp, localX, localZ, mc.RainHeightMap[k], out var block);
            _heights[paddedIndex] = topBlockHeight;
            if (topBlockHeight == SkippedPixel)
                continue;

            _isWater[paddedIndex] = block.BlockMaterial == EnumBlockMaterial.Water;
            resultPixelArray[k] = _colorByBlockId[block.Id];
        }

        ClearChunks();

        if (_showContours)
        {
            LoadNeighborEdges(chunkPos);
            DrawContours(resultPixelArray);
        }

        return resultPixelArray;
    }

    /// <summary>
    /// Walks down from startY to the first block with a valid material.
    /// Returns <see cref="SkippedPixel"/> if startY is above the column.
    /// </summary>
    private int GetGroundHeight(IWorldChunk[] column, int localX, int localZ, int startY, out Block block)
    {
        block = null;
        int topBlockHeight = startY;
        int topChunkIndex = topBlockHeight / 32;
        if (topChunkIndex >= column.Length)
            return SkippedPixel;

        int index = column[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(localX, topBlockHeight % 32, localZ, 32, 32), 3);
        block = api.World.Blocks[index];

        while (topBlockHeight > 0 && !IsBlockValid(block))
        {
            topBlockHeight--;
            topChunkIndex = topBlockHeight / 32;
            index = column[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(localX, topBlockHeight % 32, localZ, 32, 32), 3);
            block = api.World.Blocks[index];
        }

        return topBlockHeight;
    }

    // Fills the border of the padded grid with heights from neighbor chunks, so contour lines
    // do not break on chunk seams. Edges of neighbors that are not loaded are marked as skipped.
    private void LoadNeighborEdges(FastVec2i chunkPos)
    {
        LoadNeighborEdge(chunkPos.X - 1, chunkPos.Y, i => (31, i), i => (-1, i));
        LoadNeighborEdge(chunkPos.X + 1, chunkPos.Y, i => (0, i), i => (32, i));
        LoadNeighborEdge(chunkPos.X, chunkPos.Y - 1, i => (i, 31), i => (i, -1));
        LoadNeighborEdge(chunkPos.X, chunkPos.Y + 1, i => (i, 0), i => (i, 32));
    }

    private void LoadNeighborEdge(int chunkX, int chunkZ, System.Func<int, (int X, int Z)> neighborLocal, System.Func<int, (int X, int Z)> paddedLocal)
    {
        var mc = chunkX < 0 || chunkZ < 0 ? null : api.World.BlockAccessor.GetMapChunk(chunkX, chunkZ);
        bool isLoaded = mc != null && TryLoadColumn(chunkX, chunkZ, _neighborChunksTmp);

        for (var i = 0; i < 32; i++)
        {
            var (px, pz) = paddedLocal(i);
            int paddedIndex = PaddedIndex(px, pz);
            _heights[paddedIndex] = SkippedPixel;
            if (!isLoaded)
                continue;

            var (lx, lz) = neighborLocal(i);
            int height = GetGroundHeight(_neighborChunksTmp, lx, lz, mc.RainHeightMap[lz * 32 + lx], out var block);
            _heights[paddedIndex] = height;
            if (height != SkippedPixel)
                _isWater[paddedIndex] = block.BlockMaterial == EnumBlockMaterial.Water;
        }

        Array.Clear(_neighborChunksTmp);
    }

    private bool TryLoadColumn(int chunkX, int chunkZ, IWorldChunk[] column)
    {
        for (var i = 0; i < column.Length; i++)
        {
            column[i] = _capi.World.BlockAccessor.GetChunk(chunkX, i, chunkZ);
            if (column[i] == null || !(column[i] as IClientChunk).LoadedFromServer)
                return false;
        }
        return true;
    }

    // A pixel belongs to a contour line if a contour level lies between it and a lower neighbor,
    // so lines are one pixel wide and drawn on the upper side of the slope
    private void DrawContours(int[] pixels)
    {
        int majorInterval = _contourInterval * _majorContourEvery;
        Span<int> neighbors = stackalloc int[4];

        for (var k = 0; k < pixels.Length; k++)
        {
            int index = PaddedIndex(k % 32, k / 32);
            int height = _heights[index];
            if (height <= 0 || _isWater[index])
                continue;

            neighbors[0] = index - 1;
            neighbors[1] = index + 1;
            neighbors[2] = index - PaddedSize;
            neighbors[3] = index + PaddedSize;

            bool isContour = false;
            bool isMajor = false;
            foreach (int neighbor in neighbors)
            {
                int neighborHeight = _heights[neighbor];
                if (neighborHeight <= 0 || _isWater[neighbor])
                    continue;

                if (height / _contourInterval > neighborHeight / _contourInterval)
                {
                    isContour = true;
                    isMajor |= height / majorInterval > neighborHeight / majorInterval;
                }
            }

            if (isContour)
                pixels[k] = isMajor ? _majorContourColor : _contourColor;
        }
    }

    private static int PaddedIndex(int x, int z)
        => x + 1 + (z + 1) * PaddedSize;

    private bool IsBlockValid(Block block)
    {
        return block.BlockMaterial == EnumBlockMaterial.Gravel
            || block.BlockMaterial == EnumBlockMaterial.Sand
            || block.BlockMaterial == EnumBlockMaterial.Soil
            || block.BlockMaterial == EnumBlockMaterial.Stone
            || block.BlockMaterial == EnumBlockMaterial.Ice
            || block.BlockMaterial == EnumBlockMaterial.Snow
            || block.BlockMaterial == EnumBlockMaterial.Water;
    }

    // Called once per block type in OnLoaded, the result is looked up by block id
    private int GetMaterialColor(Block block) {
        if (IsRoad(block))
            return _roadColor;

        return block.BlockMaterial switch
        {
            EnumBlockMaterial.Gravel => _gravelColor,
            EnumBlockMaterial.Sand => _sandColor,
            EnumBlockMaterial.Soil => _soilColor,
            EnumBlockMaterial.Stone => _stoneColor,
            EnumBlockMaterial.Ice => _iceColor,
            EnumBlockMaterial.Snow => _snowColor,
            EnumBlockMaterial.Water => _waterColor,
            _ => _errorColor,
        };
    }

    private bool IsRoad(Block block)
    {
        var code = block.Code?.ToString();
        return code != null && _roadBlocks.Any(code.Contains);
    }
}
