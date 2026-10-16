using System.Text;
using System.Text.RegularExpressions;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace GiMap.Client.Ore;

/// <summary>
/// Shows ore deposits. An ore group is one entry of the config ore lists, every pixel stores up to
/// <see cref="MaxSlots"/> groups found in the column from the top down, packed by 8 bits with the topmost one in the high byte.
/// Pixels are drawn with the topmost shown group, so hiding ores does not regenerate chunks.
/// </summary>
public class OreMapLayer : AMapLayer
{
    private const int ChunkPixelCount = 1024;
    private const int MaxSlots = 4;
    private const int MaxGroups = 255;

    private const string SelectAllButtonKey = "ore-select-all";
    private const string ClearAllButtonKey = "ore-clear-all";
    private const string GroupSwitchKey = "ore-group-";
    private const string PresetSwitchKey = "ore-preset-";
    private const int GroupColumns = 3;
    private const int GroupRowHeight = 26;
    private const int GroupColumnWidth = 175;
    private const int PresetColumns = 3;
    private const int PresetColumnWidth = GroupColumns * GroupColumnWidth / PresetColumns;
    private const int SectionGap = 10;

    public override string Title => MapTypes.Ore;

    // Group indices are stored in the db, so it is bound to the order of the config ore lists
    protected override string DbSuffix => "-v2-" + HashGroupKeys();

    protected override bool HasLodTexelConversion => true;

    // Index 0 is no ore, group index is the position in the config lists starting from 1
    private readonly List<OreGroup?> _groups = new() { null };
    private bool[] _isGroupShown = Array.Empty<bool>();
    private byte[] _groupByBlockId = Array.Empty<byte>();
    private readonly List<OrePreset> _presets = new();
    private readonly List<int> _blockIdsTmp = new();
    private bool[] _hasOreTmp;

    private GuiComposer? _dialogComposer;
    private bool _isUpdatingDialog;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        var keys = GetGroupKeys();
        var regexes = new List<Regex>(keys.Count);
        foreach (var (key, _) in keys)
        {
            regexes.Add(new Regex(key));
            _groups.Add(null);
        }

        var blocks = api.World.Blocks;
        _groupByBlockId = new byte[blocks.Count];
        foreach (var block in blocks)
        {
            if (block?.Code == null || block.Id >= _groupByBlockId.Length)
                continue;

            var code = block.Code.ToString();
            for (var i = 0; i < regexes.Count; i++)
            {
                if (!regexes[i].IsMatch(code))
                    continue;

                int index = i + 1;
                _groups[index] ??= new OreGroup(keys[i].Key, keys[i].Color, GetGroupName(block));
                _groupByBlockId[block.Id] = (byte)index;
                break;
            }
        }

        var hidden = ConfigManager.ConfigInstance.OreMode.hiddenOres ?? Array.Empty<string>();
        _isGroupShown = new bool[_groups.Count];
        for (var i = 1; i < _groups.Count; i++)
            _isGroupShown[i] = _groups[i] is { } group && !hidden.Contains(group.Key);

