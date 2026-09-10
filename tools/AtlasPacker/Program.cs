// Locator/packer for the licensed tileset. Run only on a machine that has the vendor download.
// Writes atlas.json + obfuscated sheet bundles to an output directory for the app to load at
// runtime via BLAZORROGUE_ART_PATH. Not checked into source control, not run in CI (the unit
// tests use synthetic bitmaps, not licensed assets). See README.md.
using System.Text.Json;
using AtlasPacker;

string? oryxDir = GetArg(args, "--oryx");
string? outDir = GetArg(args, "--out");

if (oryxDir is null || outDir is null)
{
    Console.Error.WriteLine(
        "Usage: AtlasPacker --oryx <oryx_ultimate_fantasy dir> --out <output dir>"
    );
    return 1;
}

string splitDir = Path.Combine(oryxDir, "uf_split");
Directory.CreateDirectory(outDir);

var sprites = new Dictionary<string, SpriteEntry>(StringComparer.Ordinal);
var sheetBitmaps = new Dictionary<string, NormalizedImage>(StringComparer.Ordinal);
var sheetSourcePaths = new Dictionary<string, string>(StringComparer.Ordinal);
int misses = 0;

// sourcePath defaults to <sheetName>.png in the vendor dir; pass one explicitly to build a
// "sheet" from some other single PNG (see the residuals handled below).
NormalizedImage LoadSheet(string sheetName, string? sourcePath = null)
{
    if (!sheetBitmaps.TryGetValue(sheetName, out var image))
    {
        string path = sourcePath ?? Path.Combine(oryxDir, sheetName + ".png");
        image = NormalizedImage.FromFile(path);
        sheetBitmaps[sheetName] = image;
        sheetSourcePaths[sheetName] = path;
    }
    return image;
}

void MatchFolderAgainstSheet(
    string splitSubfolder,
    string sheetName,
    IReadOnlySet<string>? skip = null
)
{
    string folder = Path.Combine(splitDir, splitSubfolder);
    if (!Directory.Exists(folder))
    {
        Console.WriteLine($"  (skipping {splitSubfolder} - not present under {splitDir})");
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
        if (skip is not null && skip.Contains(key))
        {
            continue;
        }

        var sprite = NormalizedImage.FromFile(file);
        var match = SpriteMatcher.FindExact(sheet, sprite);
        if (match is null)
        {
            Console.WriteLine($"  MISS  {splitSubfolder}/{key}");
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

// Hardcoded and skipped in the pass below: an exact-match discrepancy in the source export means
// this one won't auto-match, but it's real art worth carrying rather than logging as a miss.
sprites["floor_tile_sandy_1"] = new SpriteEntry("uf_terrain", 48, 480, 48, 48);
MatchFolderAgainstSheet(
    "uf_terrain",
    "uf_terrain",
    skip: new HashSet<string> { "floor_tile_sandy_1" }
);

Console.WriteLine("Matching items...");

// These cloak recolors have no match on any sheet, so each becomes a one-sprite "sheet" built
// straight from its own source PNG (same machinery, different source) and is skipped below.
string[] residualCloaks =
[
    "cloak_blood",
    "cloak_eye",
    "cloak_fur",
    "cloak_glow",
    "cloak_gold",
    "cloak_leather",
    "cloak_ornate",
    "cloak_royal",
    "cloak_tattered",
];
foreach (string name in residualCloaks)
{
    var image = LoadSheet(name, Path.Combine(splitDir, "uf_items", name + ".png"));
    sprites[name] = new SpriteEntry(name, 0, 0, image.Width, image.Height);
}

MatchFolderAgainstSheet("uf_items", "uf_items", skip: new HashSet<string>(residualCloaks));

Console.WriteLine("Matching FX impact...");
MatchFolderAgainstSheet("uf_FX_impact", "uf_FX_impact");

// HUD icons that aren't exported as named files - coordinates resolved once by hand and fixed.
Console.WriteLine("Adding hand-mapped UI icons...");

sprites["blood_meter_empty"] = new SpriteEntry("uf_interface", 56, 8, 48, 16);
sprites["blood_meter_empty_right_end"] = new SpriteEntry("uf_interface", 100, 8, 4, 16);
sprites["blood_meter_full"] = new SpriteEntry("uf_interface", 216, 24, 48, 16);
sprites["blood_meter_strip_empty"] = new SpriteEntry("uf_interface", 60, 8, 1, 16);
sprites["blood_meter_strip_full_right_end"] = new SpriteEntry("uf_interface", 231, 40, 1, 16);
sprites["gold_empty"] = new SpriteEntry("uf_items", 432, 48, 48, 48);
sprites["gold_full"] = new SpriteEntry("uf_items", 385, 48, 48, 48);
sprites["skills_attack"] = new SpriteEntry("uf_skills", 224, 0, 32, 32);
sprites["skills_damage"] = new SpriteEntry("uf_skills", 320, 96, 32, 32);
sprites["skill_defense"] = new SpriteEntry("uf_skills", 256, 160, 32, 32);
LoadSheet("uf_interface");
LoadSheet("uf_items");
LoadSheet("uf_skills");

Console.WriteLine("Matching heroes/monsters via identity lookup...");

// Not resolvable by the identity lookup below; grid cell fixed by hand instead (see HeroBlock).
Dictionary<string, (int Col, int Row)> heroIdentityOverrides = new(StringComparer.Ordinal)
{
    ["bird_dove"] = (Col: 0, Row: 9),
    ["wolf_black"] = (Col: 0, Row: 6),
    ["merchant_b"] = (Col: 9, Row: 12),
};
foreach (var (baseName, (col, row)) in heroIdentityOverrides)
{
    var (x, y) = HeroBlock.FrameOrigin(col, row, frame: 0);
    sprites[baseName] = new SpriteEntry(
        "uf_heroes",
        x,
        y,
        HeroBlock.CellSize,
        HeroBlock.CellSize,
        FrameCount: HeroBlock.FrameCount,
        FrameStrideX: HeroBlock.CellSize
    );
}

{
    string heroesFolder = Path.Combine(splitDir, "uf_heroes");
    if (Directory.Exists(heroesFolder))
    {
        var simpleSheet = NormalizedImage.FromFile(Path.Combine(oryxDir, "uf_heroes_simple.png"));
        var bases = Directory
            .EnumerateFiles(heroesFolder, "*_1.png")
            .Select(f => Path.GetFileName(f)[..^"_1.png".Length])
            .Where(name => !heroIdentityOverrides.ContainsKey(name))
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
        Console.WriteLine($"  (skipping uf_heroes - not present under {splitDir})");
    }
}

// The identity-lookup sheet is never bundled; only uf_heroes.png ships.
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

    byte[] pngBytes = File.ReadAllBytes(sheetSourcePaths[sheetName]);
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
