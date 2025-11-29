using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

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
            || block.BlockMaterial == EnumBlockMaterial.Liquid;
    }

    private int GetMaterialColor(Block block)
    {
        if (IsRoad(block))
            return ConfigManager.ConfigInstance.TopographicMode.roadColor;
        
        return block.BlockMaterial switch
        {
            EnumBlockMaterial.Gravel => ConfigManager.ConfigInstance.TopographicMode.gravelColor,
            EnumBlockMaterial.Sand => ConfigManager.ConfigInstance.TopographicMode.sandColor,
            EnumBlockMaterial.Soil => ConfigManager.ConfigInstance.TopographicMode.soilColor,
            EnumBlockMaterial.Stone => ConfigManager.ConfigInstance.TopographicMode.stoneColor,
            EnumBlockMaterial.Ice => ConfigManager.ConfigInstance.TopographicMode.iceColor,
            EnumBlockMaterial.Snow => ConfigManager.ConfigInstance.TopographicMode.snowColor,
            EnumBlockMaterial.Liquid => ConfigManager.ConfigInstance.TopographicMode.waterColor,
            _ => ConfigManager.ConfigInstance.errorColor,
        };
    }

    private bool IsRoad(Block block)
        => _roadBlocks.Any(b => block.Code.ToString().Contains(b));
}