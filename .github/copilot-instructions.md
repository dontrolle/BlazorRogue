# BlazorRogue

A small rogue-like built in a custom game engine on C#/Blazor Server (.NET 10). Uses the modern
unified Blazor Components hosting model (`MapRazorComponents`/`AddInteractiveServerRenderMode`).
Supports both a tileset renderer (Ultimate Fantasy tileset, not included in repo) and a custom
ASCII renderer, switchable client-side.

## Build

```
dotnet build
```

`BlazorRogue.Tests` is an xUnit test project covering core, UI-independent logic (dice/combat math,
`Configuration` JSON parsing, `Map` geometry, and dungeon-generation smoke tests). Run it with
`dotnet test`. It references `BlazorRogue.csproj` directly (`InternalsVisibleTo` is set up so tests
can wire up `References.SoundManager` etc.) and mirrors `Data/*.json` into its own output directory
since `Configuration.Parse()` reads them via relative paths. There is no separate lint step (rely on
`.editorconfig` conventions and compiler warnings — nullable reference types are enabled, so watch
for new nullability warnings).

CI runs `dotnet build` then `dotnet test` on every push/PR to `master` via GitHub Actions
(`.github/workflows/build.yml`).

To run locally, follow the ASP.NET Core Blazor "Get started" instructions. The tileset image
files (`uf_split` from Oryx's Ultimate Fantasy Tileset) are proprietary and excluded from the
repo — the ASCII renderer works without them, but the tileset renderer needs
`wwwroot/img/` populated manually if you own the license.

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
  visual/audio/combat-stat tuning is data-driven through these files rather than hardcoded.
- **Entity/component model**: `GameObject` (`GameObjects/GameObject.cs`) is the abstract base for
  everything placed on the map (`Moveable`, `Door`, `Chest`, `Torch`, `HalfWall`, `CaveEdge`,
  `StaticDecorativeObject`). Behavior is composed via optional `Component` subclasses
  (`AIComponent`, `CombatComponent`, `UseableComponent`, `InventoryComponent`) attached at
  construction — a `Component` always knows its `Owner` via `SetOwner`. AI variants live under
  `AI/` (`SimpleAIComponent`, `RandomWalkAIComponent`).
- **Map & rendering**: `Map.cs` holds the `Tile` grid; `DungeonGenerator.cs` procedurally builds it.
  `Vision/` implements field-of-view (`AdamMilVisibility` algorithm). Rendering is split between a
  tileset path and an ASCII path — `GameObject.Render(Map map)` is the per-object hook, and
  `Pages/Indoor.razor` / `Pages/Outdoor.razor` are the Blazor pages that render the grid, switching
  between tileset and ASCII based on the `renderAscii` flag.
- **Combat**: lives under `Combat/`, with a specific ruleset in `Combat/Warhammer/` (e.g.
  `FightingSystem`, `Dice`) — combat stats (weapon skill, damage, toughness, armour, wounds) are
  parsed from the same `Configuration` JSON files.
- **Hosting**: `Program.cs` uses the minimal hosting API + unified Blazor Components model
  (`AddRazorComponents().AddInteractiveServerComponents()` /
  `MapRazorComponents<App>().AddInteractiveServerRenderMode()`). `App.razor` is the root HTML shell
  (`<HeadOutlet>` + `<Routes>`), and `Routes.razor` holds the `<Router>`. Note: `~/`-style Tag
  Helper URL resolution (e.g. `<base href="~/">`) does **not** work inside `.razor` components —
  only inside `.cshtml` Razor Pages — so static asset URLs in `App.razor` must be plain absolute
  paths (e.g. `<base href="/">`).

## Conventions

- Indentation is 2 spaces (`.editorconfig`), UTF-8 with BOM for `.cs`/`.razor` files.
- Nullable reference types are enabled (`<Nullable>enable</Nullable>` in the `.csproj`); prefer
  proper null-handling over suppressing warnings when touching nearby code.
- New JSON-configurable game data (monster/hero stats, tilesets, decorations) belongs in the
  matching file under `Data/`, parsed via `Configuration.ParseDataFile` following the existing
  per-type `Parse*Type` method pattern.
