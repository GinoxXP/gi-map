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