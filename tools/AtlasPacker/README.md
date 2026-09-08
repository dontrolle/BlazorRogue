# AtlasPacker

Dev-machine-only tool: packs the licensed Ultimate Fantasy tileset into an obfuscated atlas bundle
for the main app to load via `BLAZORROGUE_ART_PATH`. Never run in CI, never checked into source
control (its output is derived from licensed content), and never referenced by `BlazorRogue.csproj`
(it pulls in SkiaSharp, which the main app has no other reason to depend on).

## What it needs

Just the vendor's own download - the root of the `oryx_ultimate_fantasy` folder. It supplies
everything: the small pre-packed full sheets (`uf_terrain.png`, `uf_items.png`,
`uf_FX_impact.png`, `uf_heroes.png`, `uf_heroes_simple.png`, `uf_skills.png`, `uf_interface.png`)
plus, under its own `uf_split/` subfolder, every individually-cropped file needed to identify
which rectangle of a vendor sheet each sprite name occupies - so there's nothing to separately
populate in this repo. The handful of hand-picked HUD icons that aren't vendor-exported at all
(blood meter, gold, attack/damage/defense) are hardcoded directly in `Program.cs` instead - see its
top comment.

## Running it

```
dotnet run --project tools/AtlasPacker -- --oryx <path to oryx_ultimate_fantasy> --out <output dir>
```

Writes `atlas.json` plus one obfuscated `<N>.bin` per referenced vendor sheet to `--out`. Point
`BLAZORROGUE_ART_PATH` at that directory for the app to pick it up (see
`Rendering/SpriteAtlas.cs`).

The tool logs a `MISS` line for any `uf_split/` file it can't match and exits non-zero if there are
any. A miss for something you don't recognize is worth checking against `Data/*.json` before
worrying about it - a fair number of files in `uf_split/` aren't referenced by anything the game
actually renders.

## Design notes

- **No repacking.** The vendor's own sheets are used close to as-is (obfuscated, not resized or
  recombined) - they're already small and tightly packed, so there's nothing to gain from building
  a bin-packer on top.
- **Heroes/monsters are the one non-trivial case.** Their 4-frame animation isn't present anywhere
  in the vendor's sheets as separate matchable files - only frame 1 matches. The rest live in
  `uf_heroes.png`'s own internal 4-frame-per-character grid, worked out and verified against real
  characters and monsters (see `HeroBlock.cs`). `uf_heroes_simple.png` is used purely as an offline
  lookup key to identify which grid cell a character occupies - it's never shipped.
- **UI icons are hardcoded, not matched.** The vendor's own split-file naming for these categories
  is purely numeric (e.g. `uf_skills_08.png`), not descriptive, so there's no name to search
  `uf_split/` by. Their coordinates were resolved once by hand and are fixed in `Program.cs` -
  the underlying vendor pixels never change, so there's nothing to re-derive on later runs.
