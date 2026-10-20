namespace GiMap;

/// <summary>
/// Map modes shown in the settings switches, the map tab order and "Map properties".
/// Add-on mods (e.g. GiMap: Underground) register their modes here in <c>StartPre</c>, after the core mod.
/// </summary>
public static class GiMapModes
{
    public record Mode(string MapType, int Order, Func<bool> IsEnabled, Action<bool> SetEnabled, Action Save);

    private static readonly List<Mode> Modes = new();
    private static readonly List<Action> ResetHandlers = new();

    /// <summary>
    /// Modes in the order of the map tabs
    /// </summary>
    public static IReadOnlyList<Mode> All => Modes;

    /// <param name="order">Position of the map tab after "terrain", lower is first</param>
    /// <param name="save">Saves the config holding the enabled flag</param>
    public static void Register(string mapType, int order, Func<bool> isEnabled, Action<bool> setEnabled, Action save)
    {
        Modes.RemoveAll(mode => mode.MapType == mapType);
        Modes.Add(new Mode(mapType, order, isEnabled, setEnabled, save));
        Modes.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    /// <summary>
    /// Called by "Reset settings" in the settings tab, an add-on resets its own config here
    /// </summary>
    public static void OnReset(Action handler) => ResetHandlers.Add(handler);

    internal static void Reset()
    {
        foreach (var handler in ResetHandlers)
            handler();
    }

    internal static void Clear()
    {
        Modes.Clear();
        ResetHandlers.Clear();
    }
}
