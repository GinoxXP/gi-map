using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Fertility;

public class FertilityMapLayer : AMapLayer
{
    public override string Title => MapTypes.Fertility;

    private readonly string[] _veryLowFertilityBlocks = {"game:soil-verylow-", "game:farmland-dry-verylow", "game:farmland-moist-verylow" };
    private readonly string[] _lowFertilityBlocks = {"game:soil-low-", "game:farmland-dry-low", "game:farmland-moist-low" };
    private readonly string[] _mediumFertilityBlocks = {"game:soil-medium-", "game:farmland-dry-medium", "game:farmland-moist-medium" };
    private readonly string[] _highFertilityBlocks = {"game:soil-compost-", "game:farmland-dry-compost", "game:farmland-moist-compost" };
    private readonly string[] _extreameFertilityBlocks = {"game:soil-high-", "game:farmland-dry-high", "game:farmland-moist-high" };
    
    private readonly int _veryLowColor;
    private readonly int _lowColor;
    private readonly int _mediumColor;
    private readonly int _highColor;
    private readonly int _extreameColor;
    private readonly int _waterColor;
    private readonly int _noFertilityColor;

    private int[] _colorByBlockId = Array.Empty<int>();

    public FertilityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        var config = ConfigManager.ConfigInstance.FertilityMode;
        _veryLowColor = ColorUtilExtensions.HexToColor(config.veryLowFertilityColor);
        _lowColor = ColorUtilExtensions.HexToColor(config.lowFertilityColor);
        _mediumColor = ColorUtilExtensions.HexToColor(config.mediumFertilityColor);
        _highColor = ColorUtilExtensions.HexToColor(config.highFertilityColor);
        _extreameColor = ColorUtilExtensions.HexToColor(config.extreameFertilityColor);
        _waterColor = ColorUtilExtensions.HexToColor(config.waterColor);
        _noFertilityColor = ColorUtilExtensions.HexToColor(config.noFertilityColor);
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_veryLowColor, Lang.Get("very-low"));
        FillDictionary(_lowColor, Lang.Get("low"));
        FillDictionary(_mediumColor, Lang.Get("medium"));
        FillDictionary(_highColor, Lang.Get("high"));
        FillDictionary(_extreameColor, Lang.Get("very-high"));
        FillDictionary(_waterColor, Lang.Get("water"));
        FillDictionary(_noFertilityColor, Lang.Get("fertility-no"));
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        var blocks = api.World.Blocks;
        _colorByBlockId = new int[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            _colorByBlockId[i] = blocks[i] == null ? _noFertilityColor : GetMaterialColor(blocks[i]);
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new FertilityMultiChunkMapComponent(_capi, baseCord, this);
    
    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var vec2i = new Vec2i();
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

            resultPixelArray[k] = _colorByBlockId[block.Id];
        }

        ClearChunks();
        return resultPixelArray;
    }
    
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
    private int GetMaterialColor(Block block)
    {
        var code = block.Code?.ToString();
        if (code != null)
        {
            if (IsFertilityGroup(code, _veryLowFertilityBlocks))
                return _veryLowColor;

            if (IsFertilityGroup(code, _lowFertilityBlocks))
                return _lowColor;

            if (IsFertilityGroup(code, _mediumFertilityBlocks))
                return _mediumColor;

            if (IsFertilityGroup(code, _highFertilityBlocks))
                return _highColor;

            if (IsFertilityGroup(code, _extreameFertilityBlocks))
                return _extreameColor;
        }

        if (block.BlockMaterial == EnumBlockMaterial.Water)
            return _waterColor;

        return _noFertilityColor;
    }

    private bool IsFertilityGroup(string code, string[] fertilityGroup)
        => fertilityGroup.Any(code.Contains);
}