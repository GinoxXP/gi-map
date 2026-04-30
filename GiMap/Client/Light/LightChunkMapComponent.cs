using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client.Light;

public class LightChunkMapComponent : AChunkMapComponent
{
    public LightChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, LightMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}