using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class TopographicMultiChunkMapComponent : AChunkMapComponent<TopographicMapLayer>
{
    public TopographicMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, TopographicMapLayer mapLayer): base(capi, baseChunkCord, mapLayer)
    {
    }
}