        LoadPresets();
    }

    // Presets keep only groups found among blocks, a preset without them is not shown
    private void LoadPresets()
    {
        var presets = ConfigManager.ConfigInstance.OreMode.presets;
        if (presets == null)
            return;

        foreach (var (name, keys) in presets)
        {
            var groups = new List<int>();
            for (var i = 1; i < _groups.Count; i++)
            {
                if (_groups[i] is { } group && keys != null && keys.Contains(group.Key))
                    groups.Add(i);
            }

            if (groups.Count > 0)
                _presets.Add(new OrePreset(Lang.GetIfExists("ore-preset-" + name) ?? name, groups.ToArray()));
        }
    }

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!TryLoadChunks(chunkPos))
            return null;

        _hasOreTmp ??= new bool[_chunksTmp.Length];

        // Y-chunks without ore in their palettes are skipped
        int topOreChunkIndex = -1;
        for (var i = 0; i < _chunksTmp.Length; i++)
        {
            _hasOreTmp[i] = HasOre(_chunksTmp[i]);
            if (_hasOreTmp[i])
                topOreChunkIndex = i;
        }

        var result = new int[ChunkPixelCount];
        if (topOreChunkIndex < 0)
        {
            ClearChunks();
            return result;
        }

        for (var k = 0; k < result.Length; k++)
        {
            int y = mc.RainHeightMap[k];
            if (y / 32 >= _chunksTmp.Length)
                continue;

            int localX = k % 32;
            int localZ = k / 32;
            y = Math.Min(y, topOreChunkIndex * 32 + 31);

            int packed = 0;
            int count = 0;
            while (y > 0 && count < MaxSlots)
            {
                int chunkIndex = y / 32;
                if (!_hasOreTmp[chunkIndex])
                {
                    // Jump to the top of the next lower chunk
                    y = chunkIndex * 32 - 1;
                    continue;
                }

                int blockId = _chunksTmp[chunkIndex].UnpackAndReadBlock(MapUtil.Index3d(localX, y % 32, localZ, 32, 32), 3);
                int group = GetGroup(blockId);
                if (group != 0 && !ContainsGroup(packed, group))
                    packed |= group << SlotShift(count++);

                y--;
            }

            result[k] = packed;
        }

        ClearChunks();
        return result;
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new OreChunkMapComponent(_capi, baseCord, this);

    /// <summary>
    /// Colors of shown ores from packed chunk data
    /// </summary>
    public int[] BuildPixels(int[] data)
    {
        var pixels = new int[ChunkPixelCount];
        int count = Math.Min(data.Length, ChunkPixelCount);
        for (var k = 0; k < count; k++)
            pixels[k] = GetPixel(data[k]);

        return pixels;
    }

    /// <summary>
    /// Names of shown ores of a packed column from the top down, null if there are none
    /// </summary>
    public string? GetHoverText(int packed)
    {
        StringBuilder? text = null;
        for (var slot = 0; slot < MaxSlots; slot++)
        {
            int group = GetSlot(packed, slot);
            if (group == 0)
                break;

            if (!IsShown(group))
                continue;

            if (text == null)
                text = new StringBuilder();
            else
                text.Append(", ");

            text.Append(_groups[group]!.Name);
        }

        return text?.ToString();
    }

    public void SetGroupShown(int group, bool isShown)
    {
        if (group <= 0 || group >= _isGroupShown.Length || _groups[group] == null || _isGroupShown[group] == isShown)
            return;

        _isGroupShown[group] = isShown;
        OnFilterChanged();
    }

    public void SetPresetShown(int preset, bool isShown)
    {
        if (preset < 0 || preset >= _presets.Count)
            return;

        foreach (var group in _presets[preset].Groups)
            _isGroupShown[group] = isShown;

        OnFilterChanged();
    }

    // A preset is on while all its ores are shown
    private bool IsPresetShown(OrePreset preset)
    {
        foreach (var group in preset.Groups)
        {
            if (!_isGroupShown[group])
                return false;
        }

        return true;
    }

    public void SetAllShown(bool isShown)
    {
        for (var i = 1; i < _isGroupShown.Length; i++)
            _isGroupShown[i] = isShown && _groups[i] != null;

        OnFilterChanged();
    }

    private void OnFilterChanged()
    {
        var hidden = new List<string>();
        for (var i = 1; i < _groups.Count; i++)
        {
            if (_groups[i] is { } group && !_isGroupShown[i])
                hidden.Add(group.Key);
        }
        ConfigManager.ConfigInstance.OreMode.hiddenOres = hidden.ToArray();

        foreach (var component in _loadedMapData.Values)
            (component as OreChunkMapComponent)?.RefreshFilter();

        RefreshLodTextures();
        UpdateDialog();
    }

    protected override int ExtraDialogHeight => 35 + PresetSectionHeight + GroupRowCount * GroupRowHeight;

    private int GroupRowCount => (CountGroups() + GroupColumns - 1) / GroupColumns;

    private int PresetRowCount => (_presets.Count + PresetColumns - 1) / PresetColumns;

    private int PresetSectionHeight => PresetRowCount == 0 ? 0 : PresetRowCount * GroupRowHeight + SectionGap;

    protected override void ComposeExtraElements(GuiComposer compo, ref ElementBounds row)
    {
        row = row.BelowCopy(0, 10).WithFixedSize(120, 25);
        compo.AddSmallButton(Lang.Get("ore-select-all"), () => OnSelectAll(true), row, EnumButtonStyle.Small, SelectAllButtonKey);
        compo.AddSmallButton(Lang.Get("ore-clear-all"), () => OnSelectAll(false), row.RightCopy(10), EnumButtonStyle.Small, ClearAllButtonKey);

        double left = row.fixedX;
        double top = row.fixedY + row.fixedHeight + 10;

        for (var i = 0; i < _presets.Count; i++)
        {
            double x = left + i % PresetColumns * PresetColumnWidth;
            double y = top + i / PresetColumns * GroupRowHeight;

            int index = i;
            compo.AddSwitch(isOn => OnPresetToggled(index, isOn), ElementBounds.Fixed(x, y, 20, 20), PresetSwitchKey + i, 20);
            compo.AddStaticText(_presets[i].Name, CairoFont.WhiteSmallText(), ElementBounds.Fixed(x + 26, y + 1, PresetColumnWidth - 30, 20));
        }

        top += PresetSectionHeight;
        var position = 0;
        for (var i = 1; i < _groups.Count; i++)
        {
            if (_groups[i] is not { } group)
                continue;

            int column = position % GroupColumns;
            int line = position / GroupColumns;
            position++;

            double x = left + column * GroupColumnWidth;
            double y = top + line * GroupRowHeight;
            var switchBounds = ElementBounds.Fixed(x, y, 20, 20);
            var swatchBounds = ElementBounds.Fixed(x + 26, y + 4, 12, 12);
            var textBounds = ElementBounds.Fixed(x + 44, y + 1, GroupColumnWidth - 50, 20);

            int index = i;
            compo.AddSwitch(isOn => OnGroupToggled(index, isOn), switchBounds, GroupSwitchKey + i, 20);
            compo.AddStaticCustomDraw(swatchBounds, (ctx, _, bounds) =>
            {
                var rgba = group.RgbaDoubles;
                ctx.SetSourceRGBA(rgba[0], rgba[1], rgba[2], 1);
                ctx.Rectangle(bounds.drawX, bounds.drawY, bounds.InnerWidth, bounds.InnerHeight);
                ctx.Fill();
            });
            compo.AddStaticText(group.Name, CairoFont.WhiteSmallText(), textBounds);
        }

        row = ElementBounds.Fixed(left, top + Math.Max(0, GroupRowCount - 1) * GroupRowHeight, GroupColumnWidth, 20);
    }

    protected override void OnDialogComposed(GuiComposer compo)
    {
        _dialogComposer = compo;
        UpdateDialog();
    }

    private void UpdateDialog()
    {
        if (_dialogComposer == null)
            return;

        _isUpdatingDialog = true;
        for (var i = 1; i < _groups.Count; i++)
        {
            if (_groups[i] != null)
                _dialogComposer.GetSwitch(GroupSwitchKey + i)?.SetValue(_isGroupShown[i]);
        }
        for (var i = 0; i < _presets.Count; i++)
            _dialogComposer.GetSwitch(PresetSwitchKey + i)?.SetValue(IsPresetShown(_presets[i]));
        _isUpdatingDialog = false;
    }

    private void OnPresetToggled(int preset, bool isOn)
    {
        if (_isUpdatingDialog)
            return;

        SetPresetShown(preset, isOn);
    }

    private bool OnSelectAll(bool isShown)
    {
        SetAllShown(isShown);
        return true;
    }

    private void OnGroupToggled(int group, bool isOn)
    {
        if (_isUpdatingDialog)
            return;

        SetGroupShown(group, isOn);
    }

    public override void OnMapClosedClient()
    {
        _dialogComposer = null;
        base.OnMapClosedClient();
    }

    protected override int LodTexelToRgba(int texel)
        => GetPixel(texel);

    // Keeps the first distinct groups taking the slots of all four columns in turn, so deeper ores of one column do not push out ores of others
    protected override int CombineLodColors(int a, int b, int c, int d)
    {
        Span<int> columns = stackalloc int[] { a, b, c, d };
        int packed = 0;
        int count = 0;

        for (var slot = 0; slot < MaxSlots && count < MaxSlots; slot++)
        {
            foreach (var column in columns)
            {
                int group = GetSlot(column, slot);
                if (group == 0 || ContainsGroup(packed, group))
                    continue;

                packed |= group << SlotShift(count++);
                if (count == MaxSlots)
                    break;
            }
        }

        return packed;
    }

    protected override void AppendLodHoverText(int color, StringBuilder hoverText)
    {
        var text = GetHoverText(color);
        if (text != null)
            hoverText.Append(text);
    }

    private int GetPixel(int packed)
    {
        for (var slot = 0; slot < MaxSlots; slot++)
        {
            int group = GetSlot(packed, slot);
            if (group == 0)
                break;

            if (IsShown(group))
                return _groups[group]!.Color;
        }

        return 0;
    }

    private bool IsShown(int group)
        => group < _isGroupShown.Length && _isGroupShown[group];

    private int GetGroup(int blockId)
        => blockId >= 0 && blockId < _groupByBlockId.Length ? _groupByBlockId[blockId] : 0;

    private static int SlotShift(int slot) => 24 - slot * 8;

    private static int GetSlot(int packed, int slot) => packed >>> SlotShift(slot) & 0xff;

    private static bool ContainsGroup(int packed, int group)
    {
        for (var slot = 0; slot < MaxSlots; slot++)
        {
            int stored = GetSlot(packed, slot);
            if (stored == group)
                return true;
            if (stored == 0)
                return false;
        }

        return false;
    }

    private int CountGroups()
    {
        var count = 0;
        foreach (var group in _groups)
        {
            if (group != null)
                count++;
        }

        return count;
    }

    /// <summary>
    /// Checks block palettes of the chunk. Returns true if ore may be there or if it cannot be proven otherwise.
    /// </summary>
    private bool HasOre(IWorldChunk chunk)
    {
        chunk.Unpack_ReadOnly();
        if (chunk.Data is not ChunkData data)
            return true;

        _blockIdsTmp.Clear();
        data.FuzzyListBlockIds(_blockIdsTmp);
        foreach (var blockId in _blockIdsTmp)
        {
            if (GetGroup(blockId) != 0)
                return true;
        }

        // FuzzyListBlockIds does not include fluids, but UnpackAndReadBlock may return them
        var fluidsLayer = data.fluidsLayer;
        var fluidsPalette = fluidsLayer?.palette;
        if (fluidsPalette == null)
            return false;

        int fluidsCount = Math.Min(fluidsLayer.paletteCount, fluidsPalette.Length);
        for (var i = 0; i < fluidsCount; i++)
        {
            if (GetGroup(fluidsPalette[i]) != 0)
                return true;
        }

        return false;
    }

    // Ores first, then the whitelist, a block belongs to the first group it matches
    private static List<(string Key, string Color)> GetGroupKeys()
    {
        var config = ConfigManager.ConfigInstance.OreMode;
        var keys = new List<(string, string)>();
        foreach (var (key, color) in config.ores)
            keys.Add((key, color));
        foreach (var (key, color) in config.whitelist)
            keys.Add((key, color));

        return keys.Count > MaxGroups ? keys.GetRange(0, MaxGroups) : keys;
    }

    // FNV-1a, string.GetHashCode is not stable between runs
    private static string HashGroupKeys()
    {
        uint hash = 2166136261;
        foreach (var (key, _) in GetGroupKeys())
        {
            foreach (var c in key + "\n")
                hash = (hash ^ c) * 16777619;
        }

        return hash.ToString("x8");
    }

    private static string GetGroupName(Block block)
    {
        var type = block.Variant?["type"];
        if (type != null && Lang.GetIfExists("ore-" + type) is { } name)
            return name;

        return block.GetHeldItemName(new ItemStack(block));
    }

    private sealed record OrePreset(string Name, int[] Groups);

    private sealed class OreGroup
    {
        public readonly string Key;
        public readonly int Color;
        public readonly double[] RgbaDoubles;
        public readonly string Name;

        public OreGroup(string key, string hexColor, string name)
        {
            Key = key;
            Color = ColorUtilExtensions.HexToColor(hexColor);
            RgbaDoubles = ColorUtilExtensions.HexToRgbaDoubles(hexColor);
            Name = name;
        }
    }
}
