using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Caves;

public enum CaveKind : byte
{
    // Layer is above the ground, nothing is drawn
    None = 0,
    // No open space in the layer
    Solid,
    // Open space without a floor in reach below the layer
    Void,
    Stone,
    Soil,
    Sand,
    Gravel,
    Ore,
    Water,
    Lava,
    Ice,
    Other,
}

/// <summary>
/// Shows cave floors of one layer, every layer is one chunk (32 blocks) high, layer 1 is the deepest.
/// Chunk data holds packed floors (kind and Y) of all layers, so switching the layer does not regenerate chunks.
/// </summary>
public class CavesMapLayer : AMapLayer
{
    public const int LayerHeight = 32;

    private const int ChunkPixelCount = 1024;
    private const int SkippedPixel = -1;
    // How deep below the layer bottom a floor is still looked for
    private const int MaxFloorDepth = 32;

    private const string LayerSliderKey = "caves-layer-slider";
    private const string FollowSwitchKey = "caves-follow-switch";

    public override string Title => MapTypes.Caves;

    public int LayerCount { get; }
    public int CurrentLayer { get; private set; }

    private readonly int[] _colorByKind = new int[Enum.GetValues<CaveKind>().Length];

    private bool[] _isOpenByBlockId = Array.Empty<bool>();
    private CaveKind[] _kindByBlockId = Array.Empty<CaveKind>();
    private bool[] _isGroundByBlockId = Array.Empty<bool>();
    private int[] _columnTmp = Array.Empty<int>();

    private GuiComposer? _dialogComposer;
    private bool _isUpdatingDialog;

    public CavesMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        LayerCount = Math.Max(1, api.World.BlockAccessor.MapSizeY / LayerHeight);

        var config = ConfigManager.ConfigInstance.CavesMode;
        CurrentLayer = Math.Clamp(config.selectedLayer, 1, LayerCount);

        _colorByKind[(int)CaveKind.Solid] = ColorUtilExtensions.HexToColor(config.solidColor);
        _colorByKind[(int)CaveKind.Void] = ColorUtilExtensions.HexToColor(config.voidColor);
        _colorByKind[(int)CaveKind.Stone] = ColorUtilExtensions.HexToColor(config.stoneColor);
        _colorByKind[(int)CaveKind.Soil] = ColorUtilExtensions.HexToColor(config.soilColor);
        _colorByKind[(int)CaveKind.Sand] = ColorUtilExtensions.HexToColor(config.sandColor);
        _colorByKind[(int)CaveKind.Gravel] = ColorUtilExtensions.HexToColor(config.gravelColor);
        _colorByKind[(int)CaveKind.Ore] = ColorUtilExtensions.HexToColor(config.oreColor);
        _colorByKind[(int)CaveKind.Water] = ColorUtilExtensions.HexToColor(config.waterColor);
        _colorByKind[(int)CaveKind.Lava] = ColorUtilExtensions.HexToColor(config.lavaColor);
        _colorByKind[(int)CaveKind.Ice] = ColorUtilExtensions.HexToColor(config.iceColor);
        _colorByKind[(int)CaveKind.Other] = ColorUtilExtensions.HexToColor(config.otherColor);
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        _columnTmp = new int[api.World.BlockAccessor.MapSizeY];

        var blocks = api.World.Blocks;
        _isOpenByBlockId = new bool[blocks.Count];
        _kindByBlockId = new CaveKind[blocks.Count];
        _isGroundByBlockId = new bool[blocks.Count];
        for (var i = 0; i < blocks.Count; i++)
        {
            var block = blocks[i];
            if (block == null)
            {
                _kindByBlockId[i] = CaveKind.Other;
                continue;
            }

            _isOpenByBlockId[i] = IsOpenBlockType(block);
            _kindByBlockId[i] = GetKind(block);
            _isGroundByBlockId[i] = IsGroundBlockType(block);
        }
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new CavesChunkMapComponent(_capi, baseCord, this);

    public override void OnTick(float dt)
    {
        if (Active && ConfigManager.ConfigInstance.CavesMode.followPlayer)
        {
            var entity = _capi.World.Player?.Entity;
            if (entity != null)
                SetLayer((int)entity.Pos.Y / LayerHeight + 1);
        }

        base.OnTick(dt);
    }

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        var result = new int[ChunkPixelCount * LayerCount];
        for (var k = 0; k < ChunkPixelCount; k++)
        {
            int localX = k % 32;
            int localZ = k / 32;

            int ground = ReadColumn(localX, localZ, mc.RainHeightMap[k]);
            if (ground == SkippedPixel)
                continue;

            for (var layer = 0; layer < LayerCount; layer++)
                result[layer * ChunkPixelCount + k] = FindFloor(layer * LayerHeight, ground);
        }

