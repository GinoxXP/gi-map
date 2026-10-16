using System.Collections.Concurrent;
using System.Diagnostics;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Lod;

/// <summary>
/// Merged map textures for far zoom. A tile is a 128x128 texture covering 128 * 2^level blocks,
/// the lowest level is built from chunk pieces and every next one from 4 tiles of the level below.
/// Built tiles are cached in their own map db, so a far view reads one tile instead of hundreds of chunks.
/// </summary>
/// <remarks>
/// <see cref="OffThreadTick"/> must run on the map generation thread, the same one that uses the chunk pieces db.
/// </remarks>
public sealed class LodTileSet
{
    public const int TileSize = 128;
    private const int PixelCount = TileSize * TileSize;
    private const int ChunkSize = 32;
    private const int ChunksPerMinTile = (TileSize << LodState.MinLevel) / ChunkSize;
    private const int TexelsPerChunk = ChunkSize >> LodState.MinLevel;
    private const float MaxTTL = 30;
    private const double BuildBudgetMs = 8;
    private const int UploadsPerTick = 16;

    // Written over a cached tile to make it rebuilt, also after a restart
    private static readonly int[] DirtyMarker = Array.Empty<int>();

    private readonly ICoreClientAPI _capi;
    private readonly MapDB _db;
    private readonly Func<List<FastVec2i>, int[]?[]> _readChunkPieces;
    private readonly Func<int, int, int, int, int> _combine;

    private readonly ConcurrentDictionary<TileKey, Tile> _tiles = new();
    private readonly object _requestLock = new();
    private readonly UniqueQueue<TileKey> _requested = new();
    private readonly ConcurrentQueue<TileKey> _ready = new();
    private readonly ConcurrentQueue<FastVec2i> _invalidated = new();
    private volatile bool _purgeRequested;

    // Off-thread only
    private readonly Stack<TileKey> _buildStack = new();
    private readonly Dictionary<TileKey, int[]> _toSave = new();

    // Main thread only
    private int _viewLevel;
    private int _viewX1, _viewZ1, _viewX2, _viewZ2 = -1;
    private float _ttlAccum;
    private Vec3d _tmpWorldPos = new();
    private Vec2f _tmpViewPos = new();

    public LodTileSet(ICoreClientAPI capi, MapDB db, Func<List<FastVec2i>, int[]?[]> readChunkPieces, Func<int, int, int, int, int> combine)
    {
        _capi = capi;
        _db = db;
        _readChunkPieces = readChunkPieces;
        _combine = combine;
    }

    /// <summary>
    /// Marks the visible tiles of the level as used and requests the missing ones. Main thread.
    /// </summary>
    public void UpdateView(Cuboidd blockBounds, int level)
    {
        int span = TileSize << level;
        var blockAccessor = _capi.World.BlockAccessor;
        int maxX = (blockAccessor.MapSizeX - 1) / span;
        int maxZ = (blockAccessor.MapSizeZ - 1) / span;

        _viewLevel = level;
        _viewX1 = Math.Max(0, (int)Math.Floor(blockBounds.X1 / span));
        _viewZ1 = Math.Max(0, (int)Math.Floor(blockBounds.Z1 / span));
        _viewX2 = Math.Min(maxX, (int)Math.Floor(blockBounds.X2 / span));
        _viewZ2 = Math.Min(maxZ, (int)Math.Floor(blockBounds.Z2 / span));

        for (int z = _viewZ1; z <= _viewZ2; z++)
        {
            for (int x = _viewX1; x <= _viewX2; x++)
            {
                var key = new TileKey(level, x, z);
                var tile = _tiles.GetOrAdd(key, _ => new Tile());
                tile.Seen = true;

                if (tile.Pixels != null || tile.Requested)
                    continue;

                lock (_requestLock)
                {
                    if (tile.Requested)
                        continue;

                    tile.Requested = true;
                    _requested.Enqueue(key);
                }
            }
        }
    }

    /// <summary>
    /// Draws the tiles of the last <see cref="UpdateView"/>, a tile that is not ready yet is covered by its children if they are. Main thread.
    /// </summary>
    public void Render(GuiElementMap map, Vec4f? color)
    {
        for (int z = _viewZ1; z <= _viewZ2; z++)
        {
            for (int x = _viewX1; x <= _viewX2; x++)
            {
                if (TryRenderTile(map, new TileKey(_viewLevel, x, z), color))
                    continue;

                if (_viewLevel <= LodState.MinLevel)
                    continue;

                for (int i = 0; i < 4; i++)
                    TryRenderTile(map, new TileKey(_viewLevel - 1, x * 2 + i % 2, z * 2 + i / 2), color);
            }
        }
    }

