using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class OreMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.Ore;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new OreChunkMapComponent(_capi, baseCord, this);

    protected override bool IsBlockValid(Block block)
        => block.BlockMaterial == EnumBlockMaterial.Ore;
    

    protected override int GetColor(BlockPos pos)
    {
        var block = api.World.BlockAccessor.GetBlock(pos);
        return block.GetColor(_capi, pos) | -16777216;
    }
}