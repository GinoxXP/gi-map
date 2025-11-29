using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class FertilityMultiChunkMapComponent : AChunkMapComponent
{
    public FertilityMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, FertilityMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}