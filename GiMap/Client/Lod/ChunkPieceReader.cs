using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace GiMap.Client.Lod;

public static class ChunkPieceReader
{
    /// <summary>
    /// Pixels of chunk pieces in the order of <paramref name="cords"/>, null for missing ones.
    /// Pieces not flushed to the db yet are taken from <paramref name="pending"/>. Map generation thread.
    /// </summary>
    public static int[]?[] Read(MapDB db, Dictionary<FastVec2i, MapPieceDB> pending, List<FastVec2i> cords)
    {
        var result = new int[]?[cords.Count];
        var fromDb = new List<FastVec2i>();
        var fromDbIndices = new List<int>();

        for (var i = 0; i < cords.Count; i++)
        {
            if (pending.TryGetValue(cords[i], out var piece))
            {
                result[i] = piece.Pixels;
                continue;
            }

            fromDb.Add(cords[i]);
            fromDbIndices.Add(i);
        }

        MapPieceDB?[]? pieces;
        try
        {
            pieces = db.GetMapPieces(fromDb);
        }
        catch (Exception e) when (e is ProtoBuf.ProtoException or OverflowException)
        {
            // One broken piece fails the whole batch, the rest are read one by one
            pieces = fromDb.Select(cord => TryGetMapPiece(db, cord)).ToArray();
        }

        if (pieces == null)
            return result;

        for (var i = 0; i < pieces.Length; i++)
            result[fromDbIndices[i]] = pieces[i]?.Pixels;

        return result;
    }

    private static MapPieceDB? TryGetMapPiece(MapDB db, FastVec2i cord)
    {
        try
        {
            return db.GetMapPiece(cord);
        }
        catch (Exception e) when (e is ProtoBuf.ProtoException or OverflowException)
        {
            return null;
        }
    }
}
