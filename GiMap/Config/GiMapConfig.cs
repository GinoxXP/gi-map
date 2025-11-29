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
        public Vec4i _mountainColor = new(255, 0, 0, 255);
        public Vec4i _plainColor = new(0, 255, 0, 255);
        public Vec4i _lowLandColor = new(0, 150, 0, 255);
        public Vec4i _seaLevelWaterColor = new(0, 0, 255, 255);
        public Vec4i _highWaterColor = new(50, 255, 255, 255);
        public Vec4i _trenchColor = new(0, 0, 150, 255);

        public Height()
        {
        }
    }

    public struct Fertility
    {
        public int _veryLowFertilityColor = ColorUtil.ColorFromRgba(255, 0, 0, 255);
        public int _lowFertilityColor = ColorUtil.ColorFromRgba(255, 171, 0, 255);
        public int _mediumFertilityColor = ColorUtil.ColorFromRgba(255, 255, 0, 255);
        public int _highFertilityColor = ColorUtil.ColorFromRgba(0, 255, 0, 255);
        public int _extreameFertilityColor = ColorUtil.ColorFromRgba(0, 150, 0, 255);
    
        public int _waterColor = ColorUtil.ColorFromRgba(50, 50, 255, 255);
        public int _noFertilityColor = ColorUtil.ColorFromRgba(100, 100, 100, 255);

        public Fertility()
        {
        }
    }

    public struct Precipitation
    {
        public int _veryHighPrecipitation = ColorUtil.ColorFromRgba(70, 130, 230, 255);
        public int _highPrecipitation = ColorUtil.ColorFromRgba(40, 175, 175, 255);
        public int _mediumPrecipitation = ColorUtil.ColorFromRgba(110, 190, 110, 255);
        public int _lowPrecipitation = ColorUtil.ColorFromRgba(190, 190, 100, 255);
        public int _veryLowPrecipitation = ColorUtil.ColorFromRgba(240, 230, 160, 255);

        public Precipitation()
        {
        }
    }

    public struct Temperature
    {
        public int _arcticCold = ColorUtil.ColorFromRgba(0, 0, 100, 255);
        public int _extremeCold = ColorUtil.ColorFromRgba(0, 50, 180, 255);
        public int _veryCold = ColorUtil.ColorFromRgba(0, 100, 220, 255);
        public int _cold = ColorUtil.ColorFromRgba(100, 180, 255, 255);

        public int _cool = ColorUtil.ColorFromRgba(150, 220, 150, 255);
        public int _temperate = ColorUtil.ColorFromRgba(50, 200, 50, 255);
        public int _mild = ColorUtil.ColorFromRgba(200, 200, 80, 255);

        public int _warm = ColorUtil.ColorFromRgba(240, 180, 0, 255);
        public int _hot = ColorUtil.ColorFromRgba(220, 80, 0, 255);
        public int _extremeHot = ColorUtil.ColorFromRgba(150, 30, 30, 255);

        public Temperature()
        {
        }
    }

    public struct GeologyActivity
    {
        public int _lowActivity = ColorUtil.ColorFromRgba(100, 100, 100, 255);
        public int _moderateActivity = ColorUtil.ColorFromRgba(180, 140, 100, 255);
        public int _significantActivity = ColorUtil.ColorFromRgba(230, 190, 80, 255);
        public int _highActivity = ColorUtil.ColorFromRgba(220, 80, 0, 255);
        public int _extremeActivity = ColorUtil.ColorFromRgba(170, 30, 30, 255);

        public GeologyActivity()
        {
        }
    }

    public struct ChunkGrid
    {
        public int _borderColor = ColorUtil.ColorFromRgba(0, 0, 0, 255);
        public int _smallBorderColor = ColorUtil.ColorFromRgba(0, 0, 0, 150);
        public int _verySmallBorderColor = ColorUtil.ColorFromRgba(0, 0, 0, 100);
        public int _backgroundColor = ColorUtil.ColorFromRgba(0, 0, 0, 0);

        public ChunkGrid()
        {
        }
    }

    public struct TemporalStability
    {
        public int _colorMax = ColorUtil.ColorFromRgba(34, 139, 34, 255);
        public int _colorHigh = ColorUtil.ColorFromRgba(85, 170, 85, 255);
        public int _colorMid = ColorUtil.ColorFromRgba(255, 255, 0, 255);
        public int _colorLow = ColorUtil.ColorFromRgba(255, 165, 0, 255);
        public int _colorMin = ColorUtil.ColorFromRgba(220, 20, 60, 255);

        public TemporalStability()
        {
            
        }
    }
}