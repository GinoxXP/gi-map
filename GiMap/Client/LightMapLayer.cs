using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class LightMapLayer : ABlockMapLayer
{
    public override string Title => MapTypes.Light;
    
    public LightMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }
    
    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new LightChunkMapComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos)
    {
        var light = api.World.BlockAccessor.GetLightRGBs(pos);
        
        var r = (int)(light.R * 255);
        var g = (int)(light.G * 255);
        var b = (int)(light.B * 255);
        var a = 255;
        
        var color = ColorUtil.ColorFromRgba(r, g, b, a);
        return color;
    }
}