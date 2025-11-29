using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class TemperatureChunkMapComponent : AChunkMapComponent
{
    public TemperatureChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, TemperatureMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}