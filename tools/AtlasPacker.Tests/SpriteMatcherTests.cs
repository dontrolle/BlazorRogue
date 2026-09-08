using SkiaSharp;

namespace AtlasPacker.Tests;

public class SpriteMatcherTests
{
    [Fact]
    public void FindExactLocatesSpriteAtItsTruePosition()
    {
        using var sheet = TestImages.Checkerboard(64, 64);
        var sheetImage = NormalizedImage.FromBitmap(sheet);
        var sprite = TestImages.Crop(sheet, x: 24, y: 8, width: 8, height: 6);

        var result = SpriteMatcher.FindExact(sheetImage, sprite);

        Assert.Equal((24, 8), result);
    }

    [Fact]
    public void FindExactLocatesSpriteFlushAgainstTheSheetsBottomRightCorner()
    {
        using var sheet = TestImages.Checkerboard(32, 32);
        var sheetImage = NormalizedImage.FromBitmap(sheet);
        var sprite = TestImages.Crop(sheet, x: 24, y: 28, width: 8, height: 4);

        Assert.Equal((24, 28), SpriteMatcher.FindExact(sheetImage, sprite));
    }

    [Fact]
    public void FindExactReturnsNullWhenSpriteIsNotPresentAnywhere()
    {
        using var sheet = TestImages.Checkerboard(32, 32);
        var sheetImage = NormalizedImage.FromBitmap(sheet);
        var foreignSprite = TestImages.Solid(4, 4, new SKColor(1, 2, 3, 255));

        Assert.Null(SpriteMatcher.FindExact(sheetImage, NormalizedImage.FromBitmap(foreignSprite)));
    }

    [Fact]
    public void FindExactReturnsNullWhenSpriteIsLargerThanTheSheet()
    {
        using var sheet = TestImages.Solid(4, 4, SKColors.Red);
        using var sprite = TestImages.Solid(8, 8, SKColors.Red);

        var sheetImage = NormalizedImage.FromBitmap(sheet);
        var spriteImage = NormalizedImage.FromBitmap(sprite);

        Assert.Null(SpriteMatcher.FindExact(sheetImage, spriteImage));
    }

    [Fact]
    public void FindExactSkipsAFalseMatchStraddlingTwoPixelsAndFindsTheRealAlignedOne()
    {
        // Sheet pixel bytes (RGBA; none have alpha=0, so normalization leaves them untouched):
        //   px0=[1,10,20,30]  px1=[255,2,3,4]  px2=[5,6,7,8]  px3=[10,20,30,255]
        // Bytes 1..4 ([10,20,30,255]) coincidentally equal the 1x1 sprite's pixel bytes, but that
        // occurrence starts at byte offset 1 - not a pixel boundary - and must be skipped in favor
        // of the real match at px3 (byte offset 12, x=3).
        using var sheet = new SKBitmap(4, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        sheet.SetPixel(0, 0, new SKColor(1, 10, 20, 30));
        sheet.SetPixel(1, 0, new SKColor(255, 2, 3, 4));
        sheet.SetPixel(2, 0, new SKColor(5, 6, 7, 8));
        sheet.SetPixel(3, 0, new SKColor(10, 20, 30, 255)); // the real match

        using var sprite = new SKBitmap(1, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        sprite.SetPixel(0, 0, new SKColor(10, 20, 30, 255));

        var sheetImage = NormalizedImage.FromBitmap(sheet);
        var spriteImage = NormalizedImage.FromBitmap(sprite);

        Assert.Equal((3, 0), SpriteMatcher.FindExact(sheetImage, spriteImage));
    }

    [Fact]
    public void FindExactTreatsFullyTransparentPixelsAsEqualRegardlessOfLeftoverRgb()
    {
        // Same visible content, but the fully-transparent neighbor pixel has different "garbage"
        // RGB in each image - exactly the exporter quirk NormalizedImage exists to paper over.
        using var sheet = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        sheet.SetPixel(0, 0, new SKColor(10, 20, 30, 255));
        sheet.SetPixel(1, 0, new SKColor(255, 255, 255, 0));

        using var sprite = new SKBitmap(2, 1, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        sprite.SetPixel(0, 0, new SKColor(10, 20, 30, 255));
        sprite.SetPixel(1, 0, new SKColor(1, 1, 1, 0));

        var sheetImage = NormalizedImage.FromBitmap(sheet);
        var spriteImage = NormalizedImage.FromBitmap(sprite);

        Assert.Equal((0, 0), SpriteMatcher.FindExact(sheetImage, spriteImage));
    }
}
