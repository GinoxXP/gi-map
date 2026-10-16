namespace GiMap.Client.Topographic;

/// <summary>
/// Height label of a chunk. Labels are appended to the chunk pixels after <see cref="ChunkPixelCount"/>,
/// so they are stored in the map db together with the image.
/// </summary>
public readonly record struct HeightLabel(int X, int Z, int Height, bool IsPeak)
{
    public const int ChunkPixelCount = 1024;

    public int Encode()
        => X | (Z << 5) | ((IsPeak ? 1 : 0) << 10) | (Height << 11);

    public static HeightLabel Decode(int value)
        => new(value & 31, (value >> 5) & 31, value >> 11, ((value >> 10) & 1) == 1);

    public static int[] Append(int[] pixels, List<HeightLabel> labels)
    {
        if (labels.Count == 0)
            return pixels;

        var result = new int[ChunkPixelCount + labels.Count];
        Array.Copy(pixels, result, ChunkPixelCount);
        for (var i = 0; i < labels.Count; i++)
            result[ChunkPixelCount + i] = labels[i].Encode();

        return result;
    }

    public static HeightLabel[] Extract(int[] pixels)
    {
        if (pixels.Length <= ChunkPixelCount)
            return Array.Empty<HeightLabel>();

        var labels = new HeightLabel[pixels.Length - ChunkPixelCount];
        for (var i = 0; i < labels.Length; i++)
            labels[i] = Decode(pixels[ChunkPixelCount + i]);

        return labels;
    }
}
