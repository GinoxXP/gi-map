using System.Text;
using GiMap.Client.Height;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class HeightMultiChunkMapComponent : AChunkMapComponent
{
    public HeightMultiChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, HeightMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }
    
    public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
    }
}