// Locator/packer for the licensed Ultimate Fantasy tileset.
//
// Run only on a machine that owns the Oryx license and has both:
//   - the vendor's full pre-packed sheets (the root of the "oryx_ultimate_fantasy" download)
//   - this repo's currently-exported wwwroot/img/uf_* crops (used only to identify, by exact
//     pixel match, which rectangle of a vendor sheet each sprite name occupies)
//
// and writes atlas.json + obfuscated sheet bundles to an output directory, for the app to load at
// runtime via BLAZORROGUE_ART_PATH (added in a follow-up PR). Nothing here is checked into source
// control or run in CI beyond this project's own unit tests (which use synthetic bitmaps, not the
// licensed assets).
using System.Text.Json;
using AtlasPacker;

string? oryxDir = GetArg(args, "--oryx");
string? exportDir = GetArg(args, "--export");
string? outDir = GetArg(args, "--out");

if (oryxDir is null || exportDir is null || outDir is null)
{
    Console.Error.WriteLine(
        "Usage: AtlasPacker --oryx <oryx_ultimate_fantasy dir> --export <wwwroot/img dir> --out <output dir>"
    );
    return 1;
}

Directory.CreateDirectory(outDir);

var sprites = new Dictionary<string, SpriteEntry>(StringComparer.Ordinal);
var sheetBitmaps = new Dictionary<string, NormalizedImage>(StringComparer.Ordinal);
int misses = 0;

NormalizedImage LoadSheet(string sheetName)
{
    if (!sheetBitmaps.TryGetValue(sheetName, out var image))
    {
        image = NormalizedImage.FromFile(Path.Combine(oryxDir, sheetName + ".png"));
        sheetBitmaps[sheetName] = image;
    }
    return image;
}

void MatchFolderAgainstSheet(
    string exportSubfolder,
    string sheetName,
    IEnumerable<string>? skip = null
)
{
    var skipSet = skip is null ? null : new HashSet<string>(skip, StringComparer.OrdinalIgnoreCase);
    string folder = Path.Combine(exportDir, exportSubfolder);
    if (!Directory.Exists(folder))
    {
        Console.WriteLine($"  (skipping {exportSubfolder} - not present locally)");
        return;
    }

    var sheet = LoadSheet(sheetName);
    foreach (
        string file in Directory
            .EnumerateFiles(folder, "*.png")
            .OrderBy(f => f, StringComparer.Ordinal)
    )
    {
        string key = Path.GetFileNameWithoutExtension(file);
        if (skipSet is not null && skipSet.Contains(key))
        {
            continue;
        }

        var sprite = NormalizedImage.FromFile(file);
        var match = SpriteMatcher.FindExact(sheet, sprite);
        if (match is null)
        {
            Console.WriteLine($"  MISS  {exportSubfolder}/{key}");
            misses++;
            continue;
        }

        sprites[key] = new SpriteEntry(
            sheetName,
            match.Value.X,
            match.Value.Y,
            sprite.Width,
            sprite.Height
        );
    }
}

Console.WriteLine("Matching terrain...");
MatchFolderAgainstSheet("uf_terrain", "uf_terrain");

Console.WriteLine("Matching items...");
MatchFolderAgainstSheet("uf_items", "uf_items");

Console.WriteLine("Matching FX impact...");
MatchFolderAgainstSheet("uf_FX_impact", "uf_FX_impact");

