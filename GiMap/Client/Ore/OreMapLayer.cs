using System.Text.RegularExpressions;
using GiMap.Config;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Ore;

public class OreMapLayer : ABlockMapLayer
{
    public readonly Dictionary<string, int> ColorsByBlockCode = new();
    public readonly Dictionary<string, string> LocalizedNameByBlockCode = new();

    private readonly Dictionary<int, int> _colorByBlockId = new();

    public override string Title => MapTypes.Ore;

    public OreMapLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
    {
    }

    public override void OnLoaded()
    {
        base.OnLoaded();

        foreach (var block in api.World.Blocks)
        {
            if (block.Code == null) continue;
            RegisterBlock(block, ConfigManager.ConfigInstance.OreMode.ores);
            RegisterBlock(block, ConfigManager.ConfigInstance.OreMode.whitelist);
        }
    }

    private void RegisterBlock(Block block, Dictionary<string, string> ores)
    {
        var code = block.Code.ToString();
        foreach (var ore in ores)
        {
            if (!Regex.IsMatch(code, ore.Key)) continue;

            var color = ColorUtilExtensions.HexToColor(ore.Value);
            var name = block.GetHeldItemName(new ItemStack(block));

            if (ColorsByBlockCode.TryAdd(code, color))
            {
                LocalizedNameByBlockCode[code] = name;
                LocalizedNameByColor.TryAdd(color, name);
                _colorByBlockId[block.Id] = color;
            }
            break;
        }
    }

    protected override AChunkMapComponent CreateComponent(FastVec2i baseCord)
        => new OreChunkMapComponent(Capi, baseCord, this);

    protected override bool IsBlockValid(Block block)
        => block != null && _colorByBlockId.ContainsKey(block.Id);

    protected override int GetColor(BlockPos pos, Block block)
        => _colorByBlockId.TryGetValue(block.Id, out var color)
            ? color
            : ColorUtilExtensions.HexToColor(ConfigManager.ConfigInstance.errorColor);
}
