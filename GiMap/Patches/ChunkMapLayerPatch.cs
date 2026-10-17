using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using GiMap.Client.Lod;
using GiMap.Client.Storage;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Patches;

/// <summary>
/// LOD for the vanilla terrain layer: on far zoom it draws LOD tiles and stops loading chunk by chunk,
/// otherwise it keeps loading every visible chunk even while a mode of this mod is shown.
/// </summary>
static class ChunkMapLayerPatch
{
    private static readonly AccessTools.FieldRef<ChunkMapLayer, ICoreClientAPI>? Capi;
    private static readonly AccessTools.FieldRef<ChunkMapLayer, MapDB>? MapDb;
    private static readonly AccessTools.FieldRef<ChunkMapLayer, HashSet<FastVec2i>>? CurVisibleChunks;
    private static readonly AccessTools.FieldRef<ChunkMapLayer, UniqueQueue<FastVec2i>>? ChunksToGen;
    private static readonly AccessTools.FieldRef<ChunkMapLayer, object>? ChunksToGenLock;
    private static readonly AccessTools.FieldRef<ChunkMapLayer, ConcurrentDictionary<FastVec2i, MultiChunkMapComponent>>? LoadedMapData;
    private static readonly AccessTools.FieldRef<ChunkMapLayer, Dictionary<FastVec2i, MapPieceDB>>? ToSaveList;

    private static readonly ConditionalWeakTable<ChunkMapLayer, State> States = new();

    // False when the game changed the private fields, the terrain layer is then left as is
    private static readonly bool IsSupported;

    static ChunkMapLayerPatch()
    {
        try
        {
            Capi = AccessTools.FieldRefAccess<ChunkMapLayer, ICoreClientAPI>("capi");
            MapDb = AccessTools.FieldRefAccess<ChunkMapLayer, MapDB>("mapdb");
            CurVisibleChunks = AccessTools.FieldRefAccess<ChunkMapLayer, HashSet<FastVec2i>>("curVisibleChunks");
            ChunksToGen = AccessTools.FieldRefAccess<ChunkMapLayer, UniqueQueue<FastVec2i>>("chunksToGen");
            ChunksToGenLock = AccessTools.FieldRefAccess<ChunkMapLayer, object>("chunksToGenLock");
            LoadedMapData = AccessTools.FieldRefAccess<ChunkMapLayer, ConcurrentDictionary<FastVec2i, MultiChunkMapComponent>>("loadedMapData");
            ToSaveList = AccessTools.FieldRefAccess<ChunkMapLayer, Dictionary<FastVec2i, MapPieceDB>>("toSaveList");
            IsSupported = true;
        }
        catch (Exception)
        {
            IsSupported = false;
        }
    }

    private static State? GetState(ChunkMapLayer layer)
        => States.TryGetValue(layer, out var state) ? state : null;

    private static readonly System.Func<ChunkMapLayer, TextCommandCallingArgs?, TextCommandResult>? RedrawCmd =
        AccessTools.Method(typeof(ChunkMapLayer), "OnMapCmdRedraw") is { } method
            ? AccessTools.MethodDelegate<System.Func<ChunkMapLayer, TextCommandCallingArgs?, TextCommandResult>>(method)
            : null;

    private static ChunkMapLayer? FindLayer(ICoreClientAPI capi)
        => capi.ModLoader.GetModSystem<WorldMapManager>()?.MapLayers.OfType<ChunkMapLayer>().FirstOrDefault();

    /// <summary>
    /// Map db files of the terrain layer that are kept open, they can only be cleared by <see cref="ClearStorage"/>
    /// </summary>
    public static IEnumerable<string> GetOpenDbFiles(ICoreClientAPI capi)
    {
        var layer = FindLayer(capi);
        if (layer == null)
            yield break;

        yield return Path.Combine(GamePaths.DataPath, "Maps", capi.World.SavegameIdentifier + ".db");
        if (GetState(layer) is { } state)
            yield return state.LodPath;
    }

