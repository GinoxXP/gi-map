using System.Text;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Butterflies;

/// <summary>
/// Highlights chunks where the selected butterfly may spawn by climate and height.
/// Every pixel of a chunk stores the mask of condition groups matched by any column of the chunk
/// plus <see cref="GeneratedBit"/>, so changing the butterfly does not regenerate chunks.
/// </summary>
public class ButterfliesMapLayer : AMapLayer
{
    private const int ChunkPixelCount = 1024;
    private const int GeneratedBit = 1 << 31;
    private const int MaxGroups = 31;
    private const string SpeciesDropDownKey = "butterflies-species";
    private const string SearchInputKey = "butterflies-search";
    private const string AnyCode = "*";
    private const int MaxHoverNames = 8;

    public override string Title => MapTypes.Butterflies;

    // Group indices are stored in the db, a change of the groups is detected by groupsHash in OnLoaded
    protected override string DbSuffix => "-v1";

    protected override bool HasLodTexelConversion => true;

    private ButterflySpeciesList _species = new();
    private volatile bool _isLoaded;
    private ButterflySpecies? _selected;
    private GuiComposer? _dialogComposer;
    // "Any" first, then the butterflies
    private List<ButterflySpecies> _choices = new();
    private ButterflySpecies? _any;
    private int _allGroupsMask;
    private readonly int _spawnColor;
    private readonly int _noSpawnColor;