    /// <summary>
    /// Uploads built tiles and drops the ones not seen for a while. Main thread.
    /// </summary>
    public void MainThreadTick(float dt)
    {
        for (int q = UploadsPerTick; q > 0 && _ready.TryDequeue(out var key); q--)
        {
            if (!_tiles.TryGetValue(key, out var tile) || tile.Pixels == null)
                continue;

            tile.Texture ??= new LoadedTexture(_capi, 0, TileSize, TileSize);
            _capi.Render.LoadOrUpdateTextureFromRgba(tile.Pixels, false, 0, ref tile.Texture);
        }

        _ttlAccum += dt;
        if (_ttlAccum < 1)
            return;
        _ttlAccum = 0;

        foreach (var (key, tile) in _tiles)
        {
            if (tile.Seen)
            {
                tile.Seen = false;
                tile.TTL = MaxTTL;
                continue;
            }

            tile.TTL -= 1;
            if (tile.TTL > 0)
                continue;

            _tiles.TryRemove(key, out _);
            tile.Texture?.Dispose();
        }
    }

    /// <summary>
    /// Writes pending invalidations and, when <paramref name="build"/> is set, builds requested tiles within a time budget. Map generation thread.
    /// </summary>
    public void OffThreadTick(bool build)
    {
        if (_purgeRequested)
            Purge();

        DrainInvalidations();

        if (build)
        {
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed.TotalMilliseconds < BuildBudgetMs && BuildStep())
            {
            }
        }

