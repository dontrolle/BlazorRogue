# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

BlazorRogue is a procedurally-generated rogue-like built on C#/.NET 10, Blazor Server (unified
Blazor Components hosting model). No database or external services. Nearly all game content (monster/hero stats, floor/wall sets, decorations, map
generation weights) is data-driven from JSON under `Data/` rather than hardcoded.

```
Pages/            Blazor pages (GamePage.razor is the main game view)
Shared/           Shared Razor components
GameObjects/      GameObject and its subclasses (Moveable, Door, Chest, ...)
Components/       Component base class, InventoryComponent, UseableComponent
Combat/           Combat system, incl. the Warhammer-inspired ruleset (Combat/Warhammer/)
AI/               Monster AI components
Effects/          EffectsSystem (screen shake) and SoundManager (audio cues)
Vision/           Field-of-view implementation
World/            Map, Tile, Decoration, map generators (IMapGenerator and implementors)
Entities/         Types parsed from configuration, plus Configuration.cs (parses Data/*.json)
Sessions/         Per-browser session state that survives page reloads
Utility/          Small standalone helpers (e.g. string extension methods)
Data/             JSON game data: monsters, heroes, floorsets, wallsets, decorations, levels
Game.cs / References.cs   Core game state
wwwroot/          Static assets: CSS, JS interop, sounds, tileset images (gitignored)
docker/           Dockerfile and Dockerfile.graphics (see Commands below)
BlazorRogue.Tests/        xUnit test project
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

The proprietary Ultimate Fantasy Tileset image assets are excluded from the repo (`wwwroot/img/`
populated manually if you own a license) — without them the game falls back automatically to the
built-in ASCII renderer, so the tileset is never required for local dev.

`docker/Dockerfile.graphics`/`docker/Dockerfile.graphics.dockerignore` are a **local-build-only**
variant that bundles whatever tileset is present on disk, for producing a hosted playtest build.
Never build it in CI (the tileset isn't there) and never push the resulting image to a public
registry — it embeds proprietary, licensed assets:

```
az acr login --name <registry>
docker build -f docker/Dockerfile.graphics -t <registry>.azurecr.io/blazorrogue:graphics .
docker push <registry>.azurecr.io/blazorrogue:graphics
az containerapp update -n <app> -g <resource-group> --image <registry>.azurecr.io/blazorrogue:graphics
```

## Guardrails

- Keep the build warning-free (`TreatWarningsAsErrors`, `AnalysisLevel=preview-All`); nullable
  reference types are enabled project-wide, so handle nulls properly rather than suppressing
  warnings.
- `master` is protected — all changes go through a PR, and CI must pass before merging.
- Never let `wwwroot/img/uf_*` (the proprietary tileset) end up in a publicly-shipped or
  publicly-pushed artifact — see the `docker/Dockerfile.graphics` note above.
- Add or update tests in `BlazorRogue.Tests` for changes to game logic (combat, configuration
  parsing, map/dungeon generation). For changes that are hard to unit test (rendering, Blazor
  components, JS interop), describe how you manually verified the change (screenshot or in-browser
  testing) in the PR description — see [`TESTING.md`](TESTING.md).
- Run `dotnet csharpier format .` before committing; style/naming conventions beyond that are
  enforced by `.editorconfig`, not documented here.
- Prefer small, focused PRs, especially for anything touching rendering or the hosting model —
  those are the areas most likely to have subtle runtime-only breakage that `dotnet build` won't
  catch.