        ClearChunks();
        return result;
    }

    /// <summary>
    /// Reads block ids of the column into <see cref="_columnTmp"/> from the ground down.
    /// Returns the ground height or <see cref="SkippedPixel"/> if startY is above the world.
    /// </summary>
    private int ReadColumn(int localX, int localZ, int startY)
    {
        if (startY / 32 >= _chunksTmp.Length)
            return SkippedPixel;

        int ground = startY;
        while (ground > 0 && !_isGroundByBlockId[ReadBlockId(localX, ground, localZ)])
            ground--;

        for (int y = ground - 1; y >= 0; y--)
            _columnTmp[y] = ReadBlockId(localX, y, localZ);

        return ground;
    }

    private int ReadBlockId(int localX, int y, int localZ)
        => _chunksTmp[y / 32].UnpackAndReadBlock(MapUtil.Index3d(localX, y % 32, localZ, 32, 32), 3);

    // The floor is the first not open block below an open one, the open block must be inside the layer
    private int FindFloor(int layerBottom, int ground)
    {
        if (ground <= layerBottom)
            return Pack(CaveKind.None, 0);

        int top = Math.Min(layerBottom + LayerHeight - 1, ground - 1);
        int lowest = Math.Max(0, layerBottom - MaxFloorDepth);
        bool hasOpen = false;

        for (int y = top; y >= lowest; y--)
        {
            int blockId = _columnTmp[y];
            if (IsOpen(blockId))
            {
                hasOpen = true;
                continue;
            }

            if (hasOpen)
                return Pack(GetKind(blockId), y);

            if (y <= layerBottom)
                break;
        }

        return Pack(hasOpen ? CaveKind.Void : CaveKind.Solid, 0);
    }

    /// <summary>
    /// Colors of the current layer from packed chunk data
    /// </summary>
    public int[] BuildPixels(int[] data)
    {
        var pixels = new int[ChunkPixelCount];
        int offset = (CurrentLayer - 1) * ChunkPixelCount;
        if (data.Length < offset + ChunkPixelCount)
            return pixels;

        int layerBottom = (CurrentLayer - 1) * LayerHeight;
        for (var k = 0; k < ChunkPixelCount; k++)
        {
            var (kind, y) = Unpack(data[offset + k]);
            pixels[k] = GetColor(kind, y, layerBottom);
        }

        return pixels;
    }

    /// <summary>
    /// Hover text of a packed floor of the current layer
    /// </summary>
    public string? GetHoverText(int[] data, int k)
    {
        int index = (CurrentLayer - 1) * ChunkPixelCount + k;
        if (index >= data.Length)
            return null;

        var (kind, y) = Unpack(data[index]);
        return kind switch
        {
            CaveKind.None => null,
            CaveKind.Solid => Lang.Get("caves-solid"),
            CaveKind.Void => Lang.Get("caves-void"),
            _ => Lang.Get("caves-floor", GetKindName(kind), y),
        };
    }

    public void SetLayer(int layer)
    {
        layer = Math.Clamp(layer, 1, LayerCount);
        if (layer == CurrentLayer)
            return;

        CurrentLayer = layer;
        ConfigManager.ConfigInstance.CavesMode.selectedLayer = layer;

        foreach (var component in _loadedMapData.Values)
            (component as CavesChunkMapComponent)?.RefreshLayer();

        UpdateDialog();
    }

    protected override int ExtraDialogHeight => 70;

    protected override void ComposeExtraElements(GuiComposer compo, ref ElementBounds row)
    {
        row = row.BelowCopy(0, 10).WithFixedSize(125, 25);
        compo.AddSlider(OnLayerSliderChanged, row, LayerSliderKey);

        row = row.BelowCopy(0, 10).WithFixedSize(25, 25);
        compo.AddSwitch(OnFollowToggled, row, FollowSwitchKey, 25);
        compo.AddStaticText(Lang.Get("caves-follow-player"), CairoFont.WhiteSmallText(), row.RightCopy(8, 3).WithFixedSize(130, 25));
    }

    protected override void OnDialogComposed(GuiComposer compo)
    {
        _dialogComposer = compo;
        compo.GetSlider(LayerSliderKey).OnSliderTooltip = value => Lang.Get("caves-layer-tooltip", value, (value - 1) * LayerHeight, value * LayerHeight - 1);
        UpdateDialog();
    }

    private void UpdateDialog()
    {
        if (_dialogComposer == null)
            return;

        _isUpdatingDialog = true;
        _dialogComposer.GetSlider(LayerSliderKey)?.SetValues(CurrentLayer, 1, LayerCount, 1);
        _dialogComposer.GetSwitch(FollowSwitchKey)?.SetValue(ConfigManager.ConfigInstance.CavesMode.followPlayer);
        _isUpdatingDialog = false;
    }

    private bool OnLayerSliderChanged(int value)
    {
        if (_isUpdatingDialog)
            return true;

        // Picking a layer by hand stops following the player
        ConfigManager.ConfigInstance.CavesMode.followPlayer = false;
        SetLayer(value);
        UpdateDialog();
        return true;
    }

    private void OnFollowToggled(bool isOn)
    {
        if (_isUpdatingDialog)
            return;

        ConfigManager.ConfigInstance.CavesMode.followPlayer = isOn;
    }

    public override void OnMapClosedClient()
    {
        _dialogComposer = null;
        base.OnMapClosedClient();
    }

    // Floors are brighter the higher they are, floors below the layer are darker than any floor in it
    private int GetColor(CaveKind kind, int y, int layerBottom)
    {
        int color = _colorByKind[(int)kind];
        if (kind is CaveKind.None or CaveKind.Solid or CaveKind.Void)
            return color;

        float progress = Math.Clamp((y - (layerBottom - MaxFloorDepth)) / (float)(LayerHeight + MaxFloorDepth - 1), 0, 1);
        return ColorUtil.ColorMultiply3(color, 0.25f + 0.75f * progress);
    }

    private static string GetKindName(CaveKind kind) => kind switch
    {
        CaveKind.Stone => Lang.Get("caves-stone"),
        CaveKind.Soil => Lang.Get("caves-soil"),
        CaveKind.Sand => Lang.Get("caves-sand"),
        CaveKind.Gravel => Lang.Get("caves-gravel"),
        CaveKind.Ore => Lang.Get("caves-ore"),
        CaveKind.Water => Lang.Get("water"),
        CaveKind.Lava => Lang.Get("caves-lava"),
        CaveKind.Ice => Lang.Get("caves-ice"),
        _ => Lang.Get("caves-other"),
    };

    private static int Pack(CaveKind kind, int y)
        => (int)kind << 16 | y & 0xffff;

    private static (CaveKind Kind, int Y) Unpack(int value)
        => ((CaveKind)(value >> 16 & 0xff), value & 0xffff);

    private bool IsOpen(int blockId)
        => blockId >= 0 && blockId < _isOpenByBlockId.Length && _isOpenByBlockId[blockId];

    private CaveKind GetKind(int blockId)
        => blockId >= 0 && blockId < _kindByBlockId.Length ? _kindByBlockId[blockId] : CaveKind.Other;

    // Called once per block type in OnLoaded: air and blocks without collision (plants, loose stones) are open
    private static bool IsOpenBlockType(Block block)
    {
        if (block.Id == 0)
            return true;

        if (block.BlockMaterial is EnumBlockMaterial.Water or EnumBlockMaterial.Lava)
            return false;

        return block.CollisionBoxes == null || block.CollisionBoxes.Length == 0;
    }

    // Called once per block type in OnLoaded
    private static CaveKind GetKind(Block block) => block.BlockMaterial switch
    {
        EnumBlockMaterial.Stone => CaveKind.Stone,
        EnumBlockMaterial.Soil => CaveKind.Soil,
        EnumBlockMaterial.Sand => CaveKind.Sand,
        EnumBlockMaterial.Gravel => CaveKind.Gravel,
        EnumBlockMaterial.Ore => CaveKind.Ore,
        EnumBlockMaterial.Water => CaveKind.Water,
        EnumBlockMaterial.Lava => CaveKind.Lava,
        EnumBlockMaterial.Ice or EnumBlockMaterial.Snow => CaveKind.Ice,
        _ => CaveKind.Other,
    };

    // Called once per block type in OnLoaded: the ground is the first natural block from the top, trees are skipped
    private static bool IsGroundBlockType(Block block)
    {
        return block.BlockMaterial == EnumBlockMaterial.Gravel
            || block.BlockMaterial == EnumBlockMaterial.Sand
            || block.BlockMaterial == EnumBlockMaterial.Soil
            || block.BlockMaterial == EnumBlockMaterial.Stone
            || block.BlockMaterial == EnumBlockMaterial.Ore
            || block.BlockMaterial == EnumBlockMaterial.Ice
            || block.BlockMaterial == EnumBlockMaterial.Snow
            || block.BlockMaterial == EnumBlockMaterial.Water;
    }
}