    public ButterfliesMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
        // Pixels are drawn into the texture with blending, translucent ones would darken on every update
        var config = ConfigManager.ConfigInstance.ButterfliesMode;
        _spawnColor = Opaque(ColorUtilExtensions.HexToColor(config.spawnColor));
        _noSpawnColor = Opaque(ColorUtilExtensions.HexToColor(config.noSpawnColor));
    }

    // Entity types and assets are complete only once the world is loaded
    public override void OnLoaded()
    {
        base.OnLoaded();
        if (api.Side != EnumAppSide.Client)
            return;

        _species = ButterflySpeciesList.Load(api, MaxGroups);
        _allGroupsMask = (int)((1L << _species.Groups.Count) - 1);

        ref var config = ref ConfigManager.ConfigInstance.ButterfliesMode;
        string selectedCode = config.selectedButterfly;
        _any = new ButterflySpecies(AnyCode, Lang.Get("butterflies-any"), -1);
        _choices = _species.Species.Count == 0 ? new List<ButterflySpecies>() : _species.Species.Prepend(_any).ToList();
        _selected = _choices.FirstOrDefault(s => s.Code == selectedCode) ?? _choices.FirstOrDefault();

        string hash = HashGroups();
        if (config.groupsHash != hash)
        {
            config.groupsHash = hash;
            Reload();
        }

        _isLoaded = true;
    }

    protected override int[] GenerateChunkImage(FastVec2i chunkPos, IMapChunk mc)
    {
        if (!_isLoaded || !TryLoadChunks(chunkPos))
            return null;

        var blockAccessor = api.World.BlockAccessor;
        int seaLevel = api.World.SeaLevel;
        int mapHeight = blockAccessor.MapSizeY;
        var vec2i = new Vec2i();
        var pos = new BlockPos(0);
        int baseWorldX = chunkPos.X * 32;
        int baseWorldZ = chunkPos.Y * 32;
        int mask = 0;

        for (var k = 0; k < ChunkPixelCount && mask != _allGroupsMask; k++)
        {
            int topBlockHeight = mc.RainHeightMap[k];
            int topChunkIndex = topBlockHeight / 32;
            if (topBlockHeight <= 0 || topChunkIndex >= _chunksTmp.Length)
                continue;

            MapUtil.PosInt2d(k, 32L, vec2i);
            int blockId = _chunksTmp[topChunkIndex].UnpackAndReadBlock(MapUtil.Index3d(vec2i.X, topBlockHeight % 32, vec2i.Y, 32, 32), 3);
            var block = api.World.Blocks[blockId];
            // Butterflies spawn in plants above the ground, never over water or ice
            if (block == null || block.IsLiquid() || block.BlockMaterial == EnumBlockMaterial.Ice)
                continue;

            int y = topBlockHeight + 1;
            pos.Set(baseWorldX + vec2i.X, y, baseWorldZ + vec2i.Y);
            var climate = blockAccessor.GetClimateAt(pos, EnumGetClimateMode.WorldGenValues);
            if (climate == null)
                continue;

            // 0..1 is world bottom to sea level, 1..2 is sea level to world top
            float relativeY = y <= seaLevel ? (float)y / seaLevel : 1 + (float)(y - seaLevel) / (mapHeight - seaLevel);

            for (var group = 0; group < _species.Groups.Count; group++)
            {
                int bit = 1 << group;
                if ((mask & bit) != 0)
                    continue;

                var condition = _species.Groups[group];
                if (relativeY >= condition.MinY && relativeY <= condition.MaxY &&
                    condition.MatchesClimate(climate) && condition.MatchesForestation(climate))
                    mask |= bit;
            }
        }

        ClearChunks();

        var result = new int[ChunkPixelCount];
        Array.Fill(result, mask | GeneratedBit);
        return result;
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new ButterfliesChunkMapComponent(_capi, baseCord, this);

    /// <summary>
    /// Colors of the selected butterfly from stored chunk masks
    /// </summary>
    public int[] BuildPixels(int[] data)
    {
        var pixels = new int[ChunkPixelCount];
        int count = Math.Min(data.Length, ChunkPixelCount);
        for (var k = 0; k < count; k++)
            pixels[k] = GetPixel(data[k]);

        return pixels;
    }

    public string? GetHoverText(int mask)
    {
        if ((mask & GeneratedBit) == 0 || _selected == null)
            return null;

        if (_selected != _any)
            return Lang.Get(CanSpawn(mask) ? "butterflies-can-spawn" : "butterflies-cannot-spawn", _selected.Name);

        var names = _species.Species.Where(s => (mask & (1 << s.Group)) != 0).Select(s => s.Name).ToList();
        if (names.Count == 0)
            return Lang.Get("butterflies-any-cannot-spawn");

        var text = string.Join(", ", names.Take(MaxHoverNames));
        if (names.Count > MaxHoverNames)
            text += ", …";

        return Lang.Get("butterflies-any-can-spawn", names.Count, text);
    }

    private int GetPixel(int mask)
    {
        if ((mask & GeneratedBit) == 0)
            return 0;

        return CanSpawn(mask) ? _spawnColor : _noSpawnColor;
    }

    private bool CanSpawn(int mask)
        => _selected != null && (mask & (_selected == _any ? _allGroupsMask : 1 << _selected.Group)) != 0;

    private void SetSelected(string code)
    {
        var species = _choices.FirstOrDefault(s => s.Code == code);
        if (species == null || species == _selected)
            return;

        _selected = species;
        ConfigManager.ConfigInstance.ButterfliesMode.selectedButterfly = code;

        foreach (var component in _loadedMapData.Values)
            (component as ButterfliesChunkMapComponent)?.RefreshFilter();

        RefreshLodTextures();
    }

    protected override int ExtraDialogHeight => 100;

    protected override void ComposeExtraElements(GuiComposer compo, ref ElementBounds row)
    {
        if (_species.Species.Count == 0)
            return;

        row = row.BelowCopy(0, 10).WithFixedSize(250, 20);
        compo.AddStaticText(Lang.Get("butterflies-search"), CairoFont.WhiteSmallText(), row);
        row = row.BelowCopy(0, 2).WithFixedSize(250, 28);
        compo.AddTextInput(row, OnSearchChanged, CairoFont.TextInput(), SearchInputKey);

        var values = _choices.Select(s => s.Code).ToArray();
        var names = _choices.Select(s => s.Name).ToArray();
        int selectedIndex = Math.Max(0, Array.IndexOf(values, _selected?.Code));

        row = row.BelowCopy(0, 8).WithFixedSize(250, 26);
        compo.AddDropDown(values, names, selectedIndex, (code, _) => SetSelected(code), row, SpeciesDropDownKey);
    }

    protected override void OnDialogComposed(GuiComposer compo)
        => _dialogComposer = compo;

    public override void OnMapClosedClient()
    {
        _dialogComposer = null;
        base.OnMapClosedClient();
    }

    // Leaves in the list "Any" and the butterflies whose name or code contains the text; the first found one is picked
    // when the shown butterfly is filtered out or "Any" is shown, so the map follows the search
    private void OnSearchChanged(string text)
    {
        var dropDown = _dialogComposer?.GetDropDown(SpeciesDropDownKey);
        if (dropDown == null || _any == null)
            return;

        text = text?.Trim() ?? "";
        var found = _species.Species
            .Where(s => text.Length == 0 ||
                        s.Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
                        s.Code.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (found.Count == 0)
            return;

        var shown = found.Prepend(_any).ToList();
        dropDown.SetList(shown.Select(s => s.Code).ToArray(), shown.Select(s => s.Name).ToArray());

        var selected = _selected != null && (found.Contains(_selected) || text.Length == 0) ? _selected : found[0];
        dropDown.SetSelectedValue(selected.Code);
        SetSelected(selected.Code);
    }

    protected override int LodTexelToRgba(int texel)
        => GetPixel(texel);

    // A LOD texel covers the chunks of all four, so any of them may have the butterfly
    protected override int CombineLodColors(int a, int b, int c, int d)
        => a | b | c | d;

    protected override void AppendLodHoverText(int color, StringBuilder hoverText)
    {
        var text = GetHoverText(color);
        if (text != null)
            hoverText.Append(text);
    }

    private static int Opaque(int color)
        => color | unchecked((int)0xff000000);

    // FNV-1a, string.GetHashCode is not stable between runs
    private string HashGroups()
    {
        var key = _species.GroupsKey();
        uint hash = 2166136261;
        foreach (var c in key)
            hash = (hash ^ c) * 16777619;

        return hash.ToString("x8");
    }
}
