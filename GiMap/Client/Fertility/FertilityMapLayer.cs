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
    
    public FertilityMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.veryLowFertilityColor), Lang.Get("very-low"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.lowFertilityColor), Lang.Get("low"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.mediumFertilityColor), Lang.Get("medium"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.highFertilityColor), Lang.Get("high"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.extreameFertilityColor), Lang.Get("very-high"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.waterColor), Lang.Get("water"));
        FillDictionary(ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.noFertilityColor), Lang.Get("fertility-no"));
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new FertilityMultiChunkMapComponent(Capi, baseCord, this);
    
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
            if (topChunkIndex >= ChunksTmp.Length)
                continue;

            MapUtil.PosInt2d(k, 32L, vec2i);
            int index = ChunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
            Block block = api.World.Blocks[index];

            while (topBlockHeight > 0 && !IsBlockValid(block))
            {
                topBlockHeight--;
                topChunkIndex = topBlockHeight / 32;
                index = ChunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
                block = api.World.Blocks[index];
            }

            resultPixelArray[k] = GetMaterialColor(block);
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
    
    private int GetMaterialColor(Block block)
    {
        if (IsFertilityGroup(block, _veryLowFertilityBlocks))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.veryLowFertilityColor);
        
        if (IsFertilityGroup(block, _lowFertilityBlocks))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.lowFertilityColor);
        
        if (IsFertilityGroup(block, _mediumFertilityBlocks))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.mediumFertilityColor);
        
        if (IsFertilityGroup(block, _highFertilityBlocks))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.highFertilityColor);
        
        if (IsFertilityGroup(block, _extreameFertilityBlocks))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.extreameFertilityColor);
        
        if (block.BlockMaterial == EnumBlockMaterial.Water)
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.waterColor);
        
        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.FertilityMode.noFertilityColor);
    }
    
    private bool IsFertilityGroup(Block block, string[] fertilityGroup)
        => fertilityGroup.Any(b => block.Code.ToString().Contains(b));
}