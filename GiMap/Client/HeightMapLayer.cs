using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class HeightMapLayer : AMapLayer<HeightMultiChunkMapComponent>
{
    public override string Title => "height";
    public override string LayerGroupCode => "height";
    
    private int _maxHeight;
    private int _seaLevel;
    private int _hueOffset = 100;
    private int _discriditationRate = 5;

    private Vec4i _mountainColor = new Vec4i(255, 0, 0, 255);
    private Vec4i _plainColor = new Vec4i(0, 255, 0, 255);
    private Vec4i _lowLandColor = new Vec4i(0, 150, 0, 255);
    private Vec4i _seaLevelWaterColor = new Vec4i(0, 0, 255, 255);
    private Vec4i _highWaterColor = new Vec4i(50, 255, 255, 255);
    private Vec4i _trenchColor = new Vec4i(0, 0, 150, 255);

    private string[] _waterBlocks = new[] { "game:water-", "game:saltwater-", "game:boilingwater-"};

    public HeightMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
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

            api.ChatCommands.GetOrCreate("heightmap").BeginSubCommand("purgedb").WithDescription("purge the map db")
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

            _maxHeight = _capi.World.MapSizeY;
            _seaLevel = _capi.World.SeaLevel;
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

                    if (!_loadedMapData.TryGetValue(mcord, out HeightMultiChunkMapComponent mccomp))
                        _loadedMapData[mcord] = mccomp = new HeightMultiChunkMapComponent(api as ICoreClientAPI, baseCord, this);


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
        return Path.Combine(text, api.World.SavegameIdentifier + "-height.db");
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
            
            if (IsWater(block))
            {
                while (topBlockHeight > 0 && IsWater(block))
                {
                    topBlockHeight--;
                    topChunkIndex = topBlockHeight / 32;
                    index = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
                    block = api.World.Blocks[index];
                }
                
                resultPixelArray[k] = GetColor(topBlockHeight, _highWaterColor, _seaLevelWaterColor, _trenchColor);
            }
            else
            {
                resultPixelArray[k] = GetColor(topBlockHeight, _mountainColor, _plainColor, _lowLandColor);
            }
        }
        
        for (var n = 0; n < _chunksTmp.Length; n++)
            _chunksTmp[n] = null;
            
        return resultPixelArray;
    }

    private int GetColor(int height, Vec4i colorAboveSeaLevel, Vec4i colorSeaLevel ,Vec4i colorBelowSeaLevel)
    {
        float InverseLerp(float a, float b, float value)
        {
            if (Math.Abs(a - b) < 0.1f)
                return 0f;

            return (value - a) / (b - a);
        }
        
        var discreditationHeight = Discreditation(height, _discriditationRate);
        

        var color = 0;
        if (height >= _seaLevel)
        {
            var progress = 1 - InverseLerp(_seaLevel, _maxHeight, discreditationHeight);
            color = MixColor(colorAboveSeaLevel, colorSeaLevel, progress);
        }
        else
        {
            var progress = 1 - InverseLerp(0, _seaLevel, discreditationHeight);
            color = MixColor(colorSeaLevel,colorBelowSeaLevel, progress);
        }
        return color;
    }

    private int MixColor(Vec4i colorA, Vec4i colorB, float progress)
    {
        int MixChanel(int chanelA, int chanelB, float progress)
            => (int)float.Lerp(chanelA, chanelB, progress);
        
        return ColorUtil.ColorFromRgba(
            MixChanel(colorA.X, colorB.X, progress),
            MixChanel(colorA.Y, colorB.Y, progress),
            MixChanel(colorA.Z, colorB.Z, progress),
            MixChanel(colorA.W, colorB.W, progress));
    }
    
    private int Discreditation(int value, int rate)
        => value / rate * rate;

    private bool IsWater(Block block)
    {
        if (block.BlockMaterial == EnumBlockMaterial.Liquid)
            return _waterBlocks.Any(b => block.Code.ToString().Contains(b));
        
        return false;
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
}