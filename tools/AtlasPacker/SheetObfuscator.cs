namespace AtlasPacker;

/// <summary>
/// Lightweight obfuscation applied to sheet bytes before they're written to disk / served to the
/// browser. <c>wwwroot/atlas.js</c> reverses it client-side.
/// </summary>
static class SheetObfuscator
{
    public static byte[] Mask(ReadOnlySpan<byte> data, ReadOnlySpan<byte> key)
    {
        if (key.IsEmpty)
        {
            throw new ArgumentException("Obfuscation key must not be empty.", nameof(key));
        }

        byte[] result = new byte[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }

        return result;
    }

    public static byte[] Unmask(ReadOnlySpan<byte> maskedData, ReadOnlySpan<byte> key) =>
        Mask(maskedData, key);
}
