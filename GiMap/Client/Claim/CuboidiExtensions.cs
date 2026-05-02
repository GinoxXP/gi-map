using Vintagestory.API.MathTools;

namespace GiMap.Client.Claim;

public static class CuboidiExtensions
{
    public static bool ContainsXZ(this Cuboidi c, BlockPos pos)
    {
        return pos.X >= c.MinX && pos.X < c.MaxX && pos.Z >= c.MinZ && pos.Z < c.MaxZ;
    }
    
    public static BlockFacing GetBoundaryFacingXZ(this Cuboidi c, BlockPos pos)
    {
        if (pos.X == c.MinX)
            return BlockFacing.WEST;
    
        if (pos.X == c.MaxX - 1)
            return BlockFacing.EAST;
    
        if (pos.Z == c.MinZ)
            return BlockFacing.NORTH;
    
        if (pos.Z == c.MaxZ - 1)
            return BlockFacing.SOUTH;

        return null;
    }
}