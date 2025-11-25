using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Path = System.IO.Path;

namespace GiMap.Client;

public class TopographicMapLayer : AMapLayer<TopographicMultiChunkMapComponent>
{
    public override string Title => "topographic";
    public override string LayerGroupCode => "topographic";
    
    private readonly int soilColor = ColorUtil.ColorFromRgba(201, 234, 157, 255);
    private readonly int sandColor = ColorUtil.ColorFromRgba(255, 255, 255, 255);
    private readonly int gravelColor = ColorUtil.ColorFromRgba(200, 200, 200, 255);
    private readonly int stoneColor = ColorUtil.ColorFromRgba(150, 150, 150, 255);
    private readonly int waterColor = ColorUtil.ColorFromRgba(34, 164, 171, 255);
    private readonly int iceColor = ColorUtil.ColorFromRgba(202, 237, 238, 255);
    private readonly int snowColor = ColorUtil.ColorFromRgba(230, 230, 255, 255);
    private readonly int errorColor = ColorUtil.ColorFromRgba(255, 0, 255, 255);
    
    public TopographicMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        if (api.Side == EnumAppSide.Client)
        {
            api.World.Logger.Notification("Loading world map cache db...");
            _mapdb = new MapDB(api.World.Logger);
            string errorMessage = null;
            string mapDbFilePath = GetMapDbFilePath();
            _mapdb.OpenOrCreate(mapDbFilePath, ref errorMessage, requireWriteAccess: true, corruptionProtection: true, doIntegrityCheck: false);
            if (errorMessage != null)
                throw new Exception($"Cannot open {mapDbFilePath}, possibly corrupted. Please fix manually or delete this file to continue playing");

            api.ChatCommands.GetOrCreate("topographicmap").BeginSubCommand("purgedb").WithDescription("purge the map db")
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
            List<MultiChunkMapComponent> modified = new();
            while (q-- > 0)
            {
                if (_readyMapPieces.TryDequeue(out var mappiece))
                {
                    FastVec2i mcord = new FastVec2i(mappiece.Cord.X / MultiChunkMapComponent.ChunkLen, mappiece.Cord.Y / MultiChunkMapComponent.ChunkLen);
                    FastVec2i baseCord = new FastVec2i(mcord.X * MultiChunkMapComponent.ChunkLen, mcord.Y * MultiChunkMapComponent.ChunkLen);

                    if (!_loadedMapData.TryGetValue(mcord, out TopographicMultiChunkMapComponent mccomp))
                        _loadedMapData[mcord] = mccomp = new TopographicMultiChunkMapComponent(api as ICoreClientAPI, baseCord, this);


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
    
    private string GetMapDbFilePath()
    {
        string text = Path.Combine(GamePaths.DataPath, "Maps");
        GamePaths.EnsurePathExists(text);
        return Path.Combine(text, api.World.SavegameIdentifier + "-topology.db");
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

    protected override void LoadFromChunkPixels(FastVec2i cord, int[] pixels)
    {
        _readyMapPieces.Enqueue(new ReadyMapPiece
        {
            Pixels = pixels,
            Cord = cord
        });
    }
    
    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        var vec2i = new Vec2i();

        for (var i = 0; i < _chunksTmp.Length; i++)
        {
            _chunksTmp[i] = _capi.World.BlockAccessor.GetChunk(chunkPos.X, i, chunkPos.Y);
            if (_chunksTmp[i] == null || !(_chunksTmp[i] as IClientChunk).LoadedFromServer)
                return null;
        }

        var resultPixelArray = new int[1024];
        for (var k = 0; k < resultPixelArray.Length; k++)
        {
            int topBlockHeight = mc.RainHeightMap[k];
            int topChunkIndex = topBlockHeight / 32;
            if (topChunkIndex >= _chunksTmp.Length)
                continue;
            
            MapUtil.PosInt2d(k, 32L, vec2i);
            int index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
            Block block = api.World.Blocks[index];
            
            while (topBlockHeight > 0 && !IsBlockValid(block))
            {
                topBlockHeight--;
                topChunkIndex = topBlockHeight / 32;
                index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
                block = api.World.Blocks[index];
            }

            resultPixelArray[k] = GetMaterialColor(block);
        }
        
        for (var n = 0; n < _chunksTmp.Length; n++)
            _chunksTmp[n] = null;
            
        return resultPixelArray;
    }

    private bool IsBlockValid(Block block)
    {
        return block.BlockMaterial == EnumBlockMaterial.Gravel
            || block.BlockMaterial == EnumBlockMaterial.Sand
            || block.BlockMaterial == EnumBlockMaterial.Soil
            || block.BlockMaterial == EnumBlockMaterial.Stone
            || block.BlockMaterial == EnumBlockMaterial.Ice
            || block.BlockMaterial == EnumBlockMaterial.Snow
            || block.BlockMaterial == EnumBlockMaterial.Liquid;
    }

    private int GetMaterialColor(Block block)
    {
        return block.BlockMaterial switch
        {
            EnumBlockMaterial.Gravel => gravelColor,
            EnumBlockMaterial.Sand => sandColor,
            EnumBlockMaterial.Soil => soilColor,
            EnumBlockMaterial.Stone => stoneColor,
            EnumBlockMaterial.Ice => iceColor,
            EnumBlockMaterial.Snow => snowColor,
            EnumBlockMaterial.Liquid => waterColor,
            _ => errorColor,
        };
    }
}