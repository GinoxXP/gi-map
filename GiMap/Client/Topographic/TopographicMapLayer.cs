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

    public TopographicMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        FillDictionaries();
    }
    
    private void FillDictionaries()
    {
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.gravelColor), Lang.Get("topographic-gravel"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.iceColor), Lang.Get("topographic-ice"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.roadColor), Lang.Get("topographic-road"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.sandColor), Lang.Get("topographic-sand"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.snowColor), Lang.Get("topographic-snow"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.soilColor), Lang.Get("topographic-soil"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.stoneColor), Lang.Get("topographic-stone"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.waterColor), Lang.Get("water"));
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new TopographicMultiChunkMapComponent(_capi, baseCord, this);
    
    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        var vec2i = new Vec2i();

        for (var i = 0; i < _chunksTmp.Length; i++)
        {
            _chunksTmp[i] = _capi.World.BlockAccessor.GetChunk(chunkPos.X, i, chunkPos.Y);
            if (_chunksTmp[i] == null || !(_chunksTmp[i] as IClientChunk).LoadedFromServer)
                return null;
        }

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

            resultPixelArray[k] = GetMaterialColor(block);
        }
        
        for (var n = 0; n < _chunksTmp.Length; n++)
            _chunksTmp[n] = null;
            
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

    private int GetMaterialColor(Block block) {
        if (IsRoad(block))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.roadColor);
        
        return block.BlockMaterial switch
        {
            EnumBlockMaterial.Gravel => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.gravelColor),
            EnumBlockMaterial.Sand => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.sandColor),
            EnumBlockMaterial.Soil => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.soilColor),
            EnumBlockMaterial.Stone => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.stoneColor),
            EnumBlockMaterial.Ice => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.iceColor),
            EnumBlockMaterial.Snow => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.snowColor),
            EnumBlockMaterial.Water => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.TopographicMode.waterColor),
            _ => ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor),
        };
    }

    private bool IsRoad(Block block)
        => _roadBlocks.Any(b => block.Code.ToString().Contains(b));
}