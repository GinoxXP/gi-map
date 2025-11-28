using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class PrecipitationMapLayer : AMapLayer<PrecipitationMultiChunkMapComponent>
{
    private readonly int _veryHighPrecipitation = ColorUtil.ColorFromRgba(70, 130, 230, 255);
    private readonly int _highPrecipitation = ColorUtil.ColorFromRgba(40, 175, 175, 255);
    private readonly int _mediumPrecipitation = ColorUtil.ColorFromRgba(110, 190, 110, 255);
    private readonly int _lowPrecipitation = ColorUtil.ColorFromRgba(190, 190, 100, 255);
    private readonly int _veryLowPrecipitation = ColorUtil.ColorFromRgba(240, 230, 160, 255);
 
    public override string Title => MapTypes.Precipitation;
    
    public PrecipitationMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override PrecipitationMultiChunkMapComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out PrecipitationMultiChunkMapComponent mccomp))
            _loadedMapData[mcord] = mccomp = new PrecipitationMultiChunkMapComponent(api as ICoreClientAPI, baseCord, this);

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

        var pos = new BlockPos(0);
        int baseWorldX = chunkPos.X * 32;
        int baseWorldZ = chunkPos.Y * 32;
        
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
            
            int localX = k % 32;
            int localZ = k / 32;
            
            pos.Set(baseWorldX + localX, topBlockHeight, baseWorldZ + localZ);
            var precipitation = api.World.BlockAccessor.GetClimateAt(pos).WorldgenRainfall;
            
            resultPixelArray[k] = GetColor(precipitation);
        }
        
        for (var n = 0; n < _chunksTmp.Length; n++)
            _chunksTmp[n] = null;
            
        return resultPixelArray;
    }

    private int GetColor(float precipitation)
    {
        if (precipitation < 0.15f) return _veryLowPrecipitation; 
        if (precipitation < 0.45f) return _lowPrecipitation; 
        if (precipitation < 0.70f) return _mediumPrecipitation; 
        if (precipitation < 0.90f) return _highPrecipitation;

        return _veryHighPrecipitation; 
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
}