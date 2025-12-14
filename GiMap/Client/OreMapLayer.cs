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

    private readonly string[] _blackListOres = {"game:ore-quartz-", "game:ore-olivine-", "game:ore-flint"};
    private readonly string[] _whitelistBlocks = {"game:meteorite-iron"};

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
            var code = block.Code.ToString();

            if (Regex.IsMatch(code, "game:ore-.*-nativecopper-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.copperColor),
                    block.GetHeldItemName(new ItemStack(block)));
               
        
            if (Regex.IsMatch(code, "game:ore-.*-malachite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.malachiteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-.*-cassiterite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.cassiteriteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-sphalerite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.sphaleriteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-bismuthinite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.bismuthiniteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-galena-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.galenaColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-.*-limonite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.limoniteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-magnetite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.magnetiteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-hematite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.hematiteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (code == "game:meteorite-iron")
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.meteoriteIronColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-.*_nativegold-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.goldColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*_nativesilver-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.silverColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-.*-ilmenite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.ilmeniteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-pentlandite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.pentlanditeColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-chromite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.chromiteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-alum-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.alumColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-borax-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.boraxColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-sulfur-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.sulfurColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-cinnabar-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.cinnabarColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-sylvite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.sylviteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-lapislazuli-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.lapislazuliColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-lignite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.ligniteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-bituminouscoal-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.bituminouscoalColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-anthracite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.anthraciteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-.*-emerald-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.emeraldColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-diamond-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.diamondColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*_peridot-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.peridotColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            
            if (Regex.IsMatch(code, "game:ore-.*-uranium-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.uraniumColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-.*-rhodochrosite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.rhodochrositeColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-graphite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.graphiteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-fluorite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.fluoriteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-phosphorite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.phosphoriteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-kernite-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.kerniteColor),
                    block.GetHeldItemName(new ItemStack(block)));
            
            if (Regex.IsMatch(code, "game:ore-korundum-.*"))
                FillDictionaries(
                    code,
                    ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.korundumColor),
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
        => (block.BlockMaterial == EnumBlockMaterial.Ore &&
           !_blackListOres.Any(b => block.Code.ToString().Contains(b)))
            || _whitelistBlocks.Any(b => block.Code.ToString().Contains(b));
    

    protected override int GetColor(BlockPos pos)
    {
        var block = api.World.BlockAccessor.GetBlock(pos);
        var code = block.Code.ToString();

        if (ColorsByBlockCode.TryGetValue(code, out var color))
            return color;
        
        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
    }
}