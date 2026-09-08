namespace AtlasPacker;

static class ObfuscationKey
{
    // Non-secret, non-cryptographic - see SheetObfuscator's remarks. Whatever value lives here at
    // pack time must match the key the app's future obfuscated-sheet delivery endpoint uses to
    // unmask on the way out, so this key is duplicated there rather than shared via a project
    // reference - AtlasPacker pulls in SkiaSharp, which the main app has no other reason to
    // depend on.
    public static readonly byte[] Bytes = "BlazorRogue-uf-tileset-v1"u8.ToArray();
}
