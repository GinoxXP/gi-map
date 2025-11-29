using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class OreMapLayer : ABlockMapLayer<OreChunkMapComponent>
{
    public override string Title => MapTypes.Ore;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    protected override OreChunkMapComponent CreateComponent(FastVec2i mcord, FastVec2i baseCord)
    {
        if (!_loadedMapData.TryGetValue(mcord, out OreChunkMapComponent mccomp))
            _loadedMapData[mcord] = mccomp = new OreChunkMapComponent(api as ICoreClientAPI, baseCord, this);

        return mccomp;
    }

    protected override bool IsBlockValid(Block block)
        => block.BlockMaterial == EnumBlockMaterial.Ore;
    

    protected override int GetColor(BlockPos pos)
    {
        var block = api.World.BlockAccessor.GetBlock(pos);
        return block.GetColor(_capi, pos) | -16777216;
    }
}