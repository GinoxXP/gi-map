using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class LightChunkMapComponent : AChunkMapComponent<LightMapLayer>
{
    public LightChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, LightMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}