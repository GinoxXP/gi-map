using System.Text.RegularExpressions;
using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace GiMap.Client.Ore;

public class OreMapLayer : ABlockMapLayer
{
    public readonly Dictionary<string, int> ColorsByBlockCode = new();
    public readonly Dictionary<string, string> LocalizedNameByBlockCode = new();

    private readonly Dictionary<int, int> _colorByBlockId = new();
    private readonly List<int> _blockIdsTmp = new();
    private bool[] _hasOreTmp;

    public override string Title => MapTypes.Ore;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        foreach (var block in api.World.Blocks)
        {
            if (block.Code == null) continue;
            RegisterBlock(block, ConfigManager.ConfigInstance.OreMode.ores);
            RegisterBlock(block, ConfigManager.ConfigInstance.OreMode.whitelist);
        }
    }

    private void RegisterBlock(Block block, Dictionary<string, string> ores)
    {
        var code = block.Code.ToString();
        foreach (var ore in ores)
        {
            if (!Regex.IsMatch(code, ore.Key)) continue;

            var color = ColorUtilExtensions.HexToColor(ore.Value);
            var name = block.GetHeldItemName(new ItemStack(block));

            if (ColorsByBlockCode.TryAdd(code, color))
            {
                LocalizedNameByBlockCode[code] = name;
                LocalizedNameByColor.TryAdd(color, name);
                _colorByBlockId[block.Id] = color;
            }
            break;
        }
    }

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        _hasOreTmp ??= new bool[_chunksTmp.Length];

        // Same result as the base algorithm, but Y-chunks without ore in their palettes are skipped
        int topOreChunkIndex = -1;
        for (var i = 0; i < _chunksTmp.Length; i++)
        {
            _hasOreTmp[i] = HasOre(_chunksTmp[i]);
            if (_hasOreTmp[i])
                topOreChunkIndex = i;
        }

        var resultPixelArray = new int[1024];
        if (topOreChunkIndex < 0)
        {
            Array.Fill(resultPixelArray, ColorUtil.ColorFromRgba(0, 0, 0, 0));
            ClearChunks();
            return resultPixelArray;
        }

        var vec2i = new Vec2i();
        var pos = new BlockPos(0);
        int baseWorldX = chunkPos.X * 32;
        int baseWorldZ = chunkPos.Y * 32;

        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int topBlockHeight = mc.RainHeightMap[k];
            if (topBlockHeight / 32 >= _chunksTmp.Length)
                continue;

            MapUtil.PosInt2d(k, 32L, vec2i);
            topBlockHeight = Math.Min(topBlockHeight, topOreChunkIndex * 32 + 31);

            Block block = null;
            while (topBlockHeight > 0)
            {
                int topChunkIndex = topBlockHeight / 32;
                if (!_hasOreTmp[topChunkIndex])
                {
                    // Jump to the top of the next lower chunk
                    topBlockHeight = topChunkIndex * 32 - 1;
                    continue;
                }

                int index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
                block = api.World.Blocks[index];
                if (IsBlockValid(block))
                    break;

                topBlockHeight--;
            }

            if (topBlockHeight <= 0)
            {
                resultPixelArray[k] = ColorUtil.ColorFromRgba(0, 0, 0, 0);
                continue;
            }

            int localX = k % 32;
            int localZ = k / 32;
            pos.Set(baseWorldX + localX, topBlockHeight, baseWorldZ + localZ);
            resultPixelArray[k] = GetColor(pos, block);
        }

        ClearChunks();
        return resultPixelArray;
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new OreChunkMapComponent(_capi, baseCord, this);

    protected override bool IsBlockValid(Block block)
        => block != null && _colorByBlockId.ContainsKey(block.Id);

    /// <summary>
    /// Checks block palettes of the chunk. Returns true if ore may be there or if it cannot be proven otherwise.
    /// </summary>
    private bool HasOre(IWorldChunk chunk)
    {
        chunk.Unpack_ReadOnly();
        if (chunk.Data is not ChunkData data)
            return true;

        _blockIdsTmp.Clear();
        data.FuzzyListBlockIds(_blockIdsTmp);
        foreach (var blockId in _blockIdsTmp)
        {
            if (_colorByBlockId.ContainsKey(blockId))
                return true;
        }

        // FuzzyListBlockIds does not include fluids, but UnpackAndReadBlock may return them
        var fluidsLayer = data.fluidsLayer;
        var fluidsPalette = fluidsLayer?.palette;
        if (fluidsPalette == null)
            return false;

        int fluidsCount = Math.Min(fluidsLayer.paletteCount, fluidsPalette.Length);
        for (var i = 0; i < fluidsCount; i++)
        {
            if (_colorByBlockId.ContainsKey(fluidsPalette[i]))
                return true;
        }

        return false;
    }

    protected override int GetColor(BlockPos pos, Block block)
        => _colorByBlockId.TryGetValue(block.Id, out var color)
            ? color
            : ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
}
