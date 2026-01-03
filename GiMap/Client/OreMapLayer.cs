using System.Text;
using System.Text.RegularExpressions;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class OreMapLayer : ABlockMapLayer
{
    public readonly Dictionary<string, int> ColorsByBlockCode = new();
    public readonly Dictionary<string, string> LocalizedNameByBlockCode = new();

    public override string Title => MapTypes.Ore;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    private void FillDictionaries(string blockCode, int color, string localizedName)
    {
        ColorsByBlockCode.Add(blockCode, color);
        LocalizedNameByBlockCode.Add(blockCode, localizedName);
    }
    
    public override void OnLoaded()
    {
        base.OnLoaded();
        
        var blocks = api.World.Blocks;
        foreach (var block in blocks)
        {
            RegisterBlock(block, ConfigManager.ConfigInstance.OreMode.ores);
            RegisterBlock(block, ConfigManager.ConfigInstance.OreMode.whitelist);
        }
    }

    private void RegisterBlock(Block block, Dictionary<string, string> ores)
    {
        var code = block.Code.ToString();
        foreach (var ore in ores)
        {
            if (Regex.IsMatch(code, ore.Key))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ore.Value),
                    block.GetHeldItemName(new ItemStack(block)));
        }
    }
    
    public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
    {
        if (!Active)
            return;

        foreach (KeyValuePair<FastVec2i, AChunkMapComponent> loadedMapDatum in _loadedMapData)
            loadedMapDatum.Value.OnMouseMove(args, mapElem, hoverText);
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new OreChunkMapComponent(_capi, baseCord, this);

    protected override bool IsBlockValid(Block block)
    {
        if (!base.IsBlockValid(block))
            return false;

        string code = block.Code.ToString();

        bool isOre = block.BlockMaterial == EnumBlockMaterial.Ore;
        bool blacklisted = ConfigManager.ConfigInstance.OreMode.blacklist.Any(b => Regex.IsMatch(code, b));
        bool whitelisted = ConfigManager.ConfigInstance.OreMode.whitelist.Any(b => Regex.IsMatch(code, b.Key));

        return (isOre && !blacklisted) || whitelisted;
    }
    

    protected override int GetColor(BlockPos pos)
    {
        var block = api.World.BlockAccessor.GetBlock(pos);
        var code = block.Code.ToString();

        if (ColorsByBlockCode.TryGetValue(code, out var color))
            return color;
        
        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
    }
}