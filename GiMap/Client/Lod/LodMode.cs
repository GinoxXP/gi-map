namespace GiMap.Client.Lod;

/// <summary>
/// How a map layer is drawn when the map is zoomed out to a LOD level
/// </summary>
public enum LodMode
{
    // Chunks are merged into LOD tiles
    Tiles,
    // Nothing is loaded or drawn
    Hidden,
    // Per-chunk textures as on close zoom
    PerChunk,
}
