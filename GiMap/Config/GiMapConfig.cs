using Vintagestory.API.MathTools;

namespace GiMap.Config;

public class GiMapConfig
{
    public float overlayAlpha = 1;
    public int errorColor = ColorUtil.ColorFromRgba(255, 0, 255, 255);

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
        public int soilColor = ColorUtil.ColorFromRgba(201, 234, 157, 255);
        public int sandColor = ColorUtil.ColorFromRgba(255, 255, 255, 255);
        public int gravelColor = ColorUtil.ColorFromRgba(200, 200, 200, 255);
        public int stoneColor = ColorUtil.ColorFromRgba(150, 150, 150, 255);
        public int waterColor = ColorUtil.ColorFromRgba(34, 164, 171, 255);
        public int iceColor = ColorUtil.ColorFromRgba(202, 237, 238, 255);
        public int snowColor = ColorUtil.ColorFromRgba(230, 230, 255, 255);
        public int roadColor = ColorUtil.ColorFromRgba(50, 50, 50, 255);

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
        public int veryLowFertilityColor = ColorUtil.ColorFromRgba(255, 0, 0, 255);
        public int lowFertilityColor = ColorUtil.ColorFromRgba(255, 171, 0, 255);
        public int mediumFertilityColor = ColorUtil.ColorFromRgba(255, 255, 0, 255);
        public int highFertilityColor = ColorUtil.ColorFromRgba(0, 255, 0, 255);
        public int extreameFertilityColor = ColorUtil.ColorFromRgba(0, 150, 0, 255);
    
        public int waterColor = ColorUtil.ColorFromRgba(50, 50, 255, 255);
        public int noFertilityColor = ColorUtil.ColorFromRgba(100, 100, 100, 255);

