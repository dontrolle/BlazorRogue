# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```
dotnet build                    # Build (warnings are treated as errors)
dotnet run                      # Run the app (https://localhost:5001, http://localhost:5000)
dotnet test                     # Run all tests (BlazorRogue.Tests, xUnit)
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"   # Run a single test
dotnet tool restore && dotnet csharpier check .   # Check formatting (what CI runs)
dotnet csharpier format .       # Auto-fix formatting
```

There is no separate lint step beyond `.editorconfig` conventions (see below) and the compiler's
nullable-reference-type / analyzer warnings — the build is warning-free by design
(`TreatWarningsAsErrors`, `AnalysisLevel=preview-All`), keep it that way. CI (`.github/workflows/CI.yml`)
runs `dotnet restore` → `dotnet build --configuration Release` → `dotnet csharpier check .` →
`dotnet test --configuration Release` on every push/PR to `master`. `master` is protected; all
changes go through a PR.

No database or external services are required. A fresh dungeon is procedurally generated on every
page load. The proprietary Ultimate Fantasy Tileset image assets are excluded from the repo
(`wwwroot/img/` populated manually if you own a license) — without them the game falls back
automatically to the built-in ASCII renderer, so the tileset is never required for local dev.

## Architecture

- **`Game`** (`Game.cs`) is the root object built once per game session. It owns the
  `DungeonGenerator`, `Map`, `FightingSystem`, `Configuration`, and `EffectsSystem`.
- **`References`** (`References.cs`) is a static service-locator-style holder for the current
  `Map`, `Configuration`, `SoundManager`, and `EffectsSystem`, set up during `Game`'s constructor.
  Code throughout the engine (e.g. `GameObject.Kill()`) reaches these statics directly rather than
  receiving them via DI/constructor injection — keep this pattern in mind when wiring new code.
- **`Configuration`** (`Configuration.cs`) parses all game data from JSON files under `Data/`
  (`monsters.json`, `heroes.json`, `floorsets.json`, `wallsets.json`, `decorations.json`) into
  strongly-typed dictionaries (`MoveableType`, `StaticDecorativeObjectType`, `TileSet`). Nearly all
  visual/audio/combat-stat tuning is data-driven through these files rather than hardcoded — new
  monsters, heroes, floor/wall sets, and decorations can usually be added without touching C#. New
  JSON-configurable data goes in the matching file under `Data/`, parsed via a `Parse*Type` method
  in `Configuration.cs` following the existing pattern (and the `GetRequiredString`/
  `RequireNonNullString` helpers for required fields).
- **Entity/component model**: `GameObject` (`GameObjects/GameObject.cs`) is the abstract base for
  everything placed on the map (`Moveable`, `Door`, `Chest`, `Torch`, `HalfWall`, `CaveEdge`,
  `StaticDecorativeObject`). Behavior is composed via optional `Component` subclasses
  (`AIComponent`, `CombatComponent`, `UseableComponent`, `InventoryComponent`) attached at
  construction — a `Component` always knows its `Owner` via `SetOwner`. AI variants live under
  `AI/` (`SimpleAIComponent`, `RandomWalkAIComponent`).
- **Map & rendering**: `Map.cs` holds the `Tile` grid; `DungeonGenerator.cs` procedurally builds it.
  `Vision/` implements field-of-view (the Adam Milazzo visibility algorithm, `AdamMilVisibility`).
  Rendering is split between a tileset path and an ASCII path — `GameObject.Render(Map map)` is the
  per-object hook, and `Pages/Indoor.razor` is the Blazor page that renders the grid, switching
  between tileset and ASCII based on the `renderAscii` flag.
- **Combat**: lives under `Combat/`, with a specific ruleset in `Combat/Warhammer/`
  (`FightingSystem`, `Dice`) — combat stats (weapon skill, damage, toughness, armour, wounds) are
  parsed from the same `Configuration` JSON files.
- **Hosting**: `Program.cs` uses the minimal hosting API plus the unified Blazor Components model
  (`AddRazorComponents().AddInteractiveServerComponents()` /
  `MapRazorComponents<App>().AddInteractiveServerRenderMode()`). `App.razor` is the root HTML shell
  (`<HeadOutlet>` + `<Routes>`), and `Routes.razor` holds the `<Router>`. Gotcha: `~/`-style Tag
  Helper URL resolution (e.g. `<base href="~/">`) does **not** work inside `.razor` components —
  only inside `.cshtml` Razor Pages — so static asset URLs in `App.razor` must be plain absolute
  paths (e.g. `<base href="/">`).

## Tests (`BlazorRogue.Tests`)

xUnit project covering core, UI-independent game logic: dice/combat math (`Combat/`),
`Configuration` JSON parsing, `Map` geometry helpers, and end-to-end dungeon-generation smoke tests.
It references `BlazorRogue.csproj` directly (`InternalsVisibleTo` in `BlazorRogue.csproj` lets tests
wire up internal-setter statics like `References.SoundManager`, mirroring how `Pages/Indoor.razor`
does it at runtime), and mirrors `Data/*.json` into its own output directory since
`Configuration.Parse()` reads them via relative `Data\...` paths at the working directory. Add or
update tests here for changes to game logic (combat, configuration parsing, map/dungeon generation).
For changes that are hard to unit test (rendering, Blazor components, JS interop), describe how you
manually verified the change (screenshot or in-browser testing) in the PR description instead.

## Conventions

- Indentation is 2 spaces for `.csproj`, 4 spaces for `.cs` (`.editorconfig`); UTF-8 with BOM, LF
  line endings. `.editorconfig` is extensive (naming rules, expression-bodied member preferences,
  analyzer severities) — most deviations from the .NET defaults are annotated inline there with a
  `# Default: ...` comment explaining the change; consult it rather than guessing on style questions.
- Nullable reference types are enabled project-wide; prefer proper null-handling over suppressing
  warnings when touching nearby code.
- Formatting is enforced by CSharpier (`dotnet-tools.json`), checked in CI — run
  `dotnet csharpier format .` before committing if unsure.
- Small, focused PRs are preferred over large ones, especially for anything touching rendering or
  the hosting model — those are the areas most likely to have subtle runtime-only breakage that
  `dotnet build` won't catch.
