using GiMap.Underground.Caves;
using GiMap.Underground.Config;
using OreMapLayer = GiMap.Underground.Ore.OreMapLayer;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GiMap.Underground;

/// <summary>
/// Add-on with the modes showing what is under the ground (caves, ores). It is a separate mod,
/// so servers can forbid it by its mod id and still allow the core GiMap.
/// </summary>
public class UndergroundModSystem : ModSystem
{
    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    // After GiMapModSystem, its StartPre loads the core config and registers the core modes
    public override double ExecuteOrder() => base.ExecuteOrder() + 0.01;

    public override void StartPre(ICoreAPI api)
    {
        base.StartPre(api);

        UndergroundConfigManager.LoadModConfig(api);

        // Tab orders fit between the core modes, see GiMapModSystem.RegisterModes
        GiMapModes.Register(UndergroundMapTypes.Caves, 40,
            () => UndergroundConfigManager.ConfigInstance.CavesMode.isEnabled,
            value => UndergroundConfigManager.ConfigInstance.CavesMode.isEnabled = value,
            () => UndergroundConfigManager.SaveModConfig(api));
        GiMapModes.Register(UndergroundMapTypes.Ore, 110,
            () => UndergroundConfigManager.ConfigInstance.OreMode.isEnabled,
            value => UndergroundConfigManager.ConfigInstance.OreMode.isEnabled = value,
            () => UndergroundConfigManager.SaveModConfig(api));
        GiMapModes.OnReset(() => UndergroundConfigManager.ResetModConfig(api));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        var mapManager = api.ModLoader.GetModSystem<WorldMapManager>();
        var config = UndergroundConfigManager.ConfigInstance;

        if (config.OreMode.isEnabled)
            mapManager.RegisterMapLayer<OreMapLayer>(UndergroundMapTypes.Ore, 9);

        if (config.CavesMode.isEnabled)
            mapManager.RegisterMapLayer<CavesMapLayer>(UndergroundMapTypes.Caves, 11);
    }
}
