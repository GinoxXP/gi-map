using Cairo;
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

    private int _gravelColor;
    private int _iceColor;
    private int _roadColor;
    private int _sandColor;
    private int _snowColor;
    private int _soilColor;
    private int _stoneColor;
    private int _waterColor;
    private readonly int _errorColor;

    private bool _showContours;
    private int _contourInterval;
    private int _majorContourEvery;
    private int _contourColor;
    private int _majorContourColor;

    // Index contour fragments shorter than this do not get a height label
    private const int MinLabelPixels = 6;

    private bool _showHeightLabels;
    private readonly CairoFont _contourLabelFont;
    private readonly CairoFont _peakLabelFont;
    private readonly double[] _peakColor;
    private readonly double[] _labelOutlineColor;

    public bool ShowHeightLabels => _showHeightLabels;
    // Read from the config on every render, so they change from the settings without redrawing

    public float ContourLabelsMinZoom => ConfigManager.ConfigInstance?.TopographicMode.contourLabelsMinZoom ?? 0;
    public float PeakLabelsMinZoom => ConfigManager.ConfigInstance?.TopographicMode.peakLabelsMinZoom ?? 0;

    // Label textures are created on the main thread while rendering, keyed by height and peak flag
    private readonly Dictionary<(int Height, bool IsPeak), LoadedTexture> _labelTextures = new();
    private LoadedTexture? _peakDotTexture;
    private readonly Vec4f _labelColor = new(1, 1, 1, 1);

    // Index contour label candidates of the chunk being generated, keyed by contour height
    private readonly Dictionary<int, (int Count, int Index, int Distance)> _majorCandidates = new();
    private readonly List<HeightLabel> _labelsTmp = new();

    // Ground heights of the chunk with a one pixel border taken from neighbor chunks
    private readonly int[] _heights = new int[PaddedSize * PaddedSize];
    private readonly bool[] _isWater = new bool[PaddedSize * PaddedSize];
    private IWorldChunk[] _neighborChunksTmp = Array.Empty<IWorldChunk>();

    private int[] _colorByBlockId = Array.Empty<int>();

    public TopographicMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.TopographicMode;
        _errorColor = ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
        _peakColor = ColorUtilExtensions.HexToRgbaDoubles(config.peakColor);
        _labelOutlineColor = ColorUtilExtensions.HexToRgbaDoubles(config.labelOutlineColor);
        _contourLabelFont = CairoFont.WhiteDetailText()
            .WithColor(ColorUtilExtensions.HexToRgbaDoubles(config.majorContourColor))
            .WithStroke(_labelOutlineColor, 2);
        _peakLabelFont = CairoFont.WhiteDetailText()
            .WithColor(_peakColor)
            .WithStroke(_labelOutlineColor, 2)
            .WithWeight(FontWeight.Bold);

        ApplyConfig();
    }

    protected override void ApplyConfig()
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
        _showContours = config.showContours;
        _contourInterval = Math.Max(1, config.contourInterval);
        _majorContourEvery = Math.Max(1, config.majorContourEvery);
        _contourColor = ColorUtilExtensions.HexToColor(config.contourColor);
        _majorContourColor = ColorUtilExtensions.HexToColor(config.majorContourColor);
        _showHeightLabels = config.showHeightLabels;

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

        if (!_showContours && !_showHeightLabels)
            return resultPixelArray;

        LoadNeighborEdges(chunkPos);
        if (_showContours)
            DrawContours(resultPixelArray);

        if (!_showHeightLabels)
            return resultPixelArray;

        return HeightLabel.Append(resultPixelArray, CollectLabels());
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

            if (!isContour)
                continue;

            pixels[k] = isMajor ? _majorContourColor : _contourColor;
            if (isMajor && _showHeightLabels)
                AddMajorCandidate(k, height / majorInterval * majorInterval);
        }
    }

    // Keeps the pixel closest to the chunk center for every index contour height
    private void AddMajorCandidate(int k, int contourHeight)
    {
        int dx = k % 32 * 2 - 31;
        int dz = k / 32 * 2 - 31;
        int distance = dx * dx + dz * dz;

        if (!_majorCandidates.TryGetValue(contourHeight, out var candidate))
        {
            _majorCandidates[contourHeight] = (1, k, distance);
            return;
        }

        _majorCandidates[contourHeight] = distance < candidate.Distance
            ? (candidate.Count + 1, k, distance)
            : (candidate.Count + 1, candidate.Index, candidate.Distance);
    }

    private List<HeightLabel> CollectLabels()
    {
        _labelsTmp.Clear();

        foreach (var (contourHeight, candidate) in _majorCandidates)
        {
            if (candidate.Count >= MinLabelPixels)
                _labelsTmp.Add(new HeightLabel(candidate.Index % 32, candidate.Index / 32, contourHeight, false));
        }
        _majorCandidates.Clear();

        if (TryFindPeak(out int peakIndex))
            _labelsTmp.Add(new HeightLabel(peakIndex % 32, peakIndex / 32, _heights[PaddedIndex(peakIndex % 32, peakIndex / 32)], true));

        return _labelsTmp;
    }

    /// <summary>
    /// The chunk has a peak if its highest land pixel is higher than every pixel around the chunk,
    /// so the peak is claimed by exactly one chunk. Flat chunks with relief lower than a contour interval are skipped.
    /// </summary>
    private bool TryFindPeak(out int peakIndex)
    {
        peakIndex = -1;

        int borderMax = int.MinValue;
        for (var i = 0; i < 32; i++)
        {
            int left = _heights[PaddedIndex(-1, i)];
            int right = _heights[PaddedIndex(32, i)];
            int top = _heights[PaddedIndex(i, -1)];
            int bottom = _heights[PaddedIndex(i, 32)];

            // Unknown surroundings: the chunk will get its peak when it is regenerated
            if (left == SkippedPixel || right == SkippedPixel || top == SkippedPixel || bottom == SkippedPixel)
                return false;

            borderMax = Math.Max(borderMax, Math.Max(Math.Max(left, right), Math.Max(top, bottom)));
        }

        int maxHeight = int.MinValue;
        int minHeight = int.MaxValue;
        for (var k = 0; k < HeightLabel.ChunkPixelCount; k++)
        {
            int index = PaddedIndex(k % 32, k / 32);
            int height = _heights[index];
            if (height <= 0)
                continue;

            minHeight = Math.Min(minHeight, height);
            if (!_isWater[index] && height > maxHeight)
            {
                maxHeight = height;
                peakIndex = k;
            }
        }

        return peakIndex >= 0
            && maxHeight > borderMax
            && maxHeight - minHeight >= _contourInterval;
    }

    public LoadedTexture GetLabelTexture(int height, bool isPeak)
    {
        if (_labelTextures.TryGetValue((height, isPeak), out var texture))
            return texture;

        var background = new TextBackground { FillColor = new double[] { 0, 0, 0, 0 }, Padding = 1 };
        texture = _capi.Gui.TextTexture.GenTextTexture(height.ToString(), isPeak ? _peakLabelFont : _contourLabelFont, background);
        _labelTextures[(height, isPeak)] = texture;
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
        ctx.SetSourceRGBA(_labelOutlineColor[0], _labelOutlineColor[1], _labelOutlineColor[2], _labelOutlineColor[3]);
        ctx.Stroke();

        _peakDotTexture = new LoadedTexture(_capi, _capi.Gui.LoadCairoTexture(surface, true), size, size);
        return _peakDotTexture;
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
        foreach (var texture in _labelTextures.Values)
            texture.Dispose();
        _labelTextures.Clear();

        _peakDotTexture?.Dispose();
        _peakDotTexture = null;

        base.Dispose();
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
