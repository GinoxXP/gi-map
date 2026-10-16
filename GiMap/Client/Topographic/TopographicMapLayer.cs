using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Topographic;

public class TopographicMapLayer : AMapLayer
{
    public override string Title => MapTypes.Topographic;
    
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
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

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