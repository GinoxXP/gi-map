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
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new ClaimComponent(_capi, baseCord, this);

    public override void OnOffThreadTick(float dt)
    {
        GenerateListShowClaim();

        foreach (var claim in _claims)
        {
            foreach (var area in claim.Areas)
            {
                int minXChunk = area.MinX / _chunksize;
                int maxXChunk = area.MaxX / _chunksize;
                int minZChunk = area.MinZ / _chunksize;
                int maxZChunk = area.MaxZ / _chunksize;

                for (int xChunk = minXChunk; xChunk <= maxXChunk; xChunk++)
                {
                    for (int zChunk = minZChunk; zChunk <= maxZChunk; zChunk++)
                    {
                        var cord = new FastVec2i(xChunk, zChunk);
                        IMapChunk mc = api.World.BlockAccessor.GetMapChunk(cord.X, cord.Y);

                        if (mc == null)
                        {
                            try
                            {
                                MapPieceDB piece = _mapdb.GetMapPiece(cord);
                                if (piece?.Pixels != null)
                                {
                                    LoadFromChunkPixels(cord, piece.Pixels);
                                }
                            }
                            catch (ProtoBuf.ProtoException)
                            {
                                api.Logger.Warning("Failed loading map db section {0}/{1}, a protobuf exception was thrown. Will ignore.", cord.X, cord.Y);
                            }
                            catch (OverflowException)
                            {
                                api.Logger.Warning("Failed loading map db section {0}/{1}, a overflow exception was thrown. Will ignore.", cord.X, cord.Y);
                            }

                            continue;
                        }

                        int[] tintedPixels = GenerateChunkImage(cord, mc);
                        if (tintedPixels == null)
                        {
                            lock (_chunksToGenLock)
                            {
                                _chunksToGen.Enqueue(cord.Copy());
                            }
                            continue;
                        }

                        _toSaveList[cord.Copy()] = new MapPieceDB() { Pixels = tintedPixels };
                        LoadFromChunkPixels(cord, tintedPixels);
                    }
                }
            }
        }

        if (_toSaveList.Count > 100 || _diskSaveAccum > 4f)
        {
            _diskSaveAccum = 0;
            _mapdb.SetMapPieces(_toSaveList);
            _toSaveList.Clear();
        }
    }
    
    protected override int GetColor(BlockPos pos, Block block)
    {
        var claim = GetClaim(pos);
        
        if (claim == null)
            return 0;
        
        return OwnerNameToColor(claim.LastKnownOwnerName);
    }

    public LandClaim? GetClaim(BlockPos pos)
    {
        LandClaim? bestClaim = null;
        int maxY = int.MinValue;

        var claimsSnapshot = _claims.ToArray(); 

        foreach (var claim in claimsSnapshot)
        {
            if (claim?.Areas == null) continue;

            if (claim.Areas.Any(a => a.ContainsXZ(pos)))
            {
                if (claim.Center.Y > maxY)
                {
                    maxY = claim.Center.Y;
                    bestClaim = claim;
                }
            }
        }
        return bestClaim;
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