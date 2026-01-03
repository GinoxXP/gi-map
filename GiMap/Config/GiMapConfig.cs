using Vintagestory.API.MathTools;

namespace GiMap.Config;

public class GiMapConfig
{
    public string Version = GiMapModSystem.Version;
    
    public float overlayAlpha = 1;
    public string errorColor = "#ff00ff";

    public Topographic TopographicMode = new Topographic();
    public Height HeightMode = new Height();
    public Fertility FertilityMode = new Fertility();
    public Precipitation PrecipitationMode = new Precipitation();
    public Temperature TemperatureMode = new Temperature();
    public GeologyActivity GeologyActivityMode = new GeologyActivity();
    public ChunkGrid ChunkGridMode = new ChunkGrid();
    public Light LightMode = new Light();
    public Ore OreMode = new Ore();
    public TemporalStability TemporalStabilityMode = new TemporalStability();
    
    public struct Topographic
    {
        public bool isEnabled = true;
        
        public string soilColor = "#c9ea9d";
        public string sandColor = "#ffffff";
        public string gravelColor = "#c8c8c8";
        public string stoneColor = "#969696";
        public string waterColor = "#22a4ab";
        public string iceColor = "#caedee";
        public string snowColor = "#e6e6ff";
        public string roadColor = "#323232";

        public Topographic()
        {
        }
    }
    
    public struct Height
    {
        public bool isEnabled = true;
        
        public Vec4i mountainColor = new(255, 0, 0, 255);
        public Vec4i plainColor = new(0, 255, 0, 255);
        public Vec4i lowLandColor = new(0, 150, 0, 255);
        public Vec4i seaLevelWaterColor = new(0, 0, 255, 255);
        public Vec4i highWaterColor = new(50, 255, 255, 255);
        public Vec4i trenchColor = new(0, 0, 150, 255);

        public Height()
        {
        }
    }

    public struct Fertility
    {
        public bool isEnabled = true;
        
        public string veryLowFertilityColor = "#ff0000";
        public string lowFertilityColor = "#ffab00";
        public string mediumFertilityColor = "#ffff00";
        public string highFertilityColor = "#00ff00";
        public string extreameFertilityColor = "#009600";

        public string waterColor = "#3232ff";
        public string noFertilityColor = "#646464";

        public Fertility()
        {
        }
    }

    public struct Precipitation
    {
        public bool isEnabled = true;
        
        public string veryHighPrecipitation = "#4682e6";
        public string highPrecipitation = "#28afaf";
        public string mediumPrecipitation = "#6ebe6e";
        public string lowPrecipitation = "#bebe64";
        public string veryLowPrecipitation = "#f0e6a0";

        public Precipitation()
        {
        }
    }

    public struct Temperature
    {
        public bool isEnabled = true;
        
        public string arcticCold = "#000064";
        public string extremeCold = "#0032b4";
        public string veryCold = "#0064dc";
        public string cold = "#64b4ff";

        public string cool = "#96dc96";
        public string temperate = "#32c832";
        public string mild = "#c8c850";

        public string warm = "#f0b400";
        public string hot = "#dc5000";
        public string extremeHot = "#961e1e";

        public Temperature()
        {
        }
    }

    public struct GeologyActivity
    {
        public bool isEnabled = true;
        
        public string lowActivity = "#646464";
        public string moderateActivity = "#b48c64";
        public string significantActivity = "#e6be50";
        public string highActivity = "#dc5000";
        public string extremeActivity = "#aa1e1e";

        public GeologyActivity()
        {
        }
    }

    public struct ChunkGrid
    {
        public bool isEnabled = true;
        
        public string borderColor = "#000000ff";
        public string smallBorderColor = "#00000096";
        public string verySmallBorderColor = "#00000064";
        public string backgroundColor = "#00000000";

        public ChunkGrid()
        {
        }
    }
    
    public struct Light
    {
        public bool isEnabled = true;

        public Light()
        {
            
        }
    }

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
            {"game:ore-korundum-.*", "#960018"}
        };

        public Dictionary<string, string> whitelist = new()
        {
            {"game:meteorite-iron", "#b4b9be"},
            {"game:rawclay-blue-.*", "#527aab"},
            {"game:rawclay-red-.*", "#b34b57"},
            {"game:rawclay-fire-.*", "#c99169"},
            {"game:rock-halite", "#ffc9dd"}
        };
        
        public string[] blacklist = new[]
        {
            "game:ore-quartz-.*",
            "game:ore-olivine-.*",
            "game:ore-flint"
        };
        
        
        public Ore()
        {
            
        }
    }

    public struct TemporalStability
    {
        public bool isEnabled = true;
        
        public string maxColor = "#228b22";
        public string highColor = "#55aa55";
        public string mediumColor = "#ffff00";
        public string lowColor = "#ffa500";
        public string minColor = "#dc143c";

        public TemporalStability()
        {
            
        }
    }
}