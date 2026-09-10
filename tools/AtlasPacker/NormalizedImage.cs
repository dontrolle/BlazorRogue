using SkiaSharp;

namespace AtlasPacker;

/// <summary>
/// An RGBA8888 pixel buffer with fully-transparent pixels normalized to <c>(0,0,0,0)</c>. PNG
/// exporters disagree on what RGB to leave behind alpha=0 pixels, which otherwise breaks
/// byte-identity comparison between two crops of the same underlying artwork.
/// </summary>
sealed class NormalizedImage
{
    public int Width { get; }
    public int Height { get; }

    /// <summary>Row-major, 4 bytes (RGBA) per pixel, alpha=0 pixels zeroed in RGB too.</summary>
    public byte[] Pixels { get; }

    NormalizedImage(int width, int height, byte[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public static NormalizedImage FromFile(string path)
    {
        using var bitmap =
            SKBitmap.Decode(path)
            ?? throw new InvalidDataException($"Could not decode image: {path}");
        return FromBitmap(bitmap);
    }

    public static NormalizedImage FromBitmap(SKBitmap bitmap)
    {
        using var rgba =
            bitmap.ColorType == SKColorType.Rgba8888 ? null : bitmap.Copy(SKColorType.Rgba8888);
        var source = rgba ?? bitmap;

        int width = source.Width;
        int height = source.Height;
        int rowBytes = width * 4;
        byte[] pixels = new byte[height * rowBytes];

        // Copy row-by-row rather than one flat Bytes.CopyTo(): a bitmap produced via
        // ExtractSubset() keeps its parent's RowBytes (stride), which is wider than width * 4 -
        // copying it as one contiguous block either throws (destination too short) or, worse,
        // silently shifts every row after the first by the leftover padding.
        ReadOnlySpan<byte> sourceBytes = source.Bytes;
        int sourceStride = source.RowBytes;
        for (int y = 0; y < height; y++)
        {
            sourceBytes
                .Slice(y * sourceStride, rowBytes)
                .CopyTo(pixels.AsSpan(y * rowBytes, rowBytes));
        }

        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] == 0)
            {
                pixels[i] = 0;
                pixels[i + 1] = 0;
                pixels[i + 2] = 0;
            }
        }

        return new NormalizedImage(width, height, pixels);
    }

    public ReadOnlySpan<byte> Row(int y) => Pixels.AsSpan(y * Width * 4, Width * 4);
}
