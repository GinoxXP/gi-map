using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace GiMap.Client.Notes;

/// <summary>
/// Saves notes with the vanilla /waypoint commands, the same ones the vanilla waypoint dialogs send
/// </summary>
public static class MapNoteCommands
{
    private const string DefaultIcon = "circle";

    public static void Add(ICoreClientAPI capi, MapNote note)
    {
        var anchor = note.Anchor;
        var position = string.Create(CultureInfo.InvariantCulture, $"={anchor.X + 0.5} ={note.AnchorY} ={anchor.Y + 0.5}");
        capi.SendChatMessage($"/waypoint addati {DefaultIcon} {position} false {ColorUtil.Int2Hex(note.Color)} {MapNoteCodec.Format(note)}");
    }

    public static void Modify(ICoreClientAPI capi, MapNote note, int ownIndex)
    {
        var icon = string.IsNullOrEmpty(note.Waypoint?.Icon) ? DefaultIcon : note.Waypoint!.Icon;
        capi.SendChatMessage($"/waypoint modify {ownIndex} {ColorUtil.Int2Hex(note.Color)} {icon} false {MapNoteCodec.Format(note)}");
    }

    public static void Remove(ICoreClientAPI capi, int ownIndex)
        => capi.SendChatMessage($"/waypoint remove {ownIndex}");
}
