using System.Text.RegularExpressions;
using GiMap.Config;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client;

public class OreMapLayer : ABlockMapLayer
{
    private readonly string[] _blackListOres = {"game:ore-quartz-", "game:ore-olivine-", "game:ore-flint"};
    private readonly string[] _whitelistBlocks = {"game:meteorite-iron"};

    public override string Title => MapTypes.Ore;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
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

        if (Regex.IsMatch(code, "game:ore-.*-nativecopper-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.copperColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-malachite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.malachiteColor);
        
        
        if (Regex.IsMatch(code, "game:ore-.*-cassiterite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.cassiteriteColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-sphalerite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.sphaleriteColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-bismuthinite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.bismuthiniteColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-galena-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.galenaColor);
        
        
        if (Regex.IsMatch(code, "game:ore-.*-limonite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.limoniteColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-magnetite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.magnetiteColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-hematite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.hematiteColor);
        
        if (code == "game:meteorite-iron")
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.meteoriteIronColor);
        
        
        if (Regex.IsMatch(code, "game:ore-.*_nativegold-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.goldColor);
        
        if (Regex.IsMatch(code, "game:ore-.*_nativesilver-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.silverColor);
        
        
        if (Regex.IsMatch(code, "game:ore-.*-ilmenite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.ilmeniteColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-pentlandite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.pentlanditeColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-chromite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.chromiteColor);
        
        
        if (Regex.IsMatch(code, "game:ore-alum-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.alumColor);
        
        if (Regex.IsMatch(code, "game:ore-borax-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.boraxColor);
        
        if (Regex.IsMatch(code, "game:ore-sulfur-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.sulfurColor);
        
        if (Regex.IsMatch(code, "game:ore-cinnabar-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.cinnabarColor);
        
        if (Regex.IsMatch(code, "game:ore-sylvite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.sylviteColor);
        
        if (Regex.IsMatch(code, "game:ore-lapislazuli-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.lapislazuliColor);
        
        
        if (Regex.IsMatch(code, "game:ore-lignite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.ligniteColor);
        
        if (Regex.IsMatch(code, "game:ore-bituminouscoal-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.bituminouscoalColor);
        
        if (Regex.IsMatch(code, "game:ore-anthracite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.anthraciteColor);
        
        
        if (Regex.IsMatch(code, "game:ore-.*-emerald-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.emeraldColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-diamond-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.diamondColor);
        
        if (Regex.IsMatch(code, "game:ore-.*_peridot-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.peridotColor);
        
        
        if (Regex.IsMatch(code, "game:ore-.*-uranium-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.uraniumColor);
        
        if (Regex.IsMatch(code, "game:ore-.*-rhodochrosite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.rhodochrositeColor);
        
        if (Regex.IsMatch(code, "game:ore-graphite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.graphiteColor);
        
        if (Regex.IsMatch(code, "game:ore-fluorite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.fluoriteColor);
        
        if (Regex.IsMatch(code, "game:ore-phosphorite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.phosphoriteColor);
        
        if (Regex.IsMatch(code, "game:ore-kernite-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.kerniteColor);
        
        if (Regex.IsMatch(code, "game:ore-korundum-.*"))
            return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.OreMode.korundumColor);
        
        return ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
    }
}