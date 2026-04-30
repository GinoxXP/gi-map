using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Light;

public class LightChunkMapComponent : AChunkMapComponent
{
    public LightChunkMapComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, LightMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
    }

    public override void OnMouseMove(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
    }
}