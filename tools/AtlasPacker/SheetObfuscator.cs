namespace AtlasPacker;

/// <summary>
/// Repeating-key XOR mask applied to obfuscated sheet bytes before they're written to disk / served
/// to the browser. This is deliberately <b>not</b> cryptographic protection - the Oryx license only
/// requires an attempt to protect the artwork (e.g. embedding it in a library file or archive), and
/// defeating a determined attacker with dev tools open is explicitly a non-goal. XOR is its own
/// inverse, so masking and unmasking are the same operation; the two names exist purely for
/// readability at call sites.
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
