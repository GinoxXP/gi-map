using GiMap.Config;
using Vintagestory.GameContent;

namespace GiMap.Client.Lod;

/// <summary>
/// Picks the LOD level from the zoom of the opened world map. Level L means 2^L blocks per texel, 0 means LOD is off.
/// </summary>
public static class LodState
{
    public const int MinLevel = 2;
    private const int AbsoluteMaxLevel = 7;
    // Below this zoom per-chunk textures are replaced by LOD tiles
    private const float MaxLodZoom = 0.5f;

    // Set when the map element is created, layers read the zoom from it because view change events do not carry it
    public static GuiElementMap? Map { get; set; }

    public static int Level => Map == null ? 0 : LevelForZoom(Map.ZoomLevel);

    public static int MaxLevel
    {
        get
        {
            var minZoom = ConfigManager.ConfigInstance?.minZoom ?? 0.25f;
            return Math.Clamp((int)Math.Ceiling(Math.Log2(1 / Math.Max(minZoom, 0.001f))), MinLevel, AbsoluteMaxLevel);
        }
    }

    public static int LevelForZoom(float zoom)
    {
        if (zoom >= MaxLodZoom)
            return 0;

        return Math.Clamp((int)Math.Round(Math.Log2(1 / zoom)), MinLevel, MaxLevel);
    }
}
