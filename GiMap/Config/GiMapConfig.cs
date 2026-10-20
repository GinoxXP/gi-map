using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace GiMap.Config;

public class GiMapConfig
{
    public string Version = GiMapModSystem.Version;
    
    public float overlayAlpha = 1;
    public string errorColor = "#ff00ff";
    // Smallest map zoom; vanilla stops at 0.25
    public float minZoom = 0.0625f;

    public Topographic TopographicMode = new Topographic();
    public Height HeightMode = new Height();
    public Bathymetric BathymetricMode = new Bathymetric();
    public Fertility FertilityMode = new Fertility();
    public Precipitation PrecipitationMode = new Precipitation();
    public Temperature TemperatureMode = new Temperature();
    public GeologyActivity GeologyActivityMode = new GeologyActivity();
    public ChunkGrid ChunkGridMode = new ChunkGrid();
    public Light LightMode = new Light();
    public TemporalStability TemporalStabilityMode = new TemporalStability();
    public Butterflies ButterfliesMode = new Butterflies();
    public Notes NotesMode = new Notes();

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

        public bool showContours = true;
        public int contourInterval = 5;
        public int majorContourEvery = 5;
        public string contourColor = "#a0784b";
        public string majorContourColor = "#5a3c1e";

        public bool showHeightLabels = true;
        public string peakColor = "#000000";
        public string labelOutlineColor = "#ffffffc8";
        public float contourLabelsMinZoom = 1.5f;
        public float peakLabelsMinZoom = 0.75f;

        public Topographic()
        {
        }
    }
    
    public struct Bathymetric
    {
        public bool isEnabled = true;

        public string landColor = "#e8e2d0";
        // Gradient by depth in blocks: shallowColor at 0, mediumColor at mediumDepth, deepColor from deepDepth
        public string shallowColor = "#c6ecf7";
        public string mediumColor = "#3f86c6";
        public string deepColor = "#0b2e6b";
        public int mediumDepth = 30;
        public int deepDepth = 90;

        public bool showContours = true;
        public int contourInterval = 2;
        public int majorContourEvery = 5;
        public string contourColor = "#4f86b5";
        public string majorContourColor = "#173f66";

        public bool showDepthLabels = true;
        public string deepestColor = "#000000";
        public string labelOutlineColor = "#ffffffc8";
        public float contourLabelsMinZoom = 1.5f;
        public float deepestLabelsMinZoom = 0.75f;

        public Bathymetric()
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

        // Block code regexes, checked from very low to extreme, the first match wins
        public string[] veryLowFertilityBlocks = { "game:soil-verylow-.*", "game:farmland-.*-verylow" };
        public string[] lowFertilityBlocks = { "game:soil-low-.*", "game:farmland-.*-low", "game:forestfloor-.*" };
        public string[] mediumFertilityBlocks = { "game:soil-medium-.*", "game:farmland-.*-medium" };
        public string[] highFertilityBlocks = { "game:soil-compost-.*", "game:farmland-.*-compost" };
        public string[] extreameFertilityBlocks = { "game:soil-high-.*", "game:farmland-.*-high" };

        // Blocks drawn with diagonal hatching over their fertility color
        public bool hatchingEnabled = true;
        public string[] hatchedBlocks = { "game:forestfloor-.*" };
        public string hatchColor = "#8b5a2b";

        // Snow is skipped, fertile blocks under it are drawn with hatching of the other diagonal
        public bool ignoreSnow = true;
        public string snowHatchColor = "#ffffff";

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

    public struct Butterflies
    {
        public bool isEnabled = true;

        // Variant code of the shown butterfly, e.g. "monarch" for "butterfly-monarch"
        public string selectedButterfly = "monarch";

        public string spawnColor = "#30b030";
        public string noSpawnColor = "#c02020";

        // Hash of the spawn condition groups the map db was generated with, the db is purged when they change
        public string groupsHash = "";

        public Butterflies()
        {
        }
    }

    // Notes are stored in vanilla waypoints, their colors come from the waypoints
    public struct Notes
    {
        public bool isEnabled = true;

        public Notes()
        {
        }
    }
}