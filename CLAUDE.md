# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

BlazorRogue is a procedurally-generated rogue-like built on C#/.NET 10, Blazor Server (unified
Blazor Components hosting model). No database or external services. Nearly all game content (monster/hero stats, floor/wall/liquid sets, decorations, items, map
generation weights) is data-driven from JSON under `Data/` rather than hardcoded.

```
Pages/            Blazor pages (GamePage.razor is the main game view)
Shared/           Shared Razor components
GameObjects/      GameObject and its subclasses (Moveable, Door, Chest, Item, ...)
Components/       Component base class, InventoryComponent, PickupableComponent, UseableComponent
Combat/           Combat system, incl. the Warhammer-inspired ruleset (Combat/Warhammer/)
AI/               Monster AI components
Effects/          EffectsSystem (screen shake) and SoundManager (audio cues)
Vision/           Field-of-view implementation
World/            Map, Tile, Decoration, LiquidEdging (shoreline autotiling); World/Generation/
                  holds the map generators (IMapGenerator and implementors)
Rendering/        SpriteAtlas (licensed-tileset atlas -> CSS), AnimationCssGenerator and
                  HandAuthoredSpriteAnimations (generate @keyframes CSS from monster/hero,
                  liquid-pool, and torch/hit-flash data)
Entities/         Types parsed from configuration, plus Configuration.cs (parses Data/*.json)
Sessions/         Per-browser session state that survives page reloads
Utility/          Small standalone helpers (e.g. string extension methods)
Data/             JSON game data: monsters, heroes, floorsets, wallsets, liquidsets, decorations, items, levels, game-config
Game.cs / References.cs   Core game state
wwwroot/          Static assets: CSS, JS interop, sounds
docker/           Dockerfile (see Commands below)
BlazorRogue.Tests/        xUnit test project
tools/AtlasPacker/        Dev-machine-only tool that packs the licensed tileset into an obfuscated
                  atlas bundle (tools/AtlasPacker.Tests/ covers its matching/CSS-math logic)
```

For engine internals (Game/Sessions/References/Configuration, rendering, map generation, combat,
hosting) and their gotchas, see [`ARCHITECTURE.md`](ARCHITECTURE.md) — read it before making
structural changes. For test-writing conventions and manual browser-verification techniques, see
[`TESTING.md`](TESTING.md).

## Commands

```
dotnet build                    # Build (warnings are treated as errors)
dotnet run                      # Run the app (https://localhost:5001, http://localhost:5000)
dotnet test                     # Run all tests (BlazorRogue.Tests, xUnit)
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"   # Run a single test
dotnet tool restore && dotnet csharpier check .   # Check formatting (what CI runs)
dotnet csharpier format .       # Auto-fix formatting before committing
docker build -f docker/Dockerfile -t blazorrogue .   # Build the Linux container image
docker run -p 8080:8080 blazorrogue   # Run it, then open http://localhost:8080
```

CI (`.github/workflows/CI.yml`) runs `dotnet restore` → `dotnet build --configuration Release` →
`dotnet csharpier check .` → `dotnet test --configuration Release` on every push/PR to `master`.

The proprietary Ultimate Fantasy Tileset is never built into the app or its container image at
all. `tools/AtlasPacker` (run only on a machine that owns the license) packs it into an obfuscated
atlas bundle, deployed separately and pointed at via the `BLAZORROGUE_ART_PATH` environment
variable; `Rendering/SpriteAtlas.cs` loads it at startup if present. Without it, the game falls
back automatically to the built-in ASCII renderer, so the tileset is never required for local dev,
and the single `docker/Dockerfile` is safe to build in CI and push anywhere.

## Guardrails

- Keep the build warning-free (`TreatWarningsAsErrors`, `AnalysisLevel=preview-All`); nullable
  reference types are enabled project-wide, so handle nulls properly rather than suppressing
  warnings.
- `master` is protected — all changes go through a PR, and CI must pass before merging.
- Never check the licensed tileset (or anything derived from it, e.g. a generated `atlas.json`)
  into source control or bake it into a container image — it's deployed out-of-band via
  `BLAZORROGUE_ART_PATH`, kept separate from the app on purpose.
- Add or update tests in `BlazorRogue.Tests` for changes to game logic (combat, configuration
  parsing, map/dungeon generation). For changes that are hard to unit test (rendering, Blazor
  components, JS interop), describe how you manually verified the change (screenshot or in-browser
  testing) in the PR description — see [`TESTING.md`](TESTING.md).
- Run `dotnet csharpier format .` before committing; style/naming conventions beyond that are
  enforced by `.editorconfig`, not documented here.
- Prefer small, focused PRs, especially for anything touching rendering or the hosting model —
  those are the areas most likely to have subtle runtime-only breakage that `dotnet build` won't
  catch.
