using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class TemporalStabilityMultiChunkComponent : AChunkMapComponent
{
    public TemporalStabilityMultiChunkComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, AMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}