using System.Text.RegularExpressions;
using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Fertility;

public class FertilityMapLayer : AMapLayer
{
    public override string Title => MapTypes.Fertility;

    // Every N-th diagonal of a hatched block is drawn with the hatch color
    private const int HatchMask = 3;

    private readonly (Regex[] patterns, int color)[] _fertilityGroups;
    private readonly Regex[] _hatchedBlocks;

    private readonly int _waterColor;
    private readonly int _noFertilityColor;
    private readonly int _hatchColor;

    private int[] _colorByBlockId = Array.Empty<int>();
    private bool[] _isHatchedByBlockId = Array.Empty<bool>();
    private bool[] _isValidByBlockId = Array.Empty<bool>();

    public FertilityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.FertilityMode;
        var veryLowColor = ColorUtilExtensions.HexToColor(config.veryLowFertilityColor);
        var lowColor = ColorUtilExtensions.HexToColor(config.lowFertilityColor);
        var mediumColor = ColorUtilExtensions.HexToColor(config.mediumFertilityColor);
        var highColor = ColorUtilExtensions.HexToColor(config.highFertilityColor);
        var extreameColor = ColorUtilExtensions.HexToColor(config.extreameFertilityColor);
        _waterColor = ColorUtilExtensions.HexToColor(config.waterColor);
        _noFertilityColor = ColorUtilExtensions.HexToColor(config.noFertilityColor);
        _hatchColor = ColorUtilExtensions.HexToColor(config.hatchColor);

        _fertilityGroups = new[]
        {
            (CompilePatterns(config.veryLowFertilityBlocks), veryLowColor),
            (CompilePatterns(config.lowFertilityBlocks), lowColor),
            (CompilePatterns(config.mediumFertilityBlocks), mediumColor),
            (CompilePatterns(config.highFertilityBlocks), highColor),
            (CompilePatterns(config.extreameFertilityBlocks), extreameColor),
        };
        _hatchedBlocks = config.hatchingEnabled ? CompilePatterns(config.hatchedBlocks) : Array.Empty<Regex>();

        FillDictionary(veryLowColor, Lang.Get("very-low"));
        FillDictionary(lowColor, Lang.Get("low"));
        FillDictionary(mediumColor, Lang.Get("medium"));
        FillDictionary(highColor, Lang.Get("high"));
        FillDictionary(extreameColor, Lang.Get("very-high"));
        FillDictionary(_waterColor, Lang.Get("water"));
        FillDictionary(_noFertilityColor, Lang.Get("fertility-no"));
        if (config.hatchingEnabled && !LocalizedNameByColor.ContainsKey(_hatchColor))
            FillDictionary(_hatchColor, Lang.Get("fertility-forestfloor"));
    }

    private static Regex[] CompilePatterns(string[] patterns)
        => (patterns ?? Array.Empty<string>()).Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

    public override void OnLoaded()
    {
        base.OnLoaded();

        var blocks = api.World.Blocks;
        _colorByBlockId = new int[blocks.Count];
        _isHatchedByBlockId = new bool[blocks.Count];
        _isValidByBlockId = new bool[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null)
            {
                _colorByBlockId[i] = _noFertilityColor;
                continue;
            }

            var code = block.Code?.ToString();
            var isFertile = TryGetFertilityColor(code, out var color);
            _colorByBlockId[i] = isFertile ? color : GetMaterialColor(block);
            _isValidByBlockId[i] = isFertile || IsMaterialValid(block);
            _isHatchedByBlockId[i] = code != null && _hatchedBlocks.Any(r => r.IsMatch(code));
        }
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new FertilityMultiChunkMapComponent(_capi, baseCord, this);

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var worldX = chunkPos.X * 32;
        var worldZ = chunkPos.Y * 32;
        var vec2i = new Vec2i();
        var resultPixelArray = new int[1024];
        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int topBlockHeight = mc.RainHeightMap[k];
            int topChunkIndex = topBlockHeight / 32;
            if (topChunkIndex >= _chunksTmp.Length)
                continue;

            MapUtil.PosInt2d(k, 32L, vec2i);
            int blockId = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);

            while (topBlockHeight > 0 && !_isValidByBlockId[blockId])
            {
                topBlockHeight--;
                topChunkIndex = topBlockHeight / 32;
                blockId = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
            }

            resultPixelArray[k] = _isHatchedByBlockId[blockId] && ((worldX + vec2i.X + worldZ + vec2i.Y) & HatchMask) == 0
                ? _hatchColor
                : _colorByBlockId[blockId];
        }

        ClearChunks();
        return resultPixelArray;
    }

    private static bool IsMaterialValid(Block block)
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
    private bool TryGetFertilityColor(string? code, out int color)
    {
        color = 0;
        if (code == null)
            return false;

        foreach (var (patterns, groupColor) in _fertilityGroups)
        {
            if (!patterns.Any(r => r.IsMatch(code)))
                continue;

            color = groupColor;
            return true;
        }

        return false;
    }

    private int GetMaterialColor(Block block)
        => block.BlockMaterial == EnumBlockMaterial.Water ? _waterColor : _noFertilityColor;
}
