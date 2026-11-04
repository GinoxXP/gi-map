using HarmonyLib;
using Vintagestory.API.Common;

namespace GiMap.Terrain;

/// <summary>
/// Add-on that draws real block colors on the vanilla terrain map, see <see cref="Patches.ColorAccurateTerrainPatch"/>.
/// It is a separate mod, so servers can forbid it by its mod id and still allow the core GiMap.
/// </summary>
public class TerrainModSystem : ModSystem
{
    private const string PatchId = "gimapterrain";

    private Harmony? _harmony;

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    // After GiMapModSystem
    public override double ExecuteOrder() => base.ExecuteOrder() + 0.01;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);

        // The core PatchAll only patches the core assembly, so the add-on patches its own.
        // Applied before the map layers are created, like the core.
        if (!Harmony.HasAnyPatches(PatchId))
        {
            _harmony = new Harmony(PatchId);
            _harmony.PatchAll(typeof(TerrainModSystem).Assembly);
        }
    }

    public override void Dispose()
    {
        _harmony?.UnpatchAll(PatchId);
        base.Dispose();
    }
}
