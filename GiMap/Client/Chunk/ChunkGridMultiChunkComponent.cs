using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Chunk;

public class ChunkGridMultiChunkComponent : AChunkMapComponent
{
    public ChunkGridMultiChunkComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, ChunkGridMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
    
    public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
    }
}