        public Fertility()
        {
        }
    }

    public struct Precipitation
    {
        public int veryHighPrecipitation = ColorUtil.ColorFromRgba(70, 130, 230, 255);
        public int highPrecipitation = ColorUtil.ColorFromRgba(40, 175, 175, 255);
        public int mediumPrecipitation = ColorUtil.ColorFromRgba(110, 190, 110, 255);
        public int lowPrecipitation = ColorUtil.ColorFromRgba(190, 190, 100, 255);
        public int veryLowPrecipitation = ColorUtil.ColorFromRgba(240, 230, 160, 255);

        public Precipitation()
        {
        }
    }

    public struct Temperature
    {
        public int arcticCold = ColorUtil.ColorFromRgba(0, 0, 100, 255);
        public int extremeCold = ColorUtil.ColorFromRgba(0, 50, 180, 255);
        public int veryCold = ColorUtil.ColorFromRgba(0, 100, 220, 255);
        public int cold = ColorUtil.ColorFromRgba(100, 180, 255, 255);

        public int cool = ColorUtil.ColorFromRgba(150, 220, 150, 255);
        public int temperate = ColorUtil.ColorFromRgba(50, 200, 50, 255);
        public int mild = ColorUtil.ColorFromRgba(200, 200, 80, 255);

        public int warm = ColorUtil.ColorFromRgba(240, 180, 0, 255);
        public int hot = ColorUtil.ColorFromRgba(220, 80, 0, 255);
        public int extremeHot = ColorUtil.ColorFromRgba(150, 30, 30, 255);

        public Temperature()
        {
        }
    }

    public struct GeologyActivity
    {
        public int lowActivity = ColorUtil.ColorFromRgba(100, 100, 100, 255);
        public int moderateActivity = ColorUtil.ColorFromRgba(180, 140, 100, 255);
        public int significantActivity = ColorUtil.ColorFromRgba(230, 190, 80, 255);
        public int highActivity = ColorUtil.ColorFromRgba(220, 80, 0, 255);
        public int extremeActivity = ColorUtil.ColorFromRgba(170, 30, 30, 255);

        public GeologyActivity()
        {
        }
    }

    public struct ChunkGrid
    {
        public int borderColor = ColorUtil.ColorFromRgba(0, 0, 0, 255);
        public int smallBorderColor = ColorUtil.ColorFromRgba(0, 0, 0, 150);
        public int verySmallBorderColor = ColorUtil.ColorFromRgba(0, 0, 0, 100);
        public int backgroundColor = ColorUtil.ColorFromRgba(0, 0, 0, 0);

        public ChunkGrid()
        {
        }
    }

    public struct Ore
    {
        public int copperColor = ColorUtil.ColorFromRgba(184, 115, 51, 255);
        public int malachiteColor = ColorUtil.ColorFromRgba(11, 218, 81, 255);

        public int cassiteriteColor = ColorUtil.ColorFromRgba(70, 42, 41, 255);
        public int sphaleriteColor = ColorUtil.ColorFromRgba(149, 93, 38, 255);
        public int bismuthiniteColor = ColorUtil.ColorFromRgba(150, 150, 150, 255);
        public int galenaColor = ColorUtil.ColorFromRgba(115, 115, 130, 255);

        public int limoniteColor = ColorUtil.ColorFromRgba(181, 137, 53, 255);
        public int magnetiteColor = ColorUtil.ColorFromRgba(40, 40, 40, 255);
        public int hematiteColor = ColorUtil.ColorFromRgba(121, 6, 4, 255);
        public int meteoriteIronColor = ColorUtil.ColorFromRgba(180, 185, 190, 255);

        public int goldColor = ColorUtil.ColorFromRgba(255, 215, 0, 255);
        public int silverColor = ColorUtil.ColorFromRgba(192, 192, 192, 255);

        public int ilmeniteColor = ColorUtil.ColorFromRgba(46, 40, 40, 255);
        public int pentlanditeColor = ColorUtil.ColorFromRgba(180, 160, 100, 255);
        public int chromiteColor = ColorUtil.ColorFromRgba(50, 45, 45, 255);

        public int alumColor = ColorUtil.ColorFromRgba(240, 240, 240, 255);
        public int boraxColor = ColorUtil.ColorFromRgba(245, 245, 220, 255);
        public int sulfurColor = ColorUtil.ColorFromRgba(255, 255, 0, 255);
        public int cinnabarColor = ColorUtil.ColorFromRgba(227, 66, 52, 255);
        public int sylviteColor = ColorUtil.ColorFromRgba(255, 255, 255, 255);
        public int lapislazuliColor = ColorUtil.ColorFromRgba(3, 100, 175, 255);

        public int ligniteColor = ColorUtil.ColorFromRgba(105, 70, 40, 255);
        public int bituminouscoalColor = ColorUtil.ColorFromRgba(30, 30, 30, 255);
        public int anthraciteColor = ColorUtil.ColorFromRgba(10, 10, 10, 255);

        public int emeraldColor = ColorUtil.ColorFromRgba(80, 200, 120, 255);
        public int diamondColor = ColorUtil.ColorFromRgba(255, 255, 255, 255);
        public int peridotColor = ColorUtil.ColorFromRgba(173, 255, 47, 255);

        public int uraniumColor = ColorUtil.ColorFromRgba(60, 120, 90, 255);
        public int rhodochrositeColor = ColorUtil.ColorFromRgba(219, 112, 147, 255);
        public int graphiteColor = ColorUtil.ColorFromRgba(90, 90, 90, 255);
        public int fluoriteColor = ColorUtil.ColorFromRgba(128, 0, 128, 255);
        public int phosphoriteColor = ColorUtil.ColorFromRgba(169, 169, 169, 255);
        public int kerniteColor = ColorUtil.ColorFromRgba(230, 230, 230, 255);
        public int korundumColor = ColorUtil.ColorFromRgba(150, 0, 24, 255);
        
        public Ore()
        {
            
        }
    }

    public struct TemporalStability
    {
        public int maxColor = ColorUtil.ColorFromRgba(34, 139, 34, 255);
        public int highColor = ColorUtil.ColorFromRgba(85, 170, 85, 255);
        public int mediumColor = ColorUtil.ColorFromRgba(255, 255, 0, 255);
        public int lowColor = ColorUtil.ColorFromRgba(255, 165, 0, 255);
        public int minColor = ColorUtil.ColorFromRgba(220, 20, 60, 255);

        public TemporalStability()
        {
            
        }
    }
}