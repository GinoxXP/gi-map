using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class OreChunkMapComponent : AChunkMapComponent
{
    public OreChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, OreMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}