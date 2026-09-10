namespace AtlasPacker;

static class ObfuscationKey
{
    // Non-secret. Must stay byte-for-byte in sync with wwwroot/atlas.js (duplicated rather than
    // shared, since that's client-side JS and this project isn't referenced by the app).
    public static readonly byte[] Bytes = "BlazorRogue-uf-tileset-v1"u8.ToArray();
}
