using Vintagestory.API.MathTools;

namespace GiMap.Client.Claim;

public static class CuboidiExtensions
{
    public static bool ContainsXZ(this Cuboidi c, BlockPos pos)
    {
        return pos.X >= c.MinX && pos.X < c.MaxX && pos.Z >= c.MinZ && pos.Z < c.MaxZ;
    }
}