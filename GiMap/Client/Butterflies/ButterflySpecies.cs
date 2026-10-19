using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;

namespace GiMap.Client.Butterflies;

/// <summary>
/// Butterfly variant with the index of its spawn condition group
/// </summary>
public sealed record ButterflySpecies(string Code, string Name, int Group);

/// <summary>
/// Runtime spawn conditions of butterfly variants. Variants with equal conditions share a group,
/// so a chunk stores one bit per group instead of one per variant.
/// </summary>
public sealed class ButterflySpeciesList
{
    private const string EntityCodePrefix = "butterfly-";
    private static readonly AssetLocation EntityAsset = new("game:entities/animal/invertebrate/butterfly.json");

    public readonly List<ButterflySpecies> Species = new();
    public readonly List<ClimateSpawnCondition> Groups = new();

    public static ButterflySpeciesList Load(ICoreAPI api, int maxGroups)
    {
        var conditions = FromEntityTypes(api);
        var source = "entity types";
        if (conditions.Count == 0)
        {
            conditions = FromAsset(api);
            source = "asset";
        }

        var list = new ButterflySpeciesList();
        var groupByKey = new Dictionary<string, int>();
        var skipped = 0;
        foreach (var (code, condition) in conditions)
        {
            string key = GroupKey(condition);
            if (!groupByKey.TryGetValue(key, out int group))
            {
                if (list.Groups.Count >= maxGroups)
                {
                    skipped++;
                    continue;
                }

                group = list.Groups.Count;
                groupByKey[key] = group;
                list.Groups.Add(condition);
            }

            string name = Lang.GetIfExists("item-creature-butterfly-" + code) ?? code;
            list.Species.Add(new ButterflySpecies(code, name, group));
        }

        if (skipped > 0)
            api.Logger.Warning("[GiMap] {0} butterflies are not shown: more than {1} distinct spawn conditions", skipped, maxGroups);

        api.Logger.Notification("[GiMap] Loaded {0} butterflies in {1} spawn condition groups from {2}", list.Species.Count, list.Groups.Count, source);
        list.Species.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return list;
    }

    /// <summary>
    /// Text of all group conditions, changes when the spawn conditions change
    /// </summary>
    public string GroupsKey()
        => string.Join("\n", Groups.Select(GroupKey));

    // Includes mod changes, but the client may not have the server part of entity types
    private static List<(string Code, ClimateSpawnCondition Condition)> FromEntityTypes(ICoreAPI api)
    {
        var result = new List<(string, ClimateSpawnCondition)>();
        foreach (var type in api.World.EntityTypes)
        {
            if (type?.Code == null)
                continue;

            var path = type.Code.Path;
            if (!path.StartsWith(EntityCodePrefix))
                continue;

            var spawnConditions = type.Server?.SpawnConditions;
            ClimateSpawnCondition? condition = spawnConditions?.Runtime ?? spawnConditions?.Climate;
            if (condition != null)
                result.Add((path[EntityCodePrefix.Length..], condition));
        }

        return result;
    }

    private static List<(string Code, ClimateSpawnCondition Condition)> FromAsset(ICoreAPI api)
    {
        var result = new List<(string, ClimateSpawnCondition)>();
        try
        {
            var root = ReadEntityAsset(api);
            var states = root?["variantgroups"]?[0]?["states"]?.ToObject<string[]>();
            if (root == null || states == null)
                return result;

            var server = GetIgnoreCase(root, "server");
            var spawnConditions = server == null ? null : GetIgnoreCase(server, "spawnconditions");
            var byType = spawnConditions == null ? null : GetIgnoreCase(spawnConditions, "climateByType");
            if (byType == null)
                return result;

            foreach (var state in states)
            {
                var code = new AssetLocation("game", EntityCodePrefix + state);
                foreach (var property in byType.Properties())
                {
                    if (!WildcardUtil.Match(new AssetLocation(property.Name), code))
                        continue;

                    if (property.Value.ToObject<ClimateSpawnCondition>() is { } condition)
                        result.Add((state, condition));
                    break;
                }
            }
        }
        catch (Exception e)
        {
            api.Logger.Warning("[GiMap] Cannot read butterfly spawn conditions: {0}", e.Message);
        }

        return result;
    }

    // The client asset manager may not keep entity assets, then the file of the game install is read
    private static JObject? ReadEntityAsset(ICoreAPI api)
    {
        var asset = api.Assets.TryGet(EntityAsset, loadAsset: true);
        if (asset != null)
            return JObject.Parse(asset.ToText());

        var file = Path.Combine(GamePaths.AssetsPath, "survival", "entities", "animal", "invertebrate", "butterfly.json");
        return File.Exists(file) ? JObject.Parse(File.ReadAllText(file)) : null;
    }

    private static JObject? GetIgnoreCase(JObject obj, string name)
        => obj.GetValue(name, StringComparison.OrdinalIgnoreCase) as JObject;

    private static string GroupKey(ClimateSpawnCondition c)
        => FormattableString.Invariant(
            $"{c.MinTemp},{c.MaxTemp},{c.MinRain},{c.MaxRain},{c.MinForest},{c.MaxForest},{c.MinShrubs},{c.MaxShrubs},{c.MinForestOrShrubs},{c.MinY},{c.MaxY}");
}
