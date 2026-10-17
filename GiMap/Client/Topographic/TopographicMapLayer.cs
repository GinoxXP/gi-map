using GiMap.Client.Contours;
using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace GiMap.Client.Topographic;

public class TopographicMapLayer : AContourMapLayer
{
    public override string Title => MapTypes.Topographic;

    private readonly string[] _roadBlocks = new[]
    {
        "game:stonepath-",
        "game:stonepathslab-",
        "game:stonepathstairs-",
        "game:woodenpath-",
    };

    private int _gravelColor;
    private int _iceColor;
    private int _roadColor;
    private int _sandColor;
    private int _snowColor;
    private int _soilColor;
    private int _stoneColor;
    private int _waterColor;
    private readonly int _errorColor;

    public override float ContourLabelsMinZoom => ConfigManager.ConfigInstance?.TopographicMode.contourLabelsMinZoom ?? 0;
    public override float PeakLabelsMinZoom => ConfigManager.ConfigInstance?.TopographicMode.peakLabelsMinZoom ?? 0;

    private int[] _colorByBlockId = Array.Empty<int>();

    public TopographicMapLayer(ICoreAPI api, IWorldMapManager mapSink)
        : base(api, mapSink,
            ConfigManager.ConfigInstance.TopographicMode.majorContourColor,
            ConfigManager.ConfigInstance.TopographicMode.peakColor,
            ConfigManager.ConfigInstance.TopographicMode.labelOutlineColor)
    {
        _errorColor = ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);

        ApplyConfig();
    }

    protected override void ApplyConfig()
    {
        var config = ConfigManager.ConfigInstance.TopographicMode;
        _gravelColor = ColorUtilExtensions.HexToColor(config.gravelColor);
        _iceColor = ColorUtilExtensions.HexToColor(config.iceColor);
        _roadColor = ColorUtilExtensions.HexToColor(config.roadColor);
        _sandColor = ColorUtilExtensions.HexToColor(config.sandColor);
        _snowColor = ColorUtilExtensions.HexToColor(config.snowColor);
        _soilColor = ColorUtilExtensions.HexToColor(config.soilColor);
        _stoneColor = ColorUtilExtensions.HexToColor(config.stoneColor);
        _waterColor = ColorUtilExtensions.HexToColor(config.waterColor);
        _showContours = config.showContours;
        _contourInterval = Math.Max(1, config.contourInterval);
        _majorContourEvery = Math.Max(1, config.majorContourEvery);
        _contourColor = ColorUtilExtensions.HexToColor(config.contourColor);
        _majorContourColor = ColorUtilExtensions.HexToColor(config.majorContourColor);
        ShowLabels = config.showHeightLabels;

        FillDictionaries();
    }

    private void FillDictionaries()
    {
        FillDictionary(_gravelColor, Lang.Get("topographic-gravel"));
        FillDictionary(_iceColor, Lang.Get("topographic-ice"));
        FillDictionary(_roadColor, Lang.Get("topographic-road"));
        FillDictionary(_sandColor, Lang.Get("topographic-sand"));
        FillDictionary(_snowColor, Lang.Get("topographic-snow"));
        FillDictionary(_soilColor, Lang.Get("topographic-soil"));
        FillDictionary(_stoneColor, Lang.Get("topographic-stone"));
        FillDictionary(_waterColor, Lang.Get("water"));

        if (_showContours)
        {
            FillDictionary(_contourColor, Lang.Get("topographic-contour"));
            FillDictionary(_majorContourColor, Lang.Get("topographic-contour-major"));
        }
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        var blocks = api.World.Blocks;
        _colorByBlockId = new int[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
            _colorByBlockId[i] = blocks[i] == null ? _errorColor : GetMaterialColor(blocks[i]);
    }

    // Contours are drawn over land, water is only the lower side of the shore
    protected override int SampleColumn(IWorldChunk[] column, int localX, int localZ, int startY, out bool isContoured, out int color)
    {
        int height = GetGroundHeight(column, localX, localZ, startY, out var block);
        if (height == SkippedPixel)
        {
            isContoured = false;
            color = 0;
            return SkippedPixel;
        }

        isContoured = block.BlockMaterial != EnumBlockMaterial.Water;
        color = _colorByBlockId[block.Id];
        return height;
    }

    // Called once per block type in OnLoaded, the result is looked up by block id
    private int GetMaterialColor(Block block) {
        if (IsRoad(block))
            return _roadColor;

        return block.BlockMaterial switch
        {
            EnumBlockMaterial.Gravel => _gravelColor,
            EnumBlockMaterial.Sand => _sandColor,
            EnumBlockMaterial.Soil => _soilColor,
            EnumBlockMaterial.Stone => _stoneColor,
            EnumBlockMaterial.Ice => _iceColor,
            EnumBlockMaterial.Snow => _snowColor,
            EnumBlockMaterial.Water => _waterColor,
            _ => _errorColor,
        };
    }

    private bool IsRoad(Block block)
    {
        var code = block.Code?.ToString();
        return code != null && _roadBlocks.Any(code.Contains);
    }
}
