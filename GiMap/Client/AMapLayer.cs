using System.Collections.Concurrent;
using System.Text;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public abstract class AMapLayer : RGBMapLayer
{
    private readonly HashSet<FastVec2i> _curVisibleChunks = new();
    private readonly ConcurrentQueue<ReadyMapPiece> _readyMapPieces = new();
    private readonly ConcurrentDictionary<FastVec2i, AChunkMapComponent> _loadedMapData = new();
    
    protected readonly ICoreClientAPI Capi;
    protected readonly MapDB Mapdb;
    protected readonly int Chunksize;
    protected readonly object ChunksToGenLock = new();
    protected readonly UniqueQueue<FastVec2i> ChunksToGen = new();
    protected readonly Dictionary<FastVec2i, MapPieceDB> ToSaveList = new();
    protected readonly Dictionary<int, string> LocalizedNameByColor = new();
    
    private float _mtThread1SecAccum;
    private float _genAccum;
    
    protected float DiskSaveAccum;
    protected IWorldChunk[] ChunksTmp;
    
    public override string LayerGroupCode => Title;
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    public override MapLegendItem[] LegendItems => null;
    public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
    public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
    
    public Vec4f OverlayColor { get; } = new Vec4f(1, 1, 1, 1);

    protected AMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        Chunksize = api.World.BlockAccessor.ChunkSize;

        OverlayColor.A = ConfigManager.ConfigInstance.overlayAlpha;
        
        api.Event.ChunkDirty += OnChunkDirty;
        
        Active = false;
        
        Capi = api as ICoreClientAPI;

        if (api.Side == EnumAppSide.Client)
        {
            api.World.Logger.Notification("Loading world map cache db...");
            Mapdb = new MapDB(api.World.Logger);
            string errorMessage = null;
            string mapDbFilePath = GetMapDbFilePath();
            Mapdb.OpenOrCreate(mapDbFilePath, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
            if (errorMessage != null)
                throw new Exception($"Cannot open {mapDbFilePath}, possibly corrupted. Please fix manually or delete this file to continue playing");

            api.ChatCommands.GetOrCreate($"{Title}map").BeginSubCommand("purgedb").WithDescription("purge the map db")
                .HandleWith(delegate
                {
                    Mapdb.Purge();
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

        _mtThread1SecAccum += dt;
        if (_mtThread1SecAccum > 1)
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


            _mtThread1SecAccum = 0;
        }
    }

    public override void OnLoaded()
    {
        if (api.Side == EnumAppSide.Server)
            return;

        ChunksTmp = new IWorldChunk[api.World.BlockAccessor.MapSizeY / 32];
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
            Capi.Gui
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
        lock (ChunksToGenLock)
            ChunksToGen.Clear();
        
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
        Mapdb?.Dispose();
    }
    
    public override void OnOffThreadTick(float dt)
    {
        _genAccum += dt;
        DiskSaveAccum += dt;
        if (_genAccum < 0.1) return;
        _genAccum = 0;

        int quantityToGen = ChunksToGen.Count;
        while (quantityToGen > 0)
        {
            if (mapSink.IsShuttingDown) break;

            quantityToGen--;
            FastVec2i cord;

            lock (ChunksToGenLock)
            {
                if (ChunksToGen.Count == 0) break;
                cord = ChunksToGen.Dequeue();
            }

            if (!api.World.BlockAccessor.IsValidPos(cord.X * Chunksize, 1, cord.Y * Chunksize)) continue;

            IMapChunk mc = api.World.BlockAccessor.GetMapChunk(cord.X, cord.Y);
            if (mc == null)
            {
                try
                {
                    MapPieceDB piece = Mapdb.GetMapPiece(cord);
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
                lock (ChunksToGenLock)
                {
                    ChunksToGen.Enqueue(cord);
                }

                continue;
            }

            ToSaveList[cord.Copy()] = new MapPieceDB() { Pixels = tintedPixels };

            LoadFromChunkPixels(cord, tintedPixels);
        }

        if (ToSaveList.Count > 100 || DiskSaveAccum > 4f)
        {
            DiskSaveAccum = 0;
            Mapdb.SetMapPieces(ToSaveList);
            ToSaveList.Clear();
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

        lock (ChunksToGenLock)
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

                ChunksToGen.Enqueue(cord.Copy());
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
    
    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        if (!Active)
            return;

        foreach (KeyValuePair<FastVec2i, AChunkMapComponent> loadedMapDatum in _loadedMapData)
            loadedMapDatum.Value.OnMouseMove(args, mapElem, hoverText);
    }
    
    public virtual string GetLocalizedStringByColor(int color)
        => LocalizedNameByColor.TryGetValue(color, out var name) ? name : Lang.Get("na");
    
    protected abstract int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc);

    protected abstract AChunkMapComponent CreateComponent(FastVec2i baseCord);

    protected bool TryLoadChunks(FastVec2i chunkPos)
    {
        for (var i = 0; i < ChunksTmp.Length; i++)
        {
            ChunksTmp[i] = Capi.World.BlockAccessor.GetChunk(chunkPos.X, i, chunkPos.Y);
            if (ChunksTmp[i] == null || !(ChunksTmp[i] as IClientChunk).LoadedFromServer)
                return false;
        }
        return true;
    }

    protected void ClearChunks()
    {
        for (var i = 0; i < ChunksTmp.Length; i++)
            ChunksTmp[i] = null;
    }

    protected void FillDictionary(int color, string localizedName)
    {
        LocalizedNameByColor.Add(color, localizedName);
    }
    
    private void OnChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
    {
        lock (ChunksToGenLock)
        {
            if (!mapSink.IsOpened)
                return;

            var tmpMccoord = new FastVec2i(chunkCoord.X / MultiChunkMapComponent.ChunkLen, chunkCoord.Z / MultiChunkMapComponent.ChunkLen);
            var tmpCoord = new FastVec2i(chunkCoord.X, chunkCoord.Z);

            if (!_loadedMapData.ContainsKey(tmpMccoord) && !_curVisibleChunks.Contains(tmpCoord))
                return;

            ChunksToGen.Enqueue(new FastVec2i(chunkCoord.X, chunkCoord.Z));
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
        lock (ChunksToGenLock)
        {
            foreach (FastVec2i curVisibleChunk in _curVisibleChunks)
            {
                ChunksToGen.Enqueue(curVisibleChunk.Copy());
            }
        }

        return TextCommandResult.Success("Redrawing map...");
    }
    
    protected void LoadFromChunkPixels(FastVec2i cord, int[] pixels)
    {
        _readyMapPieces.Enqueue(new ReadyMapPiece
        {
            Pixels = pixels,
            Cord = cord
        });
    }
}