using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace BlazorRogue.Rendering;

/// <summary>Dimensions and served location of one sheet file - mirrors the shape
/// <c>tools/AtlasPacker</c> writes to <c>atlas.json</c>.</summary>
sealed record SpriteSheetInfo(int Width, int Height, string File);

/// <summary>One sprite's rectangle - a static sprite has <see cref="FrameCount"/> 1; an animated one
/// (heroes/monsters today) has consecutive frames offset by (<see cref="FrameStrideX"/>,
/// <see cref="FrameStrideY"/>) from (<see cref="X"/>, <see cref="Y"/>). Mirrors the shape
/// <c>tools/AtlasPacker</c> writes to <c>atlas.json</c>.</summary>
sealed record SpriteAtlasEntry(
    string Sheet,
    int X,
    int Y,
    int W,
    int H,
    int FrameCount = 1,
    int FrameStrideX = 0,
    int FrameStrideY = 0
);

sealed record AtlasJsonDocument(
    Dictionary<string, SpriteSheetInfo> Sheets,
    Dictionary<string, SpriteAtlasEntry> Sprites
);

/// <summary>
/// Loads the atlas manifest produced offline by <c>tools/AtlasPacker</c> (never checked into
/// source control) and generates the CSS that positions each sprite within its sheet.
///
/// The sheets themselves are served, still obfuscated, from an endpoint the browser fetches
/// directly; <see cref="GenerateStaticCss"/> only ever references them via <c>var(--atlas-N)</c>
/// custom properties that JS sets (see <c>wwwroot/atlas.js</c>) - this class never touches the
/// pixel bytes.
///
/// When no atlas is found, <see cref="IsAvailable"/> is false and the game falls back to the ASCII
/// renderer, exactly as it did when the loose tileset files were absent.
/// </summary>
sealed class SpriteAtlas
{
    readonly Dictionary<string, SpriteSheetInfo> sheets;
    readonly Dictionary<string, SpriteAtlasEntry> sprites;
    readonly Dictionary<string, int> sheetIndexes;

    public bool IsAvailable { get; }

    /// <summary>Sheet URLs in the order their <c>--atlas-N</c> CSS variable index expects -
    /// <c>SheetEndpoints[i]</c> must be fetched and set as <c>--atlas-{i}</c> (see
    /// <c>wwwroot/atlas.js</c>).</summary>
    public IReadOnlyList<string> SheetEndpoints { get; }

    SpriteAtlas(AtlasJsonDocument? document, string sheetUrlPrefix)
    {
        IsAvailable = document is not null;
        sheets = document?.Sheets ?? [];
        sprites = document?.Sprites ?? [];

        // Stable order (alphabetical by sheet name) so the same build always assigns the same
        // --atlas-N index to the same sheet.
        var orderedSheetNames = sheets
            .Keys.OrderBy(name => name, System.StringComparer.Ordinal)
            .ToList();
        sheetIndexes = orderedSheetNames
            .Select((name, index) => (name, index))
            .ToDictionary(pair => pair.name, pair => pair.index);
        SheetEndpoints = [.. orderedSheetNames.Select(name => sheetUrlPrefix + sheets[name].File)];
    }

    /// <summary>
    /// Loads <c>atlas.json</c> from <paramref name="artPath"/>. Returns an unavailable instance
    /// (no exception) if <paramref name="artPath"/> is null or the file isn't there - the same
    /// "art not installed" outcome the loose-file probe used to produce.
    /// </summary>
    public static SpriteAtlas Load(string? artPath, string sheetUrlPrefix = "/a/")
    {
        if (artPath is null)
        {
            return new SpriteAtlas(null, sheetUrlPrefix);
        }

        string manifestPath = Path.Combine(artPath, "atlas.json");
        if (!File.Exists(manifestPath))
        {
            return new SpriteAtlas(null, sheetUrlPrefix);
        }

        var document = JsonSerializer.Deserialize<AtlasJsonDocument>(
            File.ReadAllText(manifestPath)
        );
        return new SpriteAtlas(document, sheetUrlPrefix);
    }

    /// <summary>Test-only construction path: builds a SpriteAtlas directly from an in-memory
    /// document, bypassing the atlas.json file (BlazorRogue.Tests has InternalsVisibleTo).</summary>
    internal static SpriteAtlas FromDocument(
        AtlasJsonDocument document,
        string sheetUrlPrefix = "/a/"
    ) => new(document, sheetUrlPrefix);

    /// <summary>True if the given served file name (e.g. <c>"0.bin"</c>) is one of this atlas's
    /// sheets - used by the delivery endpoint to refuse serving anything else out of the art
    /// directory.</summary>
    public bool IsKnownSheetFile(string fileName) =>
        sheets.Values.Any(sheet => sheet.File == fileName);

    /// <summary>
    /// The <c>background-image</c>/<c>background-size</c>/<c>background-position</c> declarations
    /// that display <paramref name="spriteKey"/>'s given <paramref name="frame"/> (0 for a static
    /// sprite), scaled to fill whatever box the class is applied to. <see langword="null"/> if the
    /// key or frame doesn't exist - callers should treat that as "nothing to render", same as a
    /// missing image file today.
    /// </summary>
    public string? CssDeclarationsFor(string spriteKey, int frame = 0)
    {
        if (
            !sprites.TryGetValue(spriteKey, out var sprite)
            || frame < 0
            || frame >= sprite.FrameCount
        )
        {
            return null;
        }

        var sheet = sheets[sprite.Sheet];
        int frameX = sprite.X + frame * sprite.FrameStrideX;
        int frameY = sprite.Y + frame * sprite.FrameStrideY;

        // background-size is a percentage of the element's own box, so scaling the *whole* sheet
        // image by (sheet dimension / sprite dimension) makes the sprite's own region exactly fill
        // that box - independent of the box's actual pixel size (so it survives the game's 24-64px
        // tile zoom with no per-zoom recomputation). background-position then shifts that scaled
        // sheet so the sprite's top-left corner lands at the box's top-left corner; the percentage
        // form divides by (sheet - sprite) rather than sheet, per the CSS spec's definition of
        // percentage background-position (guarded below for a sprite exactly as wide/tall as its
        // sheet, which would otherwise divide by zero).
        double sizeXPercent = (double)sheet.Width / sprite.W * 100.0;
        double sizeYPercent = (double)sheet.Height / sprite.H * 100.0;
        double posXPercent =
            sheet.Width == sprite.W ? 0.0 : (double)frameX / (sheet.Width - sprite.W) * 100.0;
        double posYPercent =
            sheet.Height == sprite.H ? 0.0 : (double)frameY / (sheet.Height - sprite.H) * 100.0;

        int sheetIndex = sheetIndexes[sprite.Sheet];

        return string.Create(
            CultureInfo.InvariantCulture,
            $"background-image:var(--atlas-{sheetIndex});background-size:{Pct(sizeXPercent)}% {Pct(sizeYPercent)}%;background-position:{Pct(posXPercent)}% {Pct(posYPercent)}%;"
        );
    }

    static string Pct(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>
    /// One <c>.spr-&lt;key&gt;</c> CSS class per sprite in the atlas, each sized/positioned for
    /// frame 0. Used directly by static (non-animated) sprites - tiles, decorations, items, UI
    /// icons; animated sprites (heroes/monsters, liquids) instead go through
    /// <see cref="AnimationCssGenerator"/>, which calls <see cref="CssDeclarationsFor"/> per frame.
    /// </summary>
    public string GenerateStaticCss()
    {
        if (!IsAvailable)
        {
            return "";
        }

        StringBuilder css = new();
        foreach (string key in sprites.Keys.OrderBy(k => k, System.StringComparer.Ordinal))
        {
            _ = css.Append(
                CultureInfo.InvariantCulture,
                $".spr-{key} {{ {CssDeclarationsFor(key)} }}\n"
            );
        }

        return css.ToString();
    }
}
