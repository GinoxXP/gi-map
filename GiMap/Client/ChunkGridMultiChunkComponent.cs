using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client;

public class ChunkGridMultiChunkComponent : AChunkMapComponent
{
    public ChunkGridMultiChunkComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, ChunkGridMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
}