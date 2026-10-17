using GiMap.Config;
using GiMap.Patches;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace GiMap.Client.Storage;

/// <summary>
/// Map db files of a mode in the current world
/// </summary>
public record MapStorageEntry(string Id, List<string> Files, long Size)
{
    public bool IsTerrain => Id == MapStorage.TerrainId;

    public string Name => Lang.Get(IsTerrain ? "gimap-mapproperties-terrain" : "maplayer-" + Id);
}

/// <summary>
/// Finds and deletes map db files of the current world: <c>Maps/&lt;save&gt;-&lt;mode&gt;[...].db</c>,
/// the vanilla terrain map <c>&lt;save&gt;.db</c> and its LOD tiles <c>&lt;save&gt;-terrain-lod.db</c>
/// </summary>
public static class MapStorage
{
    public const string TerrainId = "terrain";
    private const string TerrainLodTail = "terrain-lod";
    private static readonly string[] SidecarSuffixes = ["-wal", "-shm", "-journal"];

    private static string MapsDir => Path.Combine(GamePaths.DataPath, "Maps");

    /// <summary>
    /// Entries with files, the terrain first, then modes in the order of the map tabs
    /// </summary>
    public static List<MapStorageEntry> Scan(ICoreClientAPI capi)
    {
        var save = capi.World.SavegameIdentifier;
        var ids = new List<string> { TerrainId };
        ids.AddRange(GiMapSettingsTab.ModeOrder);

        var filesById = new Dictionary<string, List<string>>();
        if (Directory.Exists(MapsDir))
        {
            foreach (var file in Directory.EnumerateFiles(MapsDir, save + "*"))
            {
                var id = GetId(Path.GetFileName(file), save, ids);
                if (id == null)
                    continue;

                if (!filesById.TryGetValue(id, out var files))
                    filesById[id] = files = new List<string>();
                files.Add(file);
            }
        }

        var entries = new List<MapStorageEntry>();
        foreach (var id in ids)
        {
            if (filesById.TryGetValue(id, out var files))
                entries.Add(new MapStorageEntry(id, files, files.Sum(GetFileSize)));
        }

        return entries;
    }

    /// <summary>
    /// Deletes the files of the entry; databases opened by a loaded layer are cleared by the layer. Main thread.
    /// </summary>
    /// <returns>False when some files could not be deleted</returns>
    public static bool Clear(ICoreClientAPI capi, MapStorageEntry entry)
    {
        var success = true;
        var openFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (entry.IsTerrain)
        {
            openFiles.UnionWith(ChunkMapLayerPatch.GetOpenDbFiles(capi).Select(Path.GetFullPath));
            if (openFiles.Count > 0)
                success &= ChunkMapLayerPatch.ClearStorage(capi);
        }
        else if (AMapLayer.FindByTitle(entry.Id) is { } layer)
        {
            foreach (var path in new[] { layer.MapDbFilePath, layer.LodDbFilePath })
            {
                if (path != null)
                    openFiles.Add(Path.GetFullPath(path));
            }

            layer.ClearStorage();
        }

        foreach (var file in entry.Files)
        {
            if (openFiles.Contains(Path.GetFullPath(StripSidecarSuffix(file))))
                continue;

            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                capi.Logger.Warning("[GiMap] Could not delete map file {0}: {1}", file, e.Message);
                success = false;
            }
        }

        return success;
    }

    private static string? GetId(string fileName, string save, List<string> ids)
    {
        var name = StripSidecarSuffix(fileName);
        if (!name.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
            return null;

        name = name[..^3];
        if (name == save)
            return TerrainId;

        if (!name.StartsWith(save + "-"))
            return null;

        var tail = name[(save.Length + 1)..];
        if (tail == TerrainLodTail)
            return TerrainId;

        // The longest id wins, so an id that is a prefix of another one does not take its files
        return ids
            .Where(id => id != TerrainId && (tail == id || tail.StartsWith(id + "-")))
            .OrderByDescending(id => id.Length)
            .FirstOrDefault();
    }

    private static string StripSidecarSuffix(string path)
    {
        foreach (var suffix in SidecarSuffixes)
        {
            if (path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return path[..^suffix.Length];
        }

        return path;
    }

    private static long GetFileSize(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    public static string FormatSize(long bytes)
    {
        string[] keys = ["gimap-size-b", "gimap-size-kb", "gimap-size-mb", "gimap-size-gb"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < keys.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return Lang.Get(keys[unit], unit == 0 ? size.ToString("0") : size.ToString("0.#"));
    }
}