        Flush();
    }

    /// <summary>
    /// Makes every tile containing the chunk rebuilt. Any thread.
    /// </summary>
    public void Invalidate(FastVec2i chunkCord)
        => _invalidated.Enqueue(chunkCord.Copy());

    /// <summary>
    /// Drops all cached tiles, they are rebuilt from chunk pieces. Any thread.
    /// </summary>
    public void RequestPurge()
        => _purgeRequested = true;

    /// <summary>
    /// Forgets requested tiles, used when the map is closed. Main thread.
    /// </summary>
    public void CancelRequests()
    {
        lock (_requestLock)
        {
            _requested.Clear();
            foreach (var tile in _tiles.Values)
                tile.Requested = false;
        }
    }

    /// <summary>
    /// Color of the visible tile under the world position. Main thread.
    /// </summary>
    public bool TryGetColorAt(Vec3d worldPos, out int color)
    {
        color = 0;
        int span = TileSize << _viewLevel;
        if (worldPos.X < 0 || worldPos.Z < 0)
            return false;

        int tileX = (int)(worldPos.X / span);
        int tileZ = (int)(worldPos.Z / span);
        if (!_tiles.TryGetValue(new TileKey(_viewLevel, tileX, tileZ), out var tile) || tile.Pixels is not { } pixels)
            return false;

        int texelX = ((int)worldPos.X - tileX * span) >> _viewLevel;
        int texelZ = ((int)worldPos.Z - tileZ * span) >> _viewLevel;
        color = pixels[texelZ * TileSize + texelX];
        return !LodColors.IsTransparent(color);
    }

    public void Dispose()
    {
        foreach (var tile in _tiles.Values)
            tile.Texture?.Dispose();

        _tiles.Clear();
    }

    private bool TryRenderTile(GuiElementMap map, TileKey key, Vec4f? color)
    {
        if (!_tiles.TryGetValue(key, out var tile) || tile.Texture == null)
            return false;

        int span = TileSize << key.Level;

        // Both edges are projected separately so neighbour tiles meet without gaps
        _tmpWorldPos.Set(key.X * span, 0, key.Z * span);
        map.TranslateWorldPosToViewPos(_tmpWorldPos, ref _tmpViewPos);
        float x1 = (float)Math.Floor(map.Bounds.renderX + _tmpViewPos.X);
        float y1 = (float)Math.Floor(map.Bounds.renderY + _tmpViewPos.Y);

        _tmpWorldPos.Set((key.X + 1) * span, 0, (key.Z + 1) * span);
        map.TranslateWorldPosToViewPos(_tmpWorldPos, ref _tmpViewPos);
        float x2 = (float)Math.Floor(map.Bounds.renderX + _tmpViewPos.X);
        float y2 = (float)Math.Floor(map.Bounds.renderY + _tmpViewPos.Y);

        _capi.Render.Render2DTexture(tile.Texture.TextureId, x1, y1, x2 - x1, y2 - y1, 50, color);
        return true;
    }

    private void Purge()
    {
        _purgeRequested = false;
        _db.Purge();
        _toSave.Clear();
        _buildStack.Clear();
        _invalidated.Clear();

        foreach (var (key, tile) in _tiles)
        {
            tile.Dirty = true;
            Request(key, tile);
        }
    }

    private void DrainInvalidations()
    {
        int maxLevel = LodState.MaxLevel;

        while (_invalidated.TryDequeue(out var chunk))
        {
            for (int level = LodState.MinLevel; level <= maxLevel; level++)
            {
                int chunksPerTile = (TileSize << level) / ChunkSize;
                var key = new TileKey(level, chunk.X / chunksPerTile, chunk.Y / chunksPerTile);
                _toSave[key] = DirtyMarker;

                if (_tiles.TryGetValue(key, out var tile))
                {
                    tile.Dirty = true;
                    Request(key, tile);
                }
            }
        }
    }

    private void Request(TileKey key, Tile tile)
    {
        lock (_requestLock)
        {
            if (tile.Requested)
                return;

            tile.Requested = true;
            _requested.Enqueue(key);
        }
    }

    /// <summary>
    /// Does one bounded piece of work: loads or builds the tile on top of the stack, or pushes its missing children.
    /// Returns false when there is nothing to do.
    /// </summary>
    private bool BuildStep()
    {
        if (_buildStack.Count == 0)
        {
            TileKey next;
            lock (_requestLock)
            {
                if (_requested.Count == 0)
                    return false;
                next = _requested.Dequeue();
            }

            // Dropped by TTL while waiting
            if (!_tiles.ContainsKey(next))
                return true;

            _buildStack.Push(next);
        }

        var key = _buildStack.Peek();
        var pixels = TryLoad(key);

        if (pixels == null)
        {
            if (key.Level == LodState.MinLevel)
            {
                pixels = BuildFromChunks(key);
            }
            else
            {
                var children = new int[]?[4];
                var isAnyMissing = false;
                for (int i = 0; i < 4; i++)
                {
                    var child = new TileKey(key.Level - 1, key.X * 2 + i % 2, key.Z * 2 + i / 2);
                    children[i] = TryLoad(child);
                    if (children[i] != null)
                        continue;

                    _buildStack.Push(child);
                    isAnyMissing = true;
                }

                if (isAnyMissing)
                    return true;

                pixels = BuildFromChildren(children!);
            }

            _toSave[key] = pixels;
        }

        _buildStack.Pop();
        Finish(key, pixels);
        return true;
    }

    private void Finish(TileKey key, int[] pixels)
    {
        if (!_tiles.TryGetValue(key, out var tile))
            return;

        tile.Pixels = pixels;
        tile.Dirty = false;
        lock (_requestLock)
            tile.Requested = false;

        _ready.Enqueue(key);
    }

    private int[]? TryLoad(TileKey key)
    {
        if (_tiles.TryGetValue(key, out var tile) && !tile.Dirty && tile.Pixels != null)
            return tile.Pixels;

        if (_toSave.TryGetValue(key, out var pending))
            return pending.Length == PixelCount ? pending : null;

        var piece = _db.GetMapPiece(key.ToDbKey());
        return piece?.Pixels?.Length == PixelCount ? piece.Pixels : null;
    }

    private int[] BuildFromChunks(TileKey key)
    {
        var cords = new List<FastVec2i>(ChunksPerMinTile * ChunksPerMinTile);
        for (int dz = 0; dz < ChunksPerMinTile; dz++)
            for (int dx = 0; dx < ChunksPerMinTile; dx++)
                cords.Add(new FastVec2i(key.X * ChunksPerMinTile + dx, key.Z * ChunksPerMinTile + dz));

        var pieces = _readChunkPieces(cords);
        var result = new int[PixelCount];

        for (int i = 0; i < cords.Count; i++)
        {
            // Pieces may carry extra data after the pixels (topographic height labels), only the pixels are read
            var pixels = pieces[i];
            if (pixels == null || pixels.Length < ChunkSize * ChunkSize)
                continue;

            int size = ChunkSize;
            for (int level = 0; level < LodState.MinLevel; level++)
            {
                pixels = Reduce(pixels, size);
                size /= 2;
            }

            Blit(pixels, size, result, i % ChunksPerMinTile * TexelsPerChunk, i / ChunksPerMinTile * TexelsPerChunk);
        }

        return result;
    }

    private int[] BuildFromChildren(int[][] children)
    {
        var result = new int[PixelCount];
        const int half = TileSize / 2;

        for (int i = 0; i < 4; i++)
            Blit(Reduce(children[i], TileSize), half, result, i % 2 * half, i / 2 * half);

        return result;
    }

    private int[] Reduce(int[] source, int size)
    {
        int half = size / 2;
        var result = new int[half * half];

        for (int z = 0; z < half; z++)
        {
            int row = z * 2 * size;
            for (int x = 0; x < half; x++)
            {
                int i = row + x * 2;
                result[z * half + x] = _combine(source[i], source[i + 1], source[i + size], source[i + size + 1]);
            }
        }

        return result;
    }

    private static void Blit(int[] source, int size, int[] target, int targetX, int targetZ)
    {
        for (int z = 0; z < size; z++)
            Array.Copy(source, z * size, target, (targetZ + z) * TileSize + targetX, size);
    }

    private void Flush()
    {
        if (_toSave.Count == 0)
            return;

        var pieces = new Dictionary<FastVec2i, MapPieceDB>(_toSave.Count);
        foreach (var (key, pixels) in _toSave)
            pieces[key.ToDbKey()] = new MapPieceDB { Pixels = pixels };

        _db.SetMapPieces(pieces);
        _toSave.Clear();
    }

    private readonly record struct TileKey(int Level, int X, int Z)
    {
        // The level is kept in the high bits of Z, MapDB packs the key as Y << 27 | X
        public FastVec2i ToDbKey() => new(X, Z | Level << 20);
    }

    private sealed class Tile
    {
        public volatile int[]? Pixels;
        public LoadedTexture? Texture;
        public volatile bool Requested;
        public bool Dirty;
        public bool Seen = true;
        public float TTL = MaxTTL;
    }
}
