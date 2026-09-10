// Fetches the obfuscated tileset sheets (Program.cs's "/a/{file}" endpoint) and turns them back
// into usable images entirely client-side, exposed to the generated CSS
// (Rendering/SpriteAtlas.cs) as --atlas-N custom properties. See GamePage.razor's
// EnsureAtlasLoadedAsync for when this runs.
window.blazorRogueAtlas = {
    _key: new TextEncoder().encode("BlazorRogue-uf-tileset-v1"),

    // urls[i] becomes --atlas-i. Safe to call more than once (e.g. toggling ASCII -> tileset more
    // than once in a session) - already-loaded sheets are left alone.
    init: function (urls) {
        return Promise.all(
            urls.map((url, index) => this._loadSheetIfNeeded(url, index))
        ).catch((error) => {
            console.error("BlazorRogue: failed to load the tileset atlas", error);
        });
    },

    _loadSheetIfNeeded: async function (url, index) {
        const property = "--atlas-" + index;
        if (document.documentElement.style.getPropertyValue(property)) {
            return;
        }

        const response = await fetch(url);
        if (!response.ok) {
            throw new Error("atlas sheet fetch failed: " + url + " (" + response.status + ")");
        }

        const masked = new Uint8Array(await response.arrayBuffer());
        const unmasked = new Uint8Array(masked.length);
        const key = this._key;
        for (let i = 0; i < masked.length; i++) {
            unmasked[i] = masked[i] ^ key[i % key.length];
        }

        const blobUrl = URL.createObjectURL(new Blob([unmasked], { type: "image/png" }));
        // Intentionally never revoked - the generated CSS keeps referencing this Blob URL for the
        // life of the page.
        document.documentElement.style.setProperty(property, "url(" + JSON.stringify(blobUrl) + ")");
    },
};
