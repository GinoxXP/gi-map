using System.Collections.Concurrent;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public abstract class AMapLayer : MapLayer
{
    public override string LayerGroupCode => Title;
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    
    protected ICoreClientAPI _capi;
    private MapDB _mapdb;
    protected IWorldChunk[] _chunksTmp;
    private int _chunksize;
    private float _mtThread1secAccum;
    private float _genAccum;
    private float _diskSaveAccum;
    private object _chunksToGenLock = new();
    private UniqueQueue<FastVec2i> _chunksToGen = new();
    private HashSet<FastVec2i> _curVisibleChunks = new();
    private ConcurrentQueue<ReadyMapPiece> _readyMapPieces = new();
    private Dictionary<FastVec2i, MapPieceDB> _toSaveList = new();
    protected ConcurrentDictionary<FastVec2i, AChunkMapComponent> _loadedMapData = new();
    
    public Vec4f OverlayColor { get; protected set; } = new Vec4f(1, 1, 1, 1);

    protected AMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        _chunksize = api.World.BlockAccessor.ChunkSize;

        OverlayColor.A = ConfigManager.ConfigInstance.overlayAlpha;
        
        api.Event.ChunkDirty += OnChunkDirty;
        
        Active = false;
        
        _capi = api as ICoreClientAPI;

        if (api.Side == EnumAppSide.Client)
        {
            api.World.Logger.Notification("Loading world map cache db...");
            _mapdb = new MapDB(api.World.Logger);
            string errorMessage = null;
            string mapDbFilePath = GetMapDbFilePath();
            _mapdb.OpenOrCreate(mapDbFilePath, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
            if (errorMessage != null)
                throw new Exception($"Cannot open {mapDbFilePath}, possibly corrupted. Please fix manually or delete this file to continue playing");

            api.ChatCommands.GetOrCreate($"{Title}map").BeginSubCommand("purgedb").WithDescription("purge the map db")
                .HandleWith(delegate
                {
                    _mapdb.Purge();
                    return TextCommandResult.Success("Ok, db purged");
                })
                .EndSubCommand()
                .BeginSubCommand("redraw")
                .WithDescription("Redraw the map")
                .HandleWith(OnMapCmdRedraw)
                .EndSubCommand();
        }
    }
    
    public override void OnTick(float dt)
    {
        if (!_readyMapPieces.IsEmpty)
        {
            int q = Math.Min(_readyMapPieces.Count, 200);
            List<AChunkMapComponent> modified = new();
            while (q-- > 0)
            {
                if (_readyMapPieces.TryDequeue(out var mappiece))
                {
                    var mcord = new FastVec2i(mappiece.Cord.X / MultiChunkMapComponent.ChunkLen, mappiece.Cord.Y / MultiChunkMapComponent.ChunkLen);
                    var baseCord = new FastVec2i(mcord.X * MultiChunkMapComponent.ChunkLen, mcord.Y * MultiChunkMapComponent.ChunkLen);
                    
                    if (!_loadedMapData.TryGetValue(mcord, out var mccomp))
                        _loadedMapData[mcord] = mccomp = CreateComponent(baseCord);
                    
                    mccomp.setChunk(mappiece.Cord.X - baseCord.X, mappiece.Cord.Y - baseCord.Y, mappiece.Pixels);
                    modified.Add(mccomp);
                }
            }

            foreach (var mccomp in modified) mccomp.FinishSetChunks();
        }

        _mtThread1secAccum += dt;
        if (_mtThread1secAccum > 1)
        {
            List<FastVec2i> toRemove = new List<FastVec2i>();

            foreach (var val in _loadedMapData)
            {
                MultiChunkMapComponent mcmp = val.Value;

                if (!mcmp.AnyChunkSet || !mcmp.IsVisible(_curVisibleChunks))
                {
                    mcmp.TTL -= 1;

                    if (mcmp.TTL <= 0)
                    {
                        FastVec2i mccord = val.Key;
                        toRemove.Add(mccord);
                        mcmp.ActuallyDispose();
                    }
                }
                else
                {
                    mcmp.TTL = MultiChunkMapComponent.MaxTTL;
                }
            }

            foreach (var val in toRemove)
                _loadedMapData.TryRemove(val, out _);


            _mtThread1secAccum = 0;
        }
    }

    public override void OnLoaded()
    {
        if (api.Side == EnumAppSide.Server)
            return;

        _chunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / 32];
    }
    
    public override void ComposeDialogExtras(GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
    {
        string key = "worldmap-layer-" + LayerGroupCode;

        ElementBounds dlgBounds =
                ElementStdBounds.AutosizedMainDialog
                    .WithFixedPosition(
                        (compo.Bounds.renderX + compo.Bounds.OuterWidth) / RuntimeEnv.GUIScale + 10,
                        (compo.Bounds.renderY + compo.Bounds.OuterHeight) / RuntimeEnv.GUIScale - 95
                    )
                    .WithAlignment(EnumDialogArea.None)
            ;

        ElementBounds row = ElementBounds.Fixed(0, 0, 160, 25);

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChild(row);


        guiDialogWorldMap.Composers[key] =
            _capi.Gui
                .CreateCompo(key, dlgBounds)
                .AddShadedDialogBG(bgBounds, false)
                .AddDialogTitleBar(Lang.Get("maplayer-"+LayerGroupCode), () => { guiDialogWorldMap.Composers[key].Enabled = false; })
                .BeginChildElements(bgBounds)
                .AddSlider((newValue) => { ConfigManager.ConfigInstance.overlayAlpha = newValue / 100.0f; OverlayColor.A = ConfigManager.ConfigInstance.overlayAlpha; return true; }, row = row.BelowCopy(0, 5).WithFixedSize(125, 25), "alpha-slider")
                .EndChildElements()
                .Compose()
            ;

        guiDialogWorldMap.Composers[key].GetSlider("alpha-slider").SetValues((int)(ConfigManager.ConfigInstance.overlayAlpha * 100), 0, 100, 1);

        guiDialogWorldMap.Composers[key].Enabled = true;
    }
    
    public override void OnMapClosedClient()
    {
        lock (_chunksToGenLock)
            _chunksToGen.Clear();
        
        _curVisibleChunks.Clear();
        ConfigManager.SaveModConfig(api);
    }
    
    public override void Dispose()
    {
        if (_loadedMapData != null)
        {
            foreach (var value in _loadedMapData.Values)
            {
                value?.ActuallyDispose();
            }
        }

        MultiChunkMapComponent.DisposeStatic();
        base.Dispose();
    }
    
    public override void OnShutDown()
    {
        MultiChunkMapComponent.tmpTexture?.Dispose();
        _mapdb?.Dispose();
    }
    
    public override void OnOffThreadTick(float dt)
    {
        _genAccum += dt;
        if (_genAccum < 0.1) return;
        _genAccum = 0;

        int quantityToGen = _chunksToGen.Count;
        while (quantityToGen > 0)
        {
            if (mapSink.IsShuttingDown) break;

            quantityToGen--;
            FastVec2i cord;

            lock (_chunksToGenLock)
            {
                if (_chunksToGen.Count == 0) break;
                cord = _chunksToGen.Dequeue();
            }

            if (!api.World.BlockAccessor.IsValidPos(cord.X * _chunksize, 1, cord.Y * _chunksize)) continue;

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
                    _chunksToGen.Enqueue(cord);
                }

                continue;
            }

            _toSaveList[cord.Copy()] = new MapPieceDB() { Pixels = tintedPixels };

            LoadFromChunkPixels(cord, tintedPixels);
        }

        if (_toSaveList.Count > 100 || _diskSaveAccum > 4f)
        {
            _diskSaveAccum = 0;
            _mapdb.SetMapPieces(_toSaveList);
            _toSaveList.Clear();
        }
    }
    
    public override void Render(GuiElementMap mapElem, float dt)
    {
        if (!Active)
        {
            return;
        }

        foreach (KeyValuePair<FastVec2i, AChunkMapComponent> loadedMapDatum in _loadedMapData)
        {
            loadedMapDatum.Value.Render(mapElem, dt);
        }
    }
    
    public override void OnViewChangedClient(List<FastVec2i> nowVisible, List<FastVec2i> nowHidden)
    {
        foreach (var val in nowVisible)
        {
            _curVisibleChunks.Add(val);
        }

        foreach (var val in nowHidden)
        {
            _curVisibleChunks.Remove(val);
        }

        lock (_chunksToGenLock)
        {
            foreach (FastVec2i cord in nowVisible)
            {
                FastVec2i tmpMccoord = new FastVec2i(cord.X / MultiChunkMapComponent.ChunkLen, cord.Y / MultiChunkMapComponent.ChunkLen);

                int dx = cord.X % MultiChunkMapComponent.ChunkLen;
                int dz = cord.Y % MultiChunkMapComponent.ChunkLen;
                if (dx < 0 || dz < 0) continue;

                if (_loadedMapData.TryGetValue(tmpMccoord, out var mcomp))
                {
                    if (mcomp.IsChunkSet(dx, dz)) continue;
                }

                _chunksToGen.Enqueue(cord.Copy());
            }
        }

        foreach (FastVec2i cord in nowHidden)
        {
            if (cord.X < 0 || cord.Y < 0) continue;

            FastVec2i mcord = new FastVec2i(cord.X / MultiChunkMapComponent.ChunkLen, cord.Y / MultiChunkMapComponent.ChunkLen);

            if (_loadedMapData.TryGetValue(mcord, out var mc))
            {
                mc.unsetChunk(cord.X % MultiChunkMapComponent.ChunkLen, cord.Y % MultiChunkMapComponent.ChunkLen);
            }
        }
    }
    
    protected abstract int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc);
    
    protected abstract AChunkMapComponent CreateComponent(FastVec2i baseCord);
    
    private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
    {
        lock (_chunksToGenLock)
        {
            if (!mapSink.IsOpened)
                return;

            var tmpMccoord = new FastVec2i(chunkCoord.X / MultiChunkMapComponent.ChunkLen, chunkCoord.Z / MultiChunkMapComponent.ChunkLen);
            var tmpCoord = new FastVec2i(chunkCoord.X, chunkCoord.Z);

            if (!_loadedMapData.ContainsKey(tmpMccoord) && !_curVisibleChunks.Contains(tmpCoord))
                return;

            _chunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Z));
        }
    }
    
    private string GetMapDbFilePath()
    {
        string text = Path.Combine(GamePaths.DataPath, "Maps");
        GamePaths.EnsurePathExists(text);
        return Path.Combine(text, api.World.SavegameIdentifier + $"-{Title}.db");
    }
    
    private TextCommandResult OnMapCmdRedraw(TextCommandCallingArgs args)
    {
        foreach (var value in _loadedMapData.Values)
        {
            value.ActuallyDispose();
        }

        _loadedMapData.Clear();
        lock (_chunksToGenLock)
        {
            foreach (FastVec2i curVisibleChunk in _curVisibleChunks)
            {
                _chunksToGen.Enqueue(curVisibleChunk.Copy());
            }
        }

        return TextCommandResult.Success("Redrawing map...");
    }
    
    private void LoadFromChunkPixels(FastVec2i cord, int[] pixels)
    {
        _readyMapPieces.Enqueue(new ReadyMapPiece
        {
            Pixels = pixels,
            Cord = cord
        });
    }
}