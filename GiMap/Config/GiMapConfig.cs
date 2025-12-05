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
    public Ore OreMode = new Ore();
    public TemporalStability TemporalStabilityMode = new TemporalStability();
    
    public struct Topographic
    {
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
        public string borderColor = "#000000ff";
        public string smallBorderColor = "#00000096";
        public string verySmallBorderColor = "#00000064";
        public string backgroundColor = "#00000000";

        public ChunkGrid()
        {
        }
    }

    public struct Ore
    {
        public string copperColor = "#b87333";
        public string malachiteColor = "#0bda51";
        
        public string cassiteriteColor = "#462a29";
        public string sphaleriteColor = "#955d26";
        public string bismuthiniteColor = "#969696";
        public string galenaColor = "#737382";
        
        public string limoniteColor = "#b58935";
        public string magnetiteColor = "#282828";
        public string hematiteColor = "#790604";
        public string meteoriteIronColor = "#b4b9be";
        
        public string goldColor = "#ffd700";
        public string silverColor = "#c0c0c0";
        
        public string ilmeniteColor = "#2e2828";
        public string pentlanditeColor = "#b4a064";
        public string chromiteColor = "#322d2d";
        
        public string alumColor = "#f0f0f0";
        public string boraxColor = "#f5f5dc";
        public string sulfurColor = "#ffff00";
        public string cinnabarColor = "#e34234";
        public string sylviteColor = "#ffffff";
        public string lapislazuliColor = "#0364af";
        
        public string ligniteColor = "#694628";
        public string bituminouscoalColor = "#1e1e1e";
        public string anthraciteColor = "#0a0a0a";
        
        public string emeraldColor = "#50c878";
        public string diamondColor = "#ffffff";
        public string peridotColor = "#adff2f";
        
        public string uraniumColor = "#3c785a";
        public string rhodochrositeColor = "#db7093";
        public string graphiteColor = "#5a5a5a";
        public string fluoriteColor = "#800080";
        public string phosphoriteColor = "#a9a9a9";
        public string kerniteColor = "#e6e6e6";
        public string korundumColor = "#960018";
        
        public Ore()
        {
            
        }
    }

    public struct TemporalStability
    {
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