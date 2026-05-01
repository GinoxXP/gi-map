using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace GiMap.Client.Claim;

public class ClaimMapLayer : ABlockMapLayer
{
    private List<LandClaim> _claims = new();
    
    public override string Title => MapTypes.Claim;
    
    public ClaimMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        _capi.World.RegisterGameTickListener(UpdateClaimRadar, 1000);
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new ClaimComponent(_capi, baseCord, this);

    protected override int GetColor(BlockPos pos, Block block)
    {
        var claim = GetClaim(pos);
        
        if (claim == null)
            return 0;
        
        return OwnerNameToColor(claim.LastKnownOwnerName);
    }

    public LandClaim? GetClaim(BlockPos pos)
    {
        return _claims
            .ToArray()
            .Where(c => c.Areas.Any(a => a.ContainsXZ(pos)))
            .OrderBy(c => -c.Center.Y)
            .FirstOrDefault();
    }
    
    private void UpdateClaimRadar(float dt) 
    {
        GenerateListShowClaim();
    }
    
    private void GenerateListShowClaim() 
    {
        _claims.Clear();
        List<LandClaim> allClaims = _capi.World.Claims.All.ToList();
        foreach (LandClaim claim in allClaims) 
        {
            foreach (Cuboidi area in claim.Areas)
            {
                if (DistanceXZTo(_capi.World.Player.Entity.Pos, area.Center.ToBlockPos()) < ClientSettings.ViewDistance ||
                    DistanceXZTo(_capi.World.Player.Entity.Pos, area.Start.ToBlockPos()) < ClientSettings.ViewDistance ||
                    DistanceXZTo(_capi.World.Player.Entity.Pos, area.End.ToBlockPos()) < ClientSettings.ViewDistance)
                {
                    _claims.Add(claim);
                    break;
                }
            } 
        }
    }
    
    private double DistanceXZTo(EntityPos player, BlockPos cube) 
        => Math.Sqrt(Math.Pow(player.X - cube.X, 2) + Math.Pow(player.Z - cube.Z, 2));
    
    private static int OwnerNameToColor(string input)
    {
        const uint firstPrime = 328514948;
        const uint secondPrime = 221669321;
        const uint thirdPrime = 301287251;

        const uint FNVOffsetBasis = 2166136261;

        uint hashR = FNVOffsetBasis;
        uint hashG = FNVOffsetBasis;
        uint hashB = FNVOffsetBasis;
        foreach (byte c in input)
        {
            hashR *= firstPrime;
            hashR ^= c;
            hashG *= secondPrime;
            hashG ^= c;
            hashB *= thirdPrime;
            hashB ^= c;
        }

        return ColorUtil.ToRgba(255, (int)(hashR % 256), (int)(hashG % 256), (int)(hashB % 256));
    }
}