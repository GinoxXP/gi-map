using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class HeightMultiChunkMapComponent : AChunkMapComponent<HeightMapLayer>
{
    public HeightMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, HeightMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}