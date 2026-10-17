using HarmonyLib;
using Microsoft.Data.Sqlite;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace GiMap.Client.Storage;

public static class MapDbUtil
{
    private static readonly AccessTools.FieldRef<SQLiteDBConnection, SqliteConnection> ConnectionRef =
        AccessTools.FieldRefAccess<SQLiteDBConnection, SqliteConnection>("sqliteConn");

    /// <summary>
    /// Deletes all map pieces and shrinks the file, a plain purge keeps its size. Call on the map generation thread.
    /// </summary>
    public static void Clear(MapDB db, ILogger logger)
    {
        try
        {
            db.Purge();
            db.Vacuum();

            // Databases are opened in WAL mode, the vacuumed pages stay in the log until a checkpoint
            using var command = ConnectionRef(db).CreateCommand();
            command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE);";
            command.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            logger.Error("[GiMap] Could not clear a map db");
            logger.Error(e);
        }
    }
}
