using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace GiMap.Client.Claim;

public class ClaimComponent : AChunkMapComponent
{
    private readonly ClaimMapLayer _claimMapLayer;
    
    public ClaimComponent(ICoreClientAPI capi, FastVec2i baseChunkCord, AMapLayer mapLayer) : base(capi, baseChunkCord, mapLayer)
    {
        _claimMapLayer = mapLayer as ClaimMapLayer;
    }

    protected override void SetHoverText(StringBuilder hoverText)
    {
        var claim = _claimMapLayer.GetClaim(_mouseWorldPos.AsBlockPos);

        if (claim == null)
        {
            hoverText.Append(Lang.Get("na"));
            return;
        }
        
        hoverText.AppendLine(claim.LastKnownOwnerName);
    }
}