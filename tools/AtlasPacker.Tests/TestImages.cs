using SkiaSharp;

namespace AtlasPacker.Tests;

/// <summary>Small helpers to build synthetic bitmaps for tests, so the matching/obfuscation logic
/// can be exercised without the licensed Oryx assets.</summary>
static class TestImages
{
    public static SKBitmap Solid(int width, int height, SKColor color)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(x, y, color);
            }
        }

        return bitmap;
    }

    /// <summary>A sheet where every pixel's color is derived from its (x, y), so any two
    /// same-sized crops from different positions are near-certain to differ - useful for
    /// asserting a matcher finds the *one* true position.</summary>
    public static SKBitmap Checkerboard(int width, int height)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bitmap.SetPixel(
                    x,
                    y,
                    new SKColor((byte)(x * 7), (byte)(y * 11), (byte)((x + y) * 3), 255)
                );
            }
        }

        return bitmap;
    }

    public static NormalizedImage Crop(SKBitmap source, int x, int y, int width, int height)
    {
        using var dest = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        if (!source.ExtractSubset(dest, new SKRectI(x, y, x + width, y + height)))
        {
            throw new InvalidOperationException(
                "ExtractSubset failed - crop rectangle out of bounds?"
            );
        }

        return NormalizedImage.FromBitmap(dest);
    }
}
