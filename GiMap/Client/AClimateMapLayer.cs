using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

/// <summary>
/// Base for modes that color a pixel by one climate value bucketed into thresholds.
/// The client computes climate by bilinear interpolation of the map region's ClimateMap nodes
/// (per byte: temperature, rainfall, geologic activity), so the nodes around a chunk bound every
/// value inside it. If both bounds fall into the same bucket the chunk is filled without per-pixel work.
/// </summary>
public abstract class AClimateMapLayer : ABlockMapLayer
{
    protected AClimateMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    protected override int GetColor(BlockPos pos, Block block)
        => GetColor(GetValue(api.World.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues)));

    protected override bool TryGetUniformColor(FastVec2i chunkPos, int minY, int maxY, out int color)
    {
        color = 0;
        if (!TryGetClimateRange(chunkPos, out var range))
            return false;

        GetValueRange(range, minY, maxY, out float min, out float max);
        color = GetColor(min);
        return color == GetColor(max);
    }

    /// <summary>Climate value used for coloring, read from the climate at the pixel.</summary>
    protected abstract float GetValue(ClimateCondition climate);

    /// <summary>Bounds of <see cref="GetValue"/> for climate bytes in range and Y in [minY, maxY].</summary>
    protected abstract void GetValueRange(ClimateRange range, int minY, int maxY, out float min, out float max);

    /// <summary>Color bucket of a value. Must be monotonic: values between two of the same bucket share it.</summary>
    protected abstract int GetColor(float value);

    private bool TryGetClimateRange(FastVec2i chunkPos, out ClimateRange range)
    {
        range = default;
        var blockAccessor = api.World.BlockAccessor;
        int regionSize = blockAccessor.RegionSize;
        int baseWorldX = chunkPos.X * 32;
        int baseWorldZ = chunkPos.Y * 32;
        if (baseWorldX < 0 || baseWorldZ < 0)
            return false;

        var climateMap = blockAccessor.GetMapRegion(baseWorldX / regionSize, baseWorldZ / regionSize)?.ClimateMap;
        if (climateMap?.Data == null)
            return false;

        int innerSize = climateMap.InnerSize;
        if (innerSize <= 0 || regionSize % innerSize != 0)
            return false;

        // Two client paths read different nodes (cached BiLerpColorMap and GetColorLerpedCorrectly,
        // the latter shifted by half a node), so take one extra node on each side to cover both.
        int zoom = regionSize / innerSize;
        int inRegionX = baseWorldX % regionSize;
        int inRegionZ = baseWorldZ % regionSize;
        int pad = climateMap.TopLeftPadding;
        int minX = Math.Max(0, inRegionX / zoom - 1 + pad);
        int maxX = Math.Min(climateMap.Size - 1, (inRegionX + 31) / zoom + 1 + pad);
        int minZ = Math.Max(0, inRegionZ / zoom - 1 + pad);
        int maxZ = Math.Min(climateMap.Size - 1, (inRegionZ + 31) / zoom + 1 + pad);
        if (minX > maxX || minZ > maxZ || (maxZ + 1) * climateMap.Size > climateMap.Data.Length)
            return false;

        range = new ClimateRange();
        for (int z = minZ; z <= maxZ; z++)
        {
            for (int x = minX; x <= maxX; x++)
                range.Include(climateMap.GetInt(x, z));
        }

        // Float rounding in the interpolation may land one unit outside the node values
        range.Widen();
        return true;
    }

    protected struct ClimateRange
    {
        public int MinTemperature, MaxTemperature;
        public int MinRainfall, MaxRainfall;
        public int MinGeologicActivity, MaxGeologicActivity;

        public ClimateRange()
        {
            MinTemperature = MinRainfall = MinGeologicActivity = 255;
            MaxTemperature = MaxRainfall = MaxGeologicActivity = 0;
        }

        public void Include(int climate)
        {
            Include((climate >> 16) & 0xFF, ref MinTemperature, ref MaxTemperature);
            Include((climate >> 8) & 0xFF, ref MinRainfall, ref MaxRainfall);
            Include(climate & 0xFF, ref MinGeologicActivity, ref MaxGeologicActivity);
        }

        public void Widen()
        {
            Widen(ref MinTemperature, ref MaxTemperature);
            Widen(ref MinRainfall, ref MaxRainfall);
            Widen(ref MinGeologicActivity, ref MaxGeologicActivity);
        }

        private static void Include(int value, ref int min, ref int max)
        {
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }

        private static void Widen(ref int min, ref int max)
        {
            min = Math.Max(0, min - 1);
            max = Math.Min(255, max + 1);
        }
    }
}
