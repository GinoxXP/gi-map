namespace GiMap.Config;

public class GiMapConfig
{
    public float overlayAlpha = 1;
    public Dictionary<string, string> rockCodeColors = new Dictionary<string, string>();
    public HashSet<string> ignoredRocks = new HashSet<string>()
    {
        "game:rock-suevite",
    };
}