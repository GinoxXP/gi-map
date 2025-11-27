using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class TopographicMapLayer : AMapLayer<TopographicMultiChunkMapComponent>
{
    public override string Title => "topographic";
    
    private readonly int soilColor = ColorUtil.ColorFromRgba(201, 234, 157, 255);
    private readonly int sandColor = ColorUtil.ColorFromRgba(255, 255, 255, 255);
    private readonly int gravelColor = ColorUtil.ColorFromRgba(200, 200, 200, 255);
    private readonly int stoneColor = ColorUtil.ColorFromRgba(150, 150, 150, 255);
    private readonly int waterColor = ColorUtil.ColorFromRgba(34, 164, 171, 255);
    private readonly int iceColor = ColorUtil.ColorFromRgba(202, 237, 238, 255);
    private readonly int snowColor = ColorUtil.ColorFromRgba(230, 230, 255, 255);
    private readonly int errorColor = ColorUtil.ColorFromRgba(255, 0, 255, 255);
    
    public TopographicMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override TopographicMultiChunkMapComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out TopographicMultiChunkMapComponent mccomp))
            _loadedMapData[mcord] = mccomp = new TopographicMultiChunkMapComponent(api as ICoreClientAPI, baseCord, this);

        return mccomp;
    }
    
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
        return block.BlockMaterial switch
        {
            EnumBlockMaterial.Gravel => gravelColor,
            EnumBlockMaterial.Sand => sandColor,
            EnumBlockMaterial.Soil => soilColor,
            EnumBlockMaterial.Stone => stoneColor,
            EnumBlockMaterial.Ice => iceColor,
            EnumBlockMaterial.Snow => snowColor,
            EnumBlockMaterial.Liquid => waterColor,
            _ => errorColor,
        };
    }
}