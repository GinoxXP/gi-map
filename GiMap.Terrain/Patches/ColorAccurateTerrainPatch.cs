using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GiMap.Terrain.Patches;

/// <summary>
/// Real block colors on the vanilla terrain map: forces the vanilla color-accurate mode of <see cref="ChunkMapLayer"/>
/// (texture color with climate tint and relief shading). Vanilla enables it only by the world config or the
/// player privilege <c>colorAccurateWorldmap</c> and reassigns the flag in the constructor and in
/// <c>OnMapOpenedClient</c>. The pixels go to the same vanilla map db, so the core terrain LOD gets them too.
/// </summary>
static class ColorAccurateTerrainPatch
{
    private static readonly AccessTools.FieldRef<ChunkMapLayer, bool>? ColorAccurate;

    // False when the game changed the private field, the terrain layer is then left as is
    private static readonly bool IsSupported;

    private static bool _warned;

    static ColorAccurateTerrainPatch()
    {
        try
        {
            ColorAccurate = AccessTools.FieldRefAccess<ChunkMapLayer, bool>("colorAccurate");
            IsSupported = true;
        }
        catch (Exception)
        {
            IsSupported = false;
        }
    }

    private static void Apply(ChunkMapLayer layer, ICoreAPI api)
    {
        if (api is not ICoreClientAPI)
            return;

        if (!IsSupported)
        {
            if (!_warned)
            {
                _warned = true;
                api.Logger.Warning("[gimapterrain] Unknown terrain map layer fields, real colors are disabled");
            }
            return;
        }

        ColorAccurate!(layer) = true;
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnLoaded")]
    static class OnLoadedPatch
    {
        static void Postfix(ChunkMapLayer __instance, ICoreAPI ___api) => Apply(__instance, ___api);
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnMapOpenedClient")]
    static class OnMapOpenedClientPatch
    {
        static void Postfix(ChunkMapLayer __instance, ICoreAPI ___api) => Apply(__instance, ___api);
    }
}
