using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using GiMap.Client.Lod;
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
    // All map layers are generated on one shared thread, so hidden layers yield to the shown one
    private const double HiddenLayerBudgetMs = 4;
    private static readonly List<AMapLayer> Layers = new();


    public override string LayerGroupCode => Title;
    public override EnumMapAppSide DataSide => EnumMapAppSide.Client;
    public override MapLegendItem[] LegendItems => null;
    public override EnumMinMagFilter MinFilter => EnumMinMagFilter.Linear;
    public override EnumMinMagFilter MagFilter => EnumMinMagFilter.Nearest;
    
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
    private Dictionary<FastVec2i, int[]> _pendingPieces = new();
    private Dictionary<FastVec2i, MapPieceDB> _toSaveList = new();
    private volatile int _retryCount;
    protected ConcurrentDictionary<FastVec2i, AChunkMapComponent> _loadedMapData = new();
    private MapDB? _lodDb;
    private LodTileSet? _lod;
    private bool _wasLodZoom;
    private Vec3d _mouseWorldPos = new();
    private Vec2f _mouseViewPos = new();
    // Changed by Reload, pieces generated with older settings are generated again
    private volatile int _configVersion;
    private int _toSaveConfigVersion;

    protected readonly Dictionary<int, string> LocalizedNameByColor = new();
    
    public Vec4f OverlayColor { get; protected set; } = new Vec4f(1, 1, 1, 1);

    protected AMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        _chunksize = api.World.BlockAccessor.ChunkSize;

        OverlayColor.A = ConfigManager.ConfigInstance.overlayAlpha;
        
        api.Event.ChunkDirty += OnChunkDirty;
        
        Active = false;
        
        _capi = api as ICoreClientAPI;

        lock (Layers)
            Layers.Add(this);

        if (api.Side == EnumAppSide.Client)
        {
            api.World.Logger.Notification("Loading world map cache db...");
            _mapdb = new MapDB(api.World.Logger);
            string errorMessage = null;
            string mapDbFilePath = GetMapDbFilePath();
            _mapdb.OpenOrCreate(mapDbFilePath, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
            if (errorMessage != null)
                throw new Exception($"Cannot open {mapDbFilePath}, possibly corrupted. Please fix manually or delete this file to continue playing");

            if (LodMode == LodMode.Tiles)
            {
                _lodDb = new MapDB(api.World.Logger);
                string lodDbFilePath = GetMapDbFilePath("-lod");
                _lodDb.OpenOrCreate(lodDbFilePath, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
                if (errorMessage != null)
                    throw new Exception($"Cannot open {lodDbFilePath}, possibly corrupted. Please fix manually or delete this file to continue playing");

                _lod = new LodTileSet(_capi, _lodDb, ReadChunkPieces, CombineLodColors, HasLodTexelConversion ? LodTexelToRgba : null);
            }

            api.ChatCommands.GetOrCreate($"{Title}map").BeginSubCommand("purgedb").WithDescription("purge the map db")
                .HandleWith(delegate
                {
                    _mapdb.Purge();
                    _lod?.RequestPurge();
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
        if (!Active)
        {
            // Hidden layer: keep generated pixels in memory, textures are uploaded once the layer is shown
            while (_readyMapPieces.TryDequeue(out var mappiece))
                _pendingPieces[mappiece.Cord] = mappiece.Pixels;
        }
        else if (!_readyMapPieces.IsEmpty || _pendingPieces.Count > 0)
        {
            int q = 200;
            HashSet<AChunkMapComponent> modified = new();

            while (q > 0 && _pendingPieces.Count > 0)
            {
                var pending = _pendingPieces.First();
                _pendingPieces.Remove(pending.Key);
                SetChunkPixels(pending.Key, pending.Value, modified);
                q--;
            }

            while (q-- > 0 && _readyMapPieces.TryDequeue(out var mappiece))
                SetChunkPixels(mappiece.Cord, mappiece.Pixels, modified);

            foreach (var mccomp in modified) mccomp.FinishSetChunks();
        }

        _lod?.MainThreadTick(dt);

        _mtThread1secAccum += dt;
        if (_mtThread1secAccum > 1)
        {
            List<FastVec2i> toRemove = new List<FastVec2i>();
            // Per-chunk textures are not drawn on LOD zoom, so they are let go like hidden ones
            bool isLodZoom = IsLodZoom;

            foreach (var val in _loadedMapData)
            {
                MultiChunkMapComponent mcmp = val.Value;

                if (isLodZoom || !mcmp.AnyChunkSet || !mcmp.IsVisible(_curVisibleChunks))
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
                        (compo.Bounds.renderY + compo.Bounds.OuterHeight) / RuntimeEnv.GUIScale - 95 - ExtraDialogHeight
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
                .AddSlider((newValue) => { ConfigManager.ConfigInstance.overlayAlpha = newValue / 100.0f; OverlayColor.A = ConfigManager.ConfigInstance.overlayAlpha; return true; }, row = row.BelowCopy(0, 5).WithFixedSize(125, 25), "alpha-slider");

        ComposeExtraElements(guiDialogWorldMap.Composers[key], ref row);

        guiDialogWorldMap.Composers[key]
                .EndChildElements()
                .Compose()
            ;

        guiDialogWorldMap.Composers[key].GetSlider("alpha-slider").SetValues((int)(ConfigManager.ConfigInstance.overlayAlpha * 100), 0, 100, 1);

        OnDialogComposed(guiDialogWorldMap.Composers[key]);

        guiDialogWorldMap.Composers[key].Enabled = true;
    }

    /// <summary>
    /// Height of the elements added by <see cref="ComposeExtraElements"/>, the dialog is moved up by it
    /// </summary>
    protected virtual int ExtraDialogHeight => 0;

    /// <summary>
    /// Adds layer specific elements to the layer dialog below the alpha slider
    /// </summary>
    protected virtual void ComposeExtraElements(GuiComposer compo, ref ElementBounds row)
    {
    }

    /// <summary>
    /// Called after the layer dialog is composed, sets initial values of the extra elements
    /// </summary>
    protected virtual void OnDialogComposed(GuiComposer compo)
    {
    }
    
    public override void OnMapClosedClient()
    {
        lock (_chunksToGenLock)
            _chunksToGen.Clear();
        
        _curVisibleChunks.Clear();
        _pendingPieces.Clear();
        _lod?.CancelRequests();
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

        _lod?.Dispose();

        lock (Layers)
            Layers.Remove(this);

        MultiChunkMapComponent.DisposeStatic();
        base.Dispose();
    }
    
    public override void OnShutDown()
    {
        MultiChunkMapComponent.tmpTexture?.Dispose();
        _mapdb?.Dispose();
        _lodDb?.Dispose();
    }

    public override void OnOffThreadTick(float dt)
    {
        _genAccum += dt;
        _diskSaveAccum += dt;

        // Pieces waiting for the disk were generated with the settings before Reload
        int configVersion = _configVersion;
        if (_toSaveConfigVersion != configVersion)
        {
            _toSaveList.Clear();
            _toSaveConfigVersion = configVersion;
        }

        bool isShown = Active;
        // Invalidations are written even for hidden layers so cached tiles never go stale
        _lod?.OffThreadTick(isShown && IsLodZoom);

        if (!isShown && (_genAccum < 0.1 || IsShownLayerBusy()))
        {
            SaveMapPieces();
            return;
        }
        _genAccum = 0;

        var stopwatch = isShown ? null : Stopwatch.StartNew();
        int retryCount = 0;
        int quantityToGen;
        lock (_chunksToGenLock)
            quantityToGen = _chunksToGen.Count;

        while (quantityToGen > 0)
        {
            if (mapSink.IsShuttingDown) break;
            if (stopwatch != null && stopwatch.Elapsed.TotalMilliseconds > HiddenLayerBudgetMs) break;

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
            if (tintedPixels == null || configVersion != _configVersion)
            {
                lock (_chunksToGenLock)
                {
                    _chunksToGen.Enqueue(cord);
                }

                retryCount++;
                continue;
            }

            _toSaveList[cord.Copy()] = new MapPieceDB() { Pixels = tintedPixels };
            _lod?.Invalidate(cord);

            LoadFromChunkPixels(cord, tintedPixels);
        }

        _retryCount = retryCount;
        SaveMapPieces();
    }

    /// <summary>
    /// True while a shown layer has chunks to generate, not counting chunks waiting for data from the server.
    /// </summary>
    private static bool IsShownLayerBusy()
    {
        lock (Layers)
        {
            foreach (var layer in Layers)
            {
                if (!layer.Active)
                    continue;

                lock (layer._chunksToGenLock)
                {
                    if (layer._chunksToGen.Count > layer._retryCount)
                        return true;
                }
            }
        }

        return false;
    }

    private void SaveMapPieces()
    {
        if (_toSaveList.Count > 100 || (_diskSaveAccum > 4f && _toSaveList.Count > 0))
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

        if (IsLodZoom)
        {
            if (_lod != null)
            {
                _lod.UpdateView(mapElem.CurrentBlockViewBounds, LodState.LevelForZoom(mapElem.ZoomLevel));
                _lod.Render(mapElem, OverlayColor);
            }
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

        bool isLodZoom = IsLodZoom;
        // The view change only reports chunks that were not visible before, the ones skipped on LOD zoom are queued here
        IEnumerable<FastVec2i> toLoad = _wasLodZoom && !isLodZoom ? _curVisibleChunks : nowVisible;
        _wasLodZoom = isLodZoom;

        lock (_chunksToGenLock)
        {
            foreach (FastVec2i cord in toLoad)
            {
                // On LOD zoom only chunks loaded around the player are generated, LOD tiles are rebuilt from them
                if (isLodZoom && (LodMode == LodMode.Hidden || api.World.BlockAccessor.GetMapChunk(cord.X, cord.Y) == null))
                    continue;

                FastVec2i tmpMccoord = new FastVec2i(cord.X / MultiChunkMapComponent.ChunkLen, cord.Y / MultiChunkMapComponent.ChunkLen);

                int dx = cord.X % MultiChunkMapComponent.ChunkLen;
                int dz = cord.Y % MultiChunkMapComponent.ChunkLen;
                if (dx < 0 || dz < 0) continue;

                if (_loadedMapData.TryGetValue(tmpMccoord, out var mcomp))
                {
                    if (mcomp.IsChunkSet(dx, dz)) continue;
                }

                if (_pendingPieces.ContainsKey(cord)) continue;

                _chunksToGen.Enqueue(cord.Copy());
            }
        }

        foreach (FastVec2i cord in nowHidden)
        {
            if (cord.X < 0 || cord.Y < 0) continue;

            _pendingPieces.Remove(cord);

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

        if (IsLodZoom)
        {
            if (_lod == null)
                return;

            _mouseViewPos.X = args.X - (float)mapElem.Bounds.renderX;
            _mouseViewPos.Y = args.Y - (float)mapElem.Bounds.renderY;
            mapElem.TranslateViewPosToWorldPos(_mouseViewPos, ref _mouseWorldPos);

            if (_lod.TryGetColorAt(_mouseWorldPos, out var color))
                AppendLodHoverText(color, hoverText);
            return;
        }

        foreach (KeyValuePair<FastVec2i, AChunkMapComponent> loadedMapDatum in _loadedMapData)
            loadedMapDatum.Value.OnMouseMove(args, mapElem, hoverText);
    }

    public virtual string GetLocalizedStringByColor(int color)
        => LocalizedNameByColor.TryGetValue(color, out var name) ? name : Lang.Get("na");

    /// <summary>
    /// How the layer is drawn on LOD zoom
    /// </summary>
    protected virtual LodMode LodMode => LodMode.Tiles;

    /// <summary>
    /// Merges 2x2 pixels into one LOD texel
    /// </summary>
    protected virtual int CombineLodColors(int a, int b, int c, int d)
        => LodColors.Dominant(a, b, c, d);

    /// <summary>
    /// Hover text for a LOD texel, the counterpart of <see cref="AChunkMapComponent.OnMouseMove"/>
    /// </summary>
    protected virtual void AppendLodHoverText(int color, StringBuilder hoverText)
        => hoverText.Append(GetLocalizedStringByColor(color));

    /// <summary>
    /// True when LOD texels are not colors and are converted by <see cref="LodTexelToRgba"/> on upload
    /// </summary>
    protected virtual bool HasLodTexelConversion => false;

    protected virtual int LodTexelToRgba(int texel) => texel;

    /// <summary>
    /// Uploads loaded LOD tiles again after <see cref="LodTexelToRgba"/> started returning other colors
    /// </summary>
    protected void RefreshLodTextures()
        => _lod?.RefreshTextures();

    /// <summary>
    /// Appended to the map db file names, changed when the format of stored pieces changes
    /// </summary>
    protected virtual string DbSuffix => "";

    private bool IsLodZoom => LodMode != LodMode.PerChunk && LodState.Level > 0;

    protected abstract int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc);

    protected abstract AChunkMapComponent CreateComponent(FastVec2i baseCord);

    protected bool TryLoadChunks(FastVec2i chunkPos)
    {
        for (var i = 0; i < _chunksTmp.Length; i++)
        {
            _chunksTmp[i] = _capi.World.BlockAccessor.GetChunk(chunkPos.X, i, chunkPos.Y);
            if (_chunksTmp[i] == null || !(_chunksTmp[i] as IClientChunk).LoadedFromServer)
                return false;
        }
        return true;
    }

    protected void ClearChunks()
    {
        for (var i = 0; i < _chunksTmp.Length; i++)
            _chunksTmp[i] = null;
    }

    protected void FillDictionary(int color, string localizedName)
    {
        LocalizedNameByColor.Add(color, localizedName);
    }
    
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
    
    private string GetMapDbFilePath(string suffix = "")
    {
        string text = Path.Combine(GamePaths.DataPath, "Maps");
        GamePaths.EnsurePathExists(text);
        return Path.Combine(text, api.World.SavegameIdentifier + $"-{Title}{DbSuffix}{suffix}.db");
    }

    private int[]?[] ReadChunkPieces(List<FastVec2i> cords)
        => ChunkPieceReader.Read(_mapdb, _toSaveList, cords);

    /// <summary>
    /// Reads the layer settings from the config again, called on the main thread from the settings tab
    /// </summary>
    protected virtual void ApplyConfig()
    {
    }

    /// <summary>
    /// Applies changed settings that are baked into the pixels: purges the map db and redraws the layer
    /// </summary>
    public void Reload()
    {
        _configVersion++;
        LocalizedNameByColor.Clear();
        ApplyConfig();
        _mapdb.Purge();
        Redraw();
    }

    public static T? Find<T>() where T : AMapLayer
    {
        lock (Layers)
            return Layers.OfType<T>().FirstOrDefault();
    }

    public static void ApplyOverlayAlphaToAll()
    {
        lock (Layers)
        {
            foreach (var layer in Layers)
                layer.OverlayColor.A = ConfigManager.ConfigInstance.overlayAlpha;
        }
    }

    private TextCommandResult OnMapCmdRedraw(TextCommandCallingArgs args)
    {
        Redraw();
        return TextCommandResult.Success("Redrawing map...");
    }

    private void Redraw()
    {
        foreach (var value in _loadedMapData.Values)
        {
            value.ActuallyDispose();
        }

        _lod?.RequestPurge();
        _loadedMapData.Clear();
        _pendingPieces.Clear();
        lock (_chunksToGenLock)
        {
            foreach (FastVec2i curVisibleChunk in _curVisibleChunks)
            {
                _chunksToGen.Enqueue(curVisibleChunk.Copy());
            }
        }
    }
    
    private void SetChunkPixels(FastVec2i cord, int[] pixels, HashSet<AChunkMapComponent> modified)
    {
        var mcord = new FastVec2i(cord.X / MultiChunkMapComponent.ChunkLen, cord.Y / MultiChunkMapComponent.ChunkLen);
        var baseCord = new FastVec2i(mcord.X * MultiChunkMapComponent.ChunkLen, mcord.Y * MultiChunkMapComponent.ChunkLen);

        if (!_loadedMapData.TryGetValue(mcord, out var mccomp))
            _loadedMapData[mcord] = mccomp = CreateComponent(baseCord);

        mccomp.setChunk(cord.X - baseCord.X, cord.Y - baseCord.Y, pixels);
        modified.Add(mccomp);
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