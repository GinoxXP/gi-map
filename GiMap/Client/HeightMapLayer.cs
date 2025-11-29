using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class HeightMapLayer : AMapLayer
{
    public override string Title => MapTypes.Height;
    
    private int _maxHeight;
    private int _seaLevel;
    private int _hueOffset = 100;
    private int _discriditationRate = 5;

    private readonly Vec4i _mountainColor = new(255, 0, 0, 255);
    private readonly Vec4i _plainColor = new(0, 255, 0, 255);
    private readonly Vec4i _lowLandColor = new(0, 150, 0, 255);
    private readonly Vec4i _seaLevelWaterColor = new(0, 0, 255, 255);
    private readonly Vec4i _highWaterColor = new(50, 255, 255, 255);
    private readonly Vec4i _trenchColor = new(0, 0, 150, 255);

    private readonly string[] _waterBlocks = new[] { "game:water-", "game:saltwater-", "game:boilingwater-"};

    public HeightMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        _maxHeight = _capi.World.MapSizeY;
        _seaLevel = _capi.World.SeaLevel;
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new HeightMultiChunkMapComponent(_capi, baseCord, this);

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
            
            if (IsWater(block))
            {
                while (topBlockHeight > 0 && IsWater(block))
                {
                    topBlockHeight--;
                    topChunkIndex = topBlockHeight / 32;
                    index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
                    block = api.World.Blocks[index];
                }
                
                resultPixelArray[k] = GetColor(topBlockHeight, _highWaterColor, _seaLevelWaterColor, _trenchColor);
            }
            else
            {
                resultPixelArray[k] = GetColor(topBlockHeight, _mountainColor, _plainColor, _lowLandColor);
            }
        }
        
        for (var n = 0; n < _chunksTmp.Length; n++)
            _chunksTmp[n] = null;
            
        return resultPixelArray;
    }

    private int GetColor(int height, Vec4i colorAboveSeaLevel, Vec4i colorSeaLevel ,Vec4i colorBelowSeaLevel)
    {
        float InverseLerp(float a, float b, float value)
        {
            if (Math.Abs(a - b) < 0.1f)
                return 0f;

            return (value - a) / (b - a);
        }
        
        var discreditationHeight = Discreditation(height, _discriditationRate);
        

        var color = 0;
        if (height >= _seaLevel)
        {
            var progress = 1 - InverseLerp(_seaLevel, _maxHeight, discreditationHeight);
            color = MixColor(colorAboveSeaLevel, colorSeaLevel, progress);
        }
        else
        {
            var progress = 1 - InverseLerp(0, _seaLevel, discreditationHeight);
            color = MixColor(colorSeaLevel,colorBelowSeaLevel, progress);
        }
        return color;
    }

    private int MixColor(Vec4i colorA, Vec4i colorB, float progress)
    {
        int MixChanel(int chanelA, int chanelB, float progress)
            => (int)float.Lerp(chanelA, chanelB, progress);
        
        return ColorUtil.ColorFromRgba(
            MixChanel(colorA.X, colorB.X, progress),
            MixChanel(colorA.Y, colorB.Y, progress),
            MixChanel(colorA.Z, colorB.Z, progress),
            MixChanel(colorA.W, colorB.W, progress));
    }
    
    private int Discreditation(int value, int rate)
        => value / rate * rate;

    private bool IsWater(Block block)
    {
        if (block.BlockMaterial == EnumBlockMaterial.Liquid)
            return _waterBlocks.Any(b => block.Code.ToString().Contains(b));
        
        return false;
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