using GiMap.Client.Contours;
using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GiMap.Client.Bathymetric;

/// <summary>
/// Depth of water bodies: depth is the number of water and ice blocks between the water surface and the bottom,
/// so lakes above sea level are measured from their own surface. Isobaths and depth labels are drawn over water.
/// </summary>
public class BathymetricMapLayer : AContourMapLayer
{
    public override string Title => MapTypes.Bathymetric;

    private int _landColor;
    // Water color of every depth step of the contour interval, the last one is used for everything deeper
    private int[] _depthColors = Array.Empty<int>();

    public override float ContourLabelsMinZoom => ConfigManager.ConfigInstance?.BathymetricMode.contourLabelsMinZoom ?? 0;
    public override float PeakLabelsMinZoom => ConfigManager.ConfigInstance?.BathymetricMode.deepestLabelsMinZoom ?? 0;

    public BathymetricMapLayer(ICoreAPI api, IWorldMapManager mapSink)
        : base(api, mapSink,
            ConfigManager.ConfigInstance.BathymetricMode.majorContourColor,
            ConfigManager.ConfigInstance.BathymetricMode.deepestColor,
            ConfigManager.ConfigInstance.BathymetricMode.labelOutlineColor)
    {
        ApplyConfig();
    }

    protected override void ApplyConfig()
    {
        var config = ConfigManager.ConfigInstance.BathymetricMode;
        _landColor = ColorUtilExtensions.HexToColor(config.landColor);
        _showContours = config.showContours;
        _contourInterval = Math.Max(1, config.contourInterval);
        _majorContourEvery = Math.Max(1, config.majorContourEvery);
        _contourColor = ColorUtilExtensions.HexToColor(config.contourColor);
        _majorContourColor = ColorUtilExtensions.HexToColor(config.majorContourColor);
        ShowLabels = config.showDepthLabels;

        if (_showContours)
        {
            FillDictionary(_contourColor, Lang.Get("bathymetric-contour"));
            FillDictionary(_majorContourColor, Lang.Get("bathymetric-contour-major"));
        }
        FillDictionary(_landColor, Lang.Get("bathymetric-land"));

        FillDepthColors(config);
    }

    private void FillDepthColors(GiMapConfig.Bathymetric config)
    {
        int shallowColor = ColorUtilExtensions.HexToColor(config.shallowColor);
        int deepColor = ColorUtilExtensions.HexToColor(config.deepColor);
        int maxDepth = Math.Max(1, config.gradientMaxDepth);

        int stepCount = (maxDepth + _contourInterval - 1) / _contourInterval + 1;
        _depthColors = new int[stepCount];
        for (var step = 0; step < stepCount; step++)
        {
            int fromDepth = Math.Max(1, step * _contourInterval);
            int toDepth = (step + 1) * _contourInterval - 1;
            float t = Math.Min(1f, step * _contourInterval / (float)maxDepth);
            _depthColors[step] = ColorUtilExtensions.Lerp(shallowColor, deepColor, t);

            string name = step == stepCount - 1
                ? Lang.Get("bathymetric-depth-deeper", fromDepth)
                : fromDepth == toDepth
                    ? Lang.Get("bathymetric-depth-exact", fromDepth)
                    : Lang.Get("bathymetric-depth", fromDepth, toDepth);
            FillDictionary(_depthColors[step], name);
        }
    }

    // Water columns are contoured by depth, land is drawn with one color and has depth 0
    protected override int SampleColumn(IWorldChunk[] column, int localX, int localZ, int startY, out bool isContoured, out int color)
    {
        isContoured = false;
        color = 0;

        int y = GetGroundHeight(column, localX, localZ, startY, out var block);
        if (y == SkippedPixel)
            return SkippedPixel;

        // Snow and ice are walked through: a frozen lake is still a lake
        int surfaceY = SkippedPixel;
        bool hasWater = false;
        while (y > 0 && IsWaterOrFrozen(block))
        {
            if (surfaceY == SkippedPixel && block.BlockMaterial != EnumBlockMaterial.Snow)
                surfaceY = y;

            hasWater |= block.BlockMaterial == EnumBlockMaterial.Water;
            y--;
            block = ReadBlock(column, localX, y, localZ);
        }

        if (!hasWater)
        {
            color = _landColor;
            return 0;
        }

        int depth = surfaceY - y;
        isContoured = true;
        color = _depthColors[Math.Min(depth / _contourInterval, _depthColors.Length - 1)];
        return depth;
    }

    private static bool IsWaterOrFrozen(Block block)
        => block.BlockMaterial == EnumBlockMaterial.Water
            || block.BlockMaterial == EnumBlockMaterial.Ice
            || block.BlockMaterial == EnumBlockMaterial.Snow;
}
