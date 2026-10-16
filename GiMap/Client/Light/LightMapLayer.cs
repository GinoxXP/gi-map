using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace GiMap.Client.Light;

public class LightMapLayer : ABlockMapLayer
{
    // Block light level is at most 31 and drops by 1 per block, so it never reaches further than one chunk
    private const int LightRadius = 32;

    private static readonly int DarkColor = ColorUtil.ColorFromRgba(0, 0, 0, 255);

    private bool[] _isLightSource;
    private readonly List<int> _blockIdsTmp = new();

    public override string Title => MapTypes.Light;

    public LightMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new LightChunkMapComponent(_capi, baseCord, this);

    // Same as the chunk component: no hover text
    protected override void AppendLodHoverText(int color, StringBuilder hoverText)
    {
    }

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (HasLightSourcesNearby(chunkPos, mc))
            return base.GenerateChunkImage(chunkPos, mc);

        var resultPixelArray = new int[1024];
        Array.Fill(resultPixelArray, DarkColor);
        return resultPixelArray;
    }

    protected override int GetColor(BlockPos pos, Block block)
    {
        var light = api.World.BlockAccessor.GetLightRGBs(pos);

        var r = (int)(light.R * 255);
        var g = (int)(light.G * 255);
        var b = (int)(light.B * 255);
        var a = 255;

        var color = ColorUtil.ColorFromRgba(r, g, b, a);
        return color;
    }

    /// <summary>
    /// Checks block palettes of the 3x3 chunk columns around the chunk near its surface.
    /// Returns true if a light source may be there or if darkness cannot be proven (e.g. chunks are not loaded).
    /// </summary>
    private bool HasLightSourcesNearby(FastVec2i chunkPos, IMapChunk mc)
    {
        _isLightSource ??= BuildLightSourceTable();

        int minY = int.MaxValue;
        int maxY = int.MinValue;
        foreach (var height in mc.RainHeightMap)
        {
            minY = Math.Min(minY, height);
            maxY = Math.Max(maxY, height);
        }

        int chunksCountY = api.World.BlockAccessor.MapSizeY / 32;
        int fromY = Math.Max(0, (minY - LightRadius) / 32);
        int toY = Math.Min(chunksCountY - 1, (maxY + LightRadius) / 32);

        for (var dx = -1; dx <= 1; dx++)
        for (var dz = -1; dz <= 1; dz++)
        for (var cy = fromY; cy <= toY; cy++)
        {
            var chunk = _capi.World.BlockAccessor.GetChunk(chunkPos.X + dx, cy, chunkPos.Y + dz);
            if (chunk == null || !(chunk as IClientChunk).LoadedFromServer)
                return true;

            chunk.Unpack_ReadOnly();
            if (chunk.Data is not ChunkData data)
                return true;

            _blockIdsTmp.Clear();
            data.FuzzyListBlockIds(_blockIdsTmp);
            foreach (var blockId in _blockIdsTmp)
            {
                if (IsLightSource(blockId))
                    return true;
            }

            // FuzzyListBlockIds does not include fluids (e.g. lava)
            var fluidsLayer = data.fluidsLayer;
            var fluidsPalette = fluidsLayer?.palette;
            if (fluidsPalette == null)
                continue;

            int fluidsCount = Math.Min(fluidsLayer.paletteCount, fluidsPalette.Length);
            for (var i = 0; i < fluidsCount; i++)
            {
                if (IsLightSource(fluidsPalette[i]))
                    return true;
            }
        }

        return false;
    }

    private bool IsLightSource(int blockId)
        => blockId < 0 || blockId >= _isLightSource.Length || _isLightSource[blockId];

    private bool[] BuildLightSourceTable()
    {
        var baseMethod = typeof(Block).GetMethod(nameof(Block.GetLightHsv));
        var parameterTypes = baseMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        var overridesByType = new Dictionary<Type, bool>();

        var blocks = api.World.Blocks;
        var table = new bool[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null)
                continue;

            var type = block.GetType();
            if (!overridesByType.TryGetValue(type, out var overridesLight))
            {
                // Blocks overriding GetLightHsv may emit light depending on their block entity
                var method = type.GetMethod(nameof(Block.GetLightHsv), BindingFlags.Instance | BindingFlags.Public, parameterTypes);
                overridesLight = method != null && method.DeclaringType != typeof(Block);
                overridesByType[type] = overridesLight;
            }

            table[i] = overridesLight || block.LightHsv[2] > 0;
        }

        return table;
    }
}
