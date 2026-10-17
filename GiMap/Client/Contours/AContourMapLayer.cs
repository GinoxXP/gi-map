using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Contours;

/// <summary>
/// Layer that colors every column and draws contour lines with labels over a value of the column: height, depth, etc.
/// Contours are drawn only between contoured pixels, labels are appended to the chunk pixels, see <see cref="HeightLabel"/>.
/// </summary>
public abstract class AContourMapLayer : AMapLayer
{
    protected const int SkippedPixel = -1;
    private const int PaddedSize = 34;

    // Index contour fragments shorter than this do not get a label
    private const int MinLabelPixels = 6;

    protected bool _showContours;
    protected int _contourInterval = 1;
    protected int _majorContourEvery = 1;
    protected int _contourColor;
    protected int _majorContourColor;

    public bool ShowLabels { get; protected set; }

    // Read from the config on every render, so they change from the settings without redrawing
    public abstract float ContourLabelsMinZoom { get; }
    public abstract float PeakLabelsMinZoom { get; }

    public ContourLabelTextures LabelTextures { get; }
    private readonly Vec4f _labelColor = new(1, 1, 1, 1);

    // Index contour label candidates of the chunk being generated, keyed by contour value
    private readonly Dictionary<int, (int Count, int Index, int Distance)> _majorCandidates = new();
    private readonly List<HeightLabel> _labelsTmp = new();

    // Values of the chunk with a one pixel border taken from neighbor chunks
    private readonly int[] _values = new int[PaddedSize * PaddedSize];
    private readonly bool[] _isContoured = new bool[PaddedSize * PaddedSize];
    private IWorldChunk[] _neighborChunksTmp = Array.Empty<IWorldChunk>();

    protected AContourMapLayer(ICoreAPI api, IWorldMapManager mapSink, string labelColor, string peakColor, string labelOutlineColor)
        : base(api, mapSink)
    {
        LabelTextures = new ContourLabelTextures(_capi, labelColor, peakColor, labelOutlineColor);
    }

    /// <summary>
    /// Value and color of a column. Returns <see cref="SkippedPixel"/> if the column is not drawn.
    /// Only pixels with <paramref name="isContoured"/> and a positive value get contour lines and peaks.
    /// </summary>
    protected abstract int SampleColumn(IWorldChunk[] column, int localX, int localZ, int startY, out bool isContoured, out int color);

    public override void OnLoaded()
    {
        base.OnLoaded();

        _neighborChunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / 32];
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new ContourLabelsMapComponent(_capi, baseCord, this);

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var resultPixelArray = new int[HeightLabel.ChunkPixelCount];
        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int localX = k % 32;
            int localZ = k / 32;
            int paddedIndex = PaddedIndex(localX, localZ);

