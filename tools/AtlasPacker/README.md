# AtlasPacker

Dev-machine-only tool: packs a licensed copy of the Ultimate Fantasy Tileset into an obfuscated
atlas bundle for the main app to load via `BLAZORROGUE_ART_PATH`. Requires the tileset license.
Never run in CI, never checked into source control (its output is derived from licensed content),
and never referenced by `BlazorRogue.csproj` (it pulls in SkiaSharp, which the app has no other
reason to depend on).

## Running it

```
dotnet run --project tools/AtlasPacker -- --oryx <path to the vendor download> --out <output dir>
```

Writes `atlas.json` plus one obfuscated `.bin` per referenced sheet to `--out`. Point
`BLAZORROGUE_ART_PATH` at that directory for the app to pick it up (see
`Rendering/SpriteAtlas.cs`).

The tool logs a `MISS` line for anything it can't place and exits non-zero if there are any.

Without the license, the tool has nothing to run against and the game falls back to the built-in
ASCII renderer — no setup needed for local dev.