    /// <summary>
    /// Deletes the explored terrain map with its LOD tiles and redraws it. Main thread.
    /// </summary>
    /// <returns>False when the terrain layer cannot be cleared on this game version</returns>
    public static bool ClearStorage(ICoreClientAPI capi)
    {
        var layer = FindLayer(capi);
        if (layer == null)
            return true;

        var state = GetState(layer);
        if (state == null || RedrawCmd == null)
            return false;

        // The vanilla db is used by the map generation thread, so it is cleared there
        state.ClearRequested = true;
        RedrawCmd(layer, null);
        state.Lod.RequestPurge(shrink: true);
        return true;
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnLoaded")]
    static class OnLoadedPatch
    {
        static void Postfix(ChunkMapLayer __instance, ICoreAPI ___api)
        {
            if (___api is not ICoreClientAPI capi)
                return;

            if (!IsSupported)
            {
                capi.Logger.Warning("[gimap] Unknown terrain map layer fields, LOD for the terrain layer is disabled");
                return;
            }

            var dir = Path.Combine(GamePaths.DataPath, "Maps");
            GamePaths.EnsurePathExists(dir);
            var path = Path.Combine(dir, capi.World.SavegameIdentifier + "-terrain-lod.db");

            var db = new MapDB(capi.Logger);
            string? errorMessage = null;
            db.OpenOrCreate(path, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
            if (errorMessage != null)
            {
                capi.Logger.Warning("[gimap] Cannot open {0}, LOD for the terrain layer is disabled: {1}", path, errorMessage);
                db.Dispose();
                return;
            }

            var lod = new LodTileSet(capi, db, cords => ChunkPieceReader.Read(MapDb!(__instance), ToSaveList!(__instance), cords), LodColors.Average);
            States.AddOrUpdate(__instance, new State(lod, db, path));
        }
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnViewChangedClient")]
    static class OnViewChangedClientPatch
    {
        static bool Prefix(ChunkMapLayer __instance, ICoreAPI ___api, List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
        {
            var state = GetState(__instance);
            if (state == null)
                return true;

            // Full visible set, vanilla one is emptied on LOD zoom so its per-chunk textures expire
            foreach (var cord in nowVisible)
                state.Visible.Add(cord);
            foreach (var cord in nowHidden)
                state.Visible.Remove(cord);

            bool isLodZoom = LodState.Level > 0;
            var curVisibleChunks = CurVisibleChunks!(__instance);
            var chunksToGen = ChunksToGen!(__instance);

            if (!isLodZoom)
            {
                if (state.WasLodZoom)
                {
                    state.WasLodZoom = false;
                    RestoreVisibleChunks(__instance, state, curVisibleChunks, chunksToGen);
                }
                return true;
            }

            var blockAccessor = ___api.World.BlockAccessor;
            lock (ChunksToGenLock!(__instance))
            {
                if (!state.WasLodZoom)
                {
                    state.WasLodZoom = true;
                    curVisibleChunks.Clear();
                    chunksToGen.Clear();
                }

                // Only chunks loaded around the player are generated, LOD tiles are rebuilt from them
                foreach (var cord in nowVisible)
                {
                    if (blockAccessor.GetMapChunk(cord.X, cord.Y) != null)
                        chunksToGen.Enqueue(cord.Copy());
                }
            }

            return false;
        }

        // The view change only reports chunks that were not visible before, the ones skipped on LOD zoom are queued here
        private static void RestoreVisibleChunks(ChunkMapLayer layer, State state, HashSet<FastVec2i> curVisibleChunks, UniqueQueue<FastVec2i> chunksToGen)
        {
            const int chunkLen = 3;
            var loadedMapData = LoadedMapData!(layer);

            curVisibleChunks.Clear();
            curVisibleChunks.UnionWith(state.Visible);

            lock (ChunksToGenLock!(layer))
            {
                foreach (var cord in state.Visible)
                {
                    if (cord.X < 0 || cord.Y < 0)
                        continue;

                    var mccord = new FastVec2i(cord.X / chunkLen, cord.Y / chunkLen);
                    if (loadedMapData.TryGetValue(mccord, out var component) && component.IsChunkSet(cord.X % chunkLen, cord.Y % chunkLen))
                        continue;

                    chunksToGen.Enqueue(cord.Copy());
                }
            }
        }
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "Render")]
    static class RenderPatch
    {
        static bool Prefix(ChunkMapLayer __instance, GuiElementMap mapElem)
        {
            var state = GetState(__instance);
            if (state == null || !__instance.Active)
                return true;

            int level = LodState.LevelForZoom(mapElem.ZoomLevel);
            if (level == 0)
                return true;

            state.Lod.UpdateView(mapElem.CurrentBlockViewBounds, level);
            state.Lod.Render(mapElem, null);
            return false;
        }
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnTick")]
    static class OnTickPatch
    {
        static void Postfix(ChunkMapLayer __instance, float dt)
            => GetState(__instance)?.Lod.MainThreadTick(dt);
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnOffThreadTick")]
    static class OnOffThreadTickPatch
    {
        static void Prefix(ChunkMapLayer __instance, ICoreAPI ___api)
        {
            var state = GetState(__instance);
            if (state == null || !state.ClearRequested)
                return;

            state.ClearRequested = false;
            ToSaveList!(__instance).Clear();
            MapDbUtil.Clear(MapDb!(__instance), ___api.Logger);
        }

        static void Postfix(ChunkMapLayer __instance)
            => GetState(__instance)?.Lod.OffThreadTick(__instance.Active && LodState.Level > 0);
    }

    // Vanilla generates a piece whenever the chunk is loaded on the client, otherwise it was read from the db
    [HarmonyPatch(typeof(ChunkMapLayer), "loadFromChunkPixels")]
    static class LoadFromChunkPixelsPatch
    {
        static void Postfix(ChunkMapLayer __instance, ICoreAPI ___api, FastVec2i cord)
        {
            var state = GetState(__instance);
            if (state != null && ___api.World.BlockAccessor.GetMapChunk(cord.X, cord.Y) != null)
                state.Lod.Invalidate(cord);
        }
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnMapClosedClient")]
    static class OnMapClosedClientPatch
    {
        static void Postfix(ChunkMapLayer __instance)
        {
            var state = GetState(__instance);
            if (state == null)
                return;

            state.Lod.CancelRequests();
            state.Visible.Clear();
            state.WasLodZoom = false;
        }
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnMapCmdRedraw")]
    static class OnMapCmdRedrawPatch
    {
        static void Postfix(ChunkMapLayer __instance)
            => GetState(__instance)?.Lod.RequestPurge();
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "Dispose")]
    static class DisposePatch
    {
        static void Postfix(ChunkMapLayer __instance)
            => GetState(__instance)?.Lod.Dispose();
    }

    [HarmonyPatch(typeof(ChunkMapLayer), "OnShutDown")]
    static class OnShutDownPatch
    {
        static void Postfix(ChunkMapLayer __instance)
        {
            var state = GetState(__instance);
            if (state == null)
                return;

            state.Db.Dispose();
            States.Remove(__instance);
        }
    }

    private sealed class State
    {
        public readonly LodTileSet Lod;
        public readonly MapDB Db;
        public readonly HashSet<FastVec2i> Visible = new();
        public bool WasLodZoom;
        public readonly string LodPath;
        public volatile bool ClearRequested;

        public State(LodTileSet lod, MapDB db, string lodPath)
        {
            Lod = lod;
            Db = db;
            LodPath = lodPath;
        }
    }
}
