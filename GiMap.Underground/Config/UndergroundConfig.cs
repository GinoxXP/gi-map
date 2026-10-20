using Vintagestory.API.MathTools;

namespace GiMap.Underground.Config;

public class UndergroundConfig
{
    public Ore OreMode = new Ore();
    public Caves CavesMode = new Caves();

    public struct Ore
    {
        public bool isEnabled = false;

        public Dictionary<string, string> ores = new()
        {
            {"game:ore-.*-nativecopper-.*", "#b87333"},
            {"game:ore-.*-malachite-.*", "#0bda51"},
            {"game:ore-.*-cassiterite-.*", "#462a29"},
            {"game:ore-.*-sphalerite-.*", "#955d26"},
            {"game:ore-.*-bismuthinite-.*", "#969696"},
            {"game:ore-.*-galena-.*", "#737382"},
            {"game:ore-.*-limonite-.*", "#b58935"},
            {"game:ore-.*-magnetite-.*", "#282828"},
            {"game:ore-.*-hematite-.*", "#790604"},
            {"game:ore-.*_nativegold-.*", "#ffd700"},
            {"game:ore-.*_nativesilver-.*", "#c0c0c0"},
            {"game:ore-.*-ilmenite-.*", "#2e2828"},
            {"game:ore-.*-pentlandite-.*", "#b4a064"},
            {"game:ore-.*-chromite-.*", "#322d2d"},
            {"game:ore-alum-.*", "#f0f0f0"},
            {"game:ore-borax-.*", "#f5f5dc"},
            {"game:ore-sulfur-.*", "#ffff00"},
            {"game:ore-cinnabar-.*", "#e34234"},
            {"game:ore-sylvite-.*", "#fefefe"},
            {"game:ore-lapislazuli-.*", "#0364af"},
            {"game:ore-lignite-.*", "#694628"},
            {"game:ore-bituminouscoal-.*", "#1e1e1e"},
            {"game:ore-anthracite-.*", "#0a0a0a"},
            {"game:ore-.*-emerald-.*", "#50c878"},
            {"game:ore-.*-diamond-.*", "#ffffff"},
            {"game:ore-.*_peridot-.*", "#adff2f"},
            {"game:ore-.*-uranium-.*", "#3c785a"},
            {"game:ore-.*-rhodochrosite-.*", "#db7093"},
            {"game:ore-graphite-.*", "#5a5a5a"},
            {"game:ore-fluorite-.*", "#800080"},
            {"game:ore-phosphorite-.*", "#a9a9a9"},
            {"game:ore-kernite-.*", "#e6e6e6"},
            {"game:ore-korundum-.*", "#960018"},
            {"game:ore-quartz-.*", "#e6dfd3"},
            {"game:ore-olivine-.*", "#8a9a3b"}
        };

        public Dictionary<string, string> whitelist = new()
        {
            {"game:meteorite-iron", "#b4b9be"},
            {"game:rawclay-blue-.*", "#527aab"},
            {"game:rawclay-red-.*", "#b34b57"},
            {"game:rawclay-fire-.*", "#c99169"},
            {"game:rock-halite", "#ffc9dd"},
            {"game:rock-whitemarble", "#ece9e2"},
            {"game:rock-redmarble", "#d49a9a"},
            {"game:rock-greenmarble", "#5f8f6c"}
        };

        public string[] blacklist = new[]
        {
            "game:ore-flint"
        };

        // Keys of ores and whitelist entries hidden on the map
        public string[] hiddenOres = Array.Empty<string>();

        // Switches toggling several ores at once: name (lang key "ore-preset-<name>") -> keys of ores and whitelist entries
        public Dictionary<string, string[]> presets = new()
        {
            {"copper", new[] {"game:ore-.*-nativecopper-.*", "game:ore-.*-malachite-.*"}},
            {"bronze", new[] {"game:ore-.*-cassiterite-.*", "game:ore-.*-bismuthinite-.*", "game:ore-.*-sphalerite-.*", "game:ore-.*_nativegold-.*", "game:ore-.*_nativesilver-.*"}},
            {"iron", new[] {"game:ore-.*-limonite-.*", "game:ore-.*-magnetite-.*", "game:ore-.*-hematite-.*", "game:meteorite-iron"}},
            {"coal", new[] {"game:ore-lignite-.*", "game:ore-bituminouscoal-.*", "game:ore-anthracite-.*"}},
            {"quartz-olivine", new[] {"game:ore-quartz-.*", "game:ore-olivine-.*"}},
            {"precious", new[] {"game:ore-.*_nativegold-.*", "game:ore-.*_nativesilver-.*"}},
            {"clay", new[] {"game:rawclay-blue-.*", "game:rawclay-red-.*", "game:rawclay-fire-.*"}},
            {"marble", new[] {"game:rock-whitemarble", "game:rock-redmarble", "game:rock-greenmarble"}}
        };

        
        public Ore()
        {
            
        }
    }

    public struct Caves
    {
        public bool isEnabled = true;

        // 1 is the deepest layer, every layer is one chunk (32 blocks) high
        public int selectedLayer = 4;
        public bool followPlayer = true;

        public string solidColor = "#1e1e1e";
        public string voidColor = "#000000";
        public string stoneColor = "#b4b4b4";
        public string soilColor = "#a0784b";
        public string sandColor = "#e6dcaa";
        public string gravelColor = "#8c8c8c";
        public string oreColor = "#e6be50";
        public string waterColor = "#3264ff";
        public string lavaColor = "#ff5000";
        public string iceColor = "#caedee";
        public string otherColor = "#c86eb4";

        public Caves()
        {
        }
    }
}
