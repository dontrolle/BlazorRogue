// Locator/packer for the licensed Ultimate Fantasy tileset.
//
// Run only on a machine that owns the Oryx license and has the vendor's own download (the
// "oryx_ultimate_fantasy" folder) - nothing else. It supplies both the full pre-packed sheets and,
// under its own uf_split/ subfolder, every individually-cropped file needed to identify which
// rectangle of a vendor sheet each sprite name occupies - matching runs straight against that.
//
// The one thing uf_split/ doesn't have is the handful of hand-picked HUD icons this project uses
// (blood meter, gold, attack/damage/defense) - the vendor's own split naming for those categories
// is purely numeric (e.g. "uf_skills_08.png"), not descriptive, so there's no name to match by.
// Those are hardcoded below instead, their coordinates located once by hand and fixed from then on
// - the underlying vendor pixels never change.
//
// Writes atlas.json + obfuscated sheet bundles to an output directory, for the app to load at
// runtime via BLAZORROGUE_ART_PATH. Nothing here is checked into source control or run in CI
// beyond this project's own unit tests (which use synthetic bitmaps, not the licensed assets).
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

// sourcePath defaults to the vendor's own <sheetName>.png; passing one explicitly is how a
// "sheet" can instead be built from some other single PNG - see the cloak residuals below, each
// of which becomes a one-sprite sheet of its own.
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

// floor_tile_sandy_1 sits exactly where its siblings _2/_3/_4 (matched automatically below, at
// consecutive X positions on the same row) predict it should - but the vendor's own standalone
// export of this one file has 3 pixels along its right edge that are a slightly different shade
// than the master sheet at that same position, so exact-match correctly refuses to paper over the
// discrepancy. Hardcoded here from the master sheet's own pixels (the actual rendered source
// either way) and skipped in the pass below, rather than logged as a miss - unlike the tool's
// other misses, this one is real, currently-unreferenced art someone might want to wire up as a
// floorset later.
sprites["floor_tile_sandy_1"] = new SpriteEntry("uf_terrain", 48, 480, 48, 48);
MatchFolderAgainstSheet(
    "uf_terrain",
    "uf_terrain",
    skip: new HashSet<string> { "floor_tile_sandy_1" }
);

Console.WriteLine("Matching items...");

// These 9 cloak recolors exist as their own files in the vendor's split export, but only one
// cloak (cloak_cloth) was ever baked into the master uf_items.png sheet - checked exhaustively
// against every sheet, not just this category's own. A real gap in the vendor's own asset
// pipeline, not something exact-match against a sheet could ever resolve. Rather than build a
// packer to combine them, each becomes a one-sprite "sheet" of its own, obfuscated straight from
// its own split file - same machinery as every other sheet, just a different source PNG - and
// skipped in the pass below, rather than logged as a miss.
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

Console.WriteLine("Matching heroes/monsters (via uf_heroes_simple.png identity lookup)...");

// These three don't exist at all in uf_heroes_simple.png (checked exhaustively), so the identity
// lookup below can never find them - but their grid cell in uf_heroes.png itself is identifiable
// by eye (visually confirmed: a dove, a black wolf, and - for merchant_b - a robed figure sitting
// directly next to merchant_a's own resolved cell, one column over, in the same row). Hardcoded
// here from that grid position rather than left unresolved.
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
