using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class GeologyChunkMapComponent : AChunkMapComponent<GeologyActivityMapLayer>
{
    public GeologyChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, GeologyActivityMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}