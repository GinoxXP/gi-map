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

    private (Regex[] patterns, int color)[] _fertilityGroups = Array.Empty<(Regex[], int)>();
    private Regex[] _hatchedBlocks = Array.Empty<Regex>();

    private int _waterColor;
    private int _noFertilityColor;
    private int _hatchColor;
    private int _snowHatchColor;
    private bool _ignoreSnow;

    private int[] _colorByBlockId = Array.Empty<int>();
    private bool[] _isHatchedByBlockId = Array.Empty<bool>();
    private bool[] _isValidByBlockId = Array.Empty<bool>();
    private bool[] _isFertileByBlockId = Array.Empty<bool>();
    private bool[] _isSnowByBlockId = Array.Empty<bool>();

    public FertilityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        ApplyConfig();
    }

    protected override void ApplyConfig()
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
        _snowHatchColor = ColorUtilExtensions.HexToColor(config.snowHatchColor);
        _ignoreSnow = config.ignoreSnow;

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
        if (_ignoreSnow && !LocalizedNameByColor.ContainsKey(_snowHatchColor))
            FillDictionary(_snowHatchColor, Lang.Get("fertility-snow"));

        // On reload the block tables depend on the new settings
        if (_colorByBlockId.Length > 0)
            BuildBlockTables();
    }

    private static Regex[] CompilePatterns(string[] patterns)
        => (patterns ?? Array.Empty<string>()).Select(p => new Regex(p, RegexOptions.Compiled)).ToArray();

    public override void OnLoaded()
    {
        base.OnLoaded();
        BuildBlockTables();
    }

    // Tables are built aside and swapped in, the generation thread may be reading the old ones
    private void BuildBlockTables()
    {
        var blocks = api.World.Blocks;
        var colorByBlockId = new int[blocks.Count];
        var isHatchedByBlockId = new bool[blocks.Count];
        var isValidByBlockId = new bool[blocks.Count];
        var isFertileByBlockId = new bool[blocks.Count];
        var isSnowByBlockId = new bool[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null)
            {
                colorByBlockId[i] = _noFertilityColor;
                continue;
            }

            var code = block.Code?.ToString();
            var isFertile = TryGetFertilityColor(code, out var color);
            colorByBlockId[i] = isFertile ? color : GetMaterialColor(block);
            isFertileByBlockId[i] = isFertile;
            isSnowByBlockId[i] = _ignoreSnow && block.BlockMaterial == EnumBlockMaterial.Snow;
            isValidByBlockId[i] = isFertile || (IsMaterialValid(block) && !isSnowByBlockId[i]);
            isHatchedByBlockId[i] = code != null && _hatchedBlocks.Any(r => r.IsMatch(code));
        }

        _colorByBlockId = colorByBlockId;
        _isHatchedByBlockId = isHatchedByBlockId;
        _isValidByBlockId = isValidByBlockId;
        _isFertileByBlockId = isFertileByBlockId;
        _isSnowByBlockId = isSnowByBlockId;
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
            var isUnderSnow = false;

            while (topBlockHeight > 0 && !_isValidByBlockId[blockId])
            {
                isUnderSnow |= _isSnowByBlockId[blockId];
                topBlockHeight--;
                topChunkIndex = topBlockHeight / 32;
                blockId = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
            }

            var x = worldX + vec2i.X;
            var z = worldZ + vec2i.Y;
            if (isUnderSnow && _isFertileByBlockId[blockId] && ((x - z) & HatchMask) == 0)
                resultPixelArray[k] = _snowHatchColor;
            else if (_isHatchedByBlockId[blockId] && ((x + z) & HatchMask) == 0)
                resultPixelArray[k] = _hatchColor;
            else
                resultPixelArray[k] = _colorByBlockId[blockId];
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
