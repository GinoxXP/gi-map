using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class TopographicMultiChunkMapComponent : AChunkMapComponent
{
    public TopographicMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, TopographicMapLayer mapLayer): base(capi, baseChunkCord, mapLayer)
    {
    }
}