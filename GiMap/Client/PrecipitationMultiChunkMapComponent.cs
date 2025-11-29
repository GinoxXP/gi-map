using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class PrecipitationMultiChunkMapComponent : AChunkMapComponent
{
    public PrecipitationMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, PrecipitationMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}