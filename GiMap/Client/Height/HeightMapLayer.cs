using System.Text;
using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Height;

public class HeightMapLayer : AMapLayer
{
    public override string Title => MapTypes.Height;
    
    private int _maxHeight;
    private int _seaLevel;
    private int _discriditationRate = 5;

    private readonly string[] _waterBlocks = new[] { "game:water-", "game:saltwater-", "game:boilingwater-"};

    private readonly Vec4i _mountainColor;
    private readonly Vec4i _plainColor;
    private readonly Vec4i _lowLandColor;
    private readonly Vec4i _seaLevelWaterColor;
    private readonly Vec4i _highWaterColor;
    private readonly Vec4i _trenchColor;

    private bool[] _isWaterByBlockId = Array.Empty<bool>();

    public HeightMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        _maxHeight = _capi.World.MapSizeY;
        _seaLevel = _capi.World.SeaLevel;

        var config = ConfigManager.ConfigInstance.HeightMode;
        _mountainColor = config.mountainColor;
        _plainColor = config.plainColor;
        _lowLandColor = config.lowLandColor;
        _seaLevelWaterColor = config.seaLevelWaterColor;
        _highWaterColor = config.highWaterColor;
        _trenchColor = config.trenchColor;
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        var blocks = api.World.Blocks;
        _isWaterByBlockId = new bool[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            _isWaterByBlockId[i] = blocks[i] != null && IsWaterBlockType(blocks[i]);
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new HeightMultiChunkMapComponent(_capi, baseCord, this);

    // Same as the chunk component: no hover text
    protected override void AppendLodHoverText(int color, StringBuilder hoverText)
    {
    }

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
        
        ClearChunks();
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
        => _isWaterByBlockId[block.Id];

    // Called once per block type in OnLoaded, the result is looked up by block id
    private bool IsWaterBlockType(Block block)
    {
        if (block.BlockMaterial != EnumBlockMaterial.Water)
            return false;

        var code = block.Code?.ToString();
        return code != null && _waterBlocks.Any(code.Contains);
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
}