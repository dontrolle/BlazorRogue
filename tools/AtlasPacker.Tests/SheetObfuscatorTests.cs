namespace AtlasPacker.Tests;

public class SheetObfuscatorTests
{
    [Fact]
    public void MaskThenUnmaskRoundTripsToTheOriginalBytes()
    {
        byte[] original = [1, 2, 3, 4, 5, 255, 0, 128];
        byte[] key = [9, 8, 7];

        byte[] masked = SheetObfuscator.Mask(original, key);
        byte[] unmasked = SheetObfuscator.Unmask(masked, key);

        Assert.Equal(original, unmasked);
    }

    [Fact]
    public void MaskActuallyChangesTheBytes()
    {
        byte[] original = [1, 2, 3, 4];
        byte[] key = [255];

        byte[] masked = SheetObfuscator.Mask(original, key);

        Assert.NotEqual(original, masked);
    }

    [Fact]
    public void MaskRepeatsTheKeyAcrossLongerData()
    {
        byte[] zeros = new byte[6];
        byte[] key = [1, 2];

        // Masking an all-zero buffer surfaces the key material, repeated to length.
        Assert.Equal([1, 2, 1, 2, 1, 2], SheetObfuscator.Mask(zeros, key));
    }

    [Fact]
    public void MaskThrowsOnAnEmptyKey()
    {
        Assert.Throws<ArgumentException>(() => SheetObfuscator.Mask([1, 2, 3], []));
    }
}