            _values[paddedIndex] = SampleColumn(_chunksTmp, localX, localZ, mc.RainHeightMap[k], out _isContoured[paddedIndex], out var color);
            if (_values[paddedIndex] != SkippedPixel)
                resultPixelArray[k] = color;
        }

        ClearChunks();

        if (!_showContours && !ShowLabels)
            return resultPixelArray;

        LoadNeighborEdges(chunkPos);
        if (_showContours)
            DrawContours(resultPixelArray);

        if (!ShowLabels)
            return resultPixelArray;

        return HeightLabel.Append(resultPixelArray, CollectLabels());
    }

    /// <summary>
    /// Walks down from startY to the first block with a valid material.
    /// Returns <see cref="SkippedPixel"/> if startY is above the column.
    /// </summary>
    protected int GetGroundHeight(IWorldChunk[] column, int localX, int localZ, int startY, out Block block)
    {
        block = null;
        int topBlockHeight = startY;
        if (topBlockHeight / 32 >= column.Length)
            return SkippedPixel;

        block = ReadBlock(column, localX, topBlockHeight, localZ);
        while (topBlockHeight > 0 && !IsBlockValid(block))
        {
            topBlockHeight--;
            block = ReadBlock(column, localX, topBlockHeight, localZ);
        }

        return topBlockHeight;
    }

    protected Block ReadBlock(IWorldChunk[] column, int localX, int y, int localZ)
    {
        int index = column[y / 32].UnpackAndReadBlock(MapUtil.Index3d(localX, y % 32, localZ, 32, 32), 3);
        return api.World.Blocks[index];
    }

    protected static bool IsBlockValid(Block block)
    {
        return block.BlockMaterial == EnumBlockMaterial.Gravel
            || block.BlockMaterial == EnumBlockMaterial.Sand
            || block.BlockMaterial == EnumBlockMaterial.Soil
            || block.BlockMaterial == EnumBlockMaterial.Stone
            || block.BlockMaterial == EnumBlockMaterial.Ice
            || block.BlockMaterial == EnumBlockMaterial.Snow
            || block.BlockMaterial == EnumBlockMaterial.Water;
    }

    // Fills the border of the padded grid with values from neighbor chunks, so contour lines
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
            _values[paddedIndex] = SkippedPixel;
            _isContoured[paddedIndex] = false;
            if (!isLoaded)
                continue;

            var (lx, lz) = neighborLocal(i);
            _values[paddedIndex] = SampleColumn(_neighborChunksTmp, lx, lz, mc.RainHeightMap[lz * 32 + lx], out _isContoured[paddedIndex], out _);
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

    // A pixel belongs to a contour line if a contour level lies between it and a neighbor with a lower value,
    // so lines are one pixel wide and drawn on the side of the higher value
    private void DrawContours(int[] pixels)
    {
        int majorInterval = _contourInterval * _majorContourEvery;
        Span<int> neighbors = stackalloc int[4];

        for (var k = 0; k < pixels.Length; k++)
        {
            int index = PaddedIndex(k % 32, k / 32);
            int value = _values[index];
            if (value <= 0 || !_isContoured[index])
                continue;

            neighbors[0] = index - 1;
            neighbors[1] = index + 1;
            neighbors[2] = index - PaddedSize;
            neighbors[3] = index + PaddedSize;

            bool isContour = false;
            bool isMajor = false;
            foreach (int neighbor in neighbors)
            {
                int neighborValue = _values[neighbor];
                if (neighborValue <= 0 || !_isContoured[neighbor])
                    continue;

                if (value / _contourInterval > neighborValue / _contourInterval)
                {
                    isContour = true;
                    isMajor |= value / majorInterval > neighborValue / majorInterval;
                }
            }

            if (!isContour)
                continue;

            pixels[k] = isMajor ? _majorContourColor : _contourColor;
            if (isMajor && ShowLabels)
                AddMajorCandidate(k, value / majorInterval * majorInterval);
        }
    }

    // Keeps the pixel closest to the chunk center for every index contour value
    private void AddMajorCandidate(int k, int contourValue)
    {
        int dx = k % 32 * 2 - 31;
        int dz = k / 32 * 2 - 31;
        int distance = dx * dx + dz * dz;

        if (!_majorCandidates.TryGetValue(contourValue, out var candidate))
        {
            _majorCandidates[contourValue] = (1, k, distance);
            return;
        }

        _majorCandidates[contourValue] = distance < candidate.Distance
            ? (candidate.Count + 1, k, distance)
            : (candidate.Count + 1, candidate.Index, candidate.Distance);
    }

    private List<HeightLabel> CollectLabels()
    {
        _labelsTmp.Clear();

        foreach (var (contourValue, candidate) in _majorCandidates)
        {
            if (candidate.Count >= MinLabelPixels)
                _labelsTmp.Add(new HeightLabel(candidate.Index % 32, candidate.Index / 32, contourValue, false));
        }
        _majorCandidates.Clear();

        if (TryFindPeak(out int peakIndex))
            _labelsTmp.Add(new HeightLabel(peakIndex % 32, peakIndex / 32, _values[PaddedIndex(peakIndex % 32, peakIndex / 32)], true));

        return _labelsTmp;
    }

    /// <summary>
    /// The chunk has a peak if its highest contoured pixel is higher than every pixel around the chunk,
    /// so the peak is claimed by exactly one chunk. Flat chunks with relief lower than a contour interval are skipped.
    /// </summary>
    private bool TryFindPeak(out int peakIndex)
    {
        peakIndex = -1;

        int borderMax = int.MinValue;
        for (var i = 0; i < 32; i++)
        {
            int left = _values[PaddedIndex(-1, i)];
            int right = _values[PaddedIndex(32, i)];
            int top = _values[PaddedIndex(i, -1)];
            int bottom = _values[PaddedIndex(i, 32)];

            // Unknown surroundings: the chunk will get its peak when it is regenerated
            if (left == SkippedPixel || right == SkippedPixel || top == SkippedPixel || bottom == SkippedPixel)
                return false;

            borderMax = Math.Max(borderMax, Math.Max(Math.Max(left, right), Math.Max(top, bottom)));
        }

        int maxValue = int.MinValue;
        int minValue = int.MaxValue;
        for (var k = 0; k < HeightLabel.ChunkPixelCount; k++)
        {
            int index = PaddedIndex(k % 32, k / 32);
            int value = _values[index];
            if (value <= 0)
                continue;

            minValue = Math.Min(minValue, value);
            if (_isContoured[index] && value > maxValue)
            {
                maxValue = value;
                peakIndex = k;
            }
        }

        return peakIndex >= 0
            && maxValue > borderMax
            && maxValue - minValue >= _contourInterval;
    }

    /// <summary>
    /// Tint for premultiplied alpha label textures, follows the layer alpha slider
    /// </summary>
    public Vec4f GetLabelColor()
    {
        float alpha = OverlayColor.A;
        _labelColor.Set(alpha, alpha, alpha, alpha);
        return _labelColor;
    }

    public override void Dispose()
    {
        LabelTextures.Dispose();
        base.Dispose();
    }

    private static int PaddedIndex(int x, int z)
        => x + 1 + (z + 1) * PaddedSize;
}
