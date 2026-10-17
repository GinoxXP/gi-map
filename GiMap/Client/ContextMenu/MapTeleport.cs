using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client.ContextMenu;

public static class MapTeleport
{
    /// <summary>
    /// Teleports the player onto the surface at the column of <paramref name="worldPos"/> with the /tp command.
    /// The surface is only known for chunks loaded on the client, farther points go to the sea level
    /// </summary>
    public static void Teleport(ICoreClientAPI capi, Vec3d worldPos)
    {
        var x = (int)Math.Floor(worldPos.X);
        var z = (int)Math.Floor(worldPos.Z);

        var blockAccessor = capi.World.BlockAccessor;
        var y = blockAccessor.GetMapChunkAtBlockPos(new BlockPos(x, 0, z)) != null
            ? blockAccessor.GetRainMapHeightAt(x, z) + 1
            : capi.World.SeaLevel;

        // "=" marks absolute coordinates, otherwise x and z are relative to the world spawn
        capi.SendChatMessage(string.Format(CultureInfo.InvariantCulture, "/tp ={0} ={1} ={2}", x + 0.5, y, z + 0.5));
    }
}