Console.WriteLine("Matching UI icons...");
{
    // "*_gray" variants are the project's own edits, not vendor art, and are being dropped.
    // "skill_defense" has no vendor equivalent anywhere in the tileset (checked exhaustively,
    // including flips) and is hand-mapped below instead.
    string[] uiSkip =
    [
        "skill_defense",
        "skill_defense_gray",
        "skills_attack_gray",
        "skills_damage_gray",
    ];
    string[] candidateSheets = ["uf_skills", "uf_interface", "uf_items"];
    string uiFolder = Path.Combine(exportDir, "ui");

    if (Directory.Exists(uiFolder))
    {
        foreach (
            string file in Directory
                .EnumerateFiles(uiFolder, "*.png")
                .OrderBy(f => f, StringComparer.Ordinal)
        )
        {
            string key = Path.GetFileNameWithoutExtension(file);
            if (uiSkip.Contains(key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            var sprite = NormalizedImage.FromFile(file);
            bool found = false;
            foreach (string sheetName in candidateSheets)
            {
                var match = SpriteMatcher.FindExact(LoadSheet(sheetName), sprite);
                if (match is not null)
                {
                    sprites[key] = new SpriteEntry(
                        sheetName,
                        match.Value.X,
                        match.Value.Y,
                        sprite.Width,
                        sprite.Height
                    );
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Console.WriteLine($"  MISS  ui/{key}");
                misses++;
            }
        }
    }
    else
    {
        Console.WriteLine("  (skipping ui - not present locally)");
    }
}

// Manually located: a shield-with-cross icon in uf_skills.png with no byte-identical counterpart
// among the project's own custom-edited icons.
sprites["skill_defense"] = new SpriteEntry("uf_skills", 64, 160, 32, 32);
LoadSheet("uf_skills"); // ensure the sheet is included even if no other UI icon matched it

Console.WriteLine("Matching heroes/monsters (via uf_heroes_simple.png identity lookup)...");
{
    string heroesFolder = Path.Combine(exportDir, "uf_heroes");
    if (Directory.Exists(heroesFolder))
    {
        var simpleSheet = NormalizedImage.FromFile(Path.Combine(oryxDir, "uf_heroes_simple.png"));
        var bases = Directory
            .EnumerateFiles(heroesFolder, "*_1.png")
            .Select(f => Path.GetFileName(f)[..^"_1.png".Length])
            .OrderBy(n => n, StringComparer.Ordinal);

        foreach (string baseName in bases)
        {
            string frame1Path = Path.Combine(heroesFolder, baseName + "_1.png");
            var sprite = NormalizedImage.FromFile(frame1Path);
            var match = SpriteMatcher.FindExact(simpleSheet, sprite);
            if (match is null)
            {
                Console.WriteLine(
                    $"  MISS  uf_heroes/{baseName} (no identity match in uf_heroes_simple.png)"
                );
                misses++;
                continue;
            }

            var (col, row) = HeroBlock.IdentityFromSimpleSheetPosition(
                match.Value.X,
                match.Value.Y
            );
            var (x, y) = HeroBlock.FrameOrigin(col, row, frame: 0);
            sprites[baseName] = new SpriteEntry(
                "uf_heroes",
                x,
                y,
                sprite.Width,
                sprite.Height,
                FrameCount: HeroBlock.FrameCount,
                FrameStrideX: HeroBlock.CellSize
            );
        }
    }
    else
    {
        Console.WriteLine("  (skipping uf_heroes - not present locally)");
    }
}

// uf_heroes.png is the shipped source for hero/monster frames (uf_heroes_simple.png was only an
// offline lookup key and must not be shipped).
if (!sheetBitmaps.ContainsKey("uf_heroes") && sprites.Values.Any(s => s.Sheet == "uf_heroes"))
{
    _ = LoadSheet("uf_heroes");
}

Console.WriteLine();
Console.WriteLine($"Resolved {sprites.Count} sprites, {misses} misses.");

var sheets = new Dictionary<string, SheetInfo>(StringComparer.Ordinal);
int sheetId = 0;
foreach (var (sheetName, sheetImage) in sheetBitmaps)
{
    string fileName = $"{sheetId}.bin";
    sheetId++;

    byte[] pngBytes = File.ReadAllBytes(Path.Combine(oryxDir, sheetName + ".png"));
    byte[] masked = SheetObfuscator.Mask(pngBytes, ObfuscationKey.Bytes);
    File.WriteAllBytes(Path.Combine(outDir, fileName), masked);

    sheets[sheetName] = new SheetInfo(sheetImage.Width, sheetImage.Height, fileName);
}

var document = new AtlasDocument(sheets, sprites);
string json = JsonSerializer.Serialize(
    document,
    new JsonSerializerOptions { WriteIndented = true }
);
File.WriteAllText(Path.Combine(outDir, "atlas.json"), json);

Console.WriteLine($"Wrote atlas.json + {sheets.Count} obfuscated sheet(s) to {outDir}");

return misses == 0 ? 0 : 1;

static string? GetArg(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
