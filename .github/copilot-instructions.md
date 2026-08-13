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

`ConfigurationTests` deliberately avoids hardcoding tunable data values (monster stats, level
dimensions, etc.) pulled from the real `Data/*.json` files — those get retuned often, so it asserts
*shape* (sane ranges, non-empty fields, a level's `Number` matching its dictionary key) rather than
exact values. Follow that pattern for new data-driven assertions. `MapGeneratorFactoryTests` covers
the generator-id → `IMapGenerator` dispatch contract in isolation from `Configuration`, by
constructing `LevelConfiguration` directly rather than parsing JSON.

CI runs `dotnet build` then `dotnet test` on every push/PR to `master` via GitHub Actions
(`.github/workflows/CI.yml`).

To run locally, follow the ASP.NET Core Blazor "Get started" instructions. The tileset image
files (`uf_split` from Oryx's Ultimate Fantasy Tileset) are proprietary and excluded from the
repo — the ASCII renderer works without them, but the tileset renderer needs
`wwwroot/img/` populated manually if you own the license.

## Architecture

- **`Game`** (`Game.cs`) is the root object for one playthrough. It owns the `MapGenerator`,
  `Map`, `FightingSystem`, `Configuration`, and `EffectsSystem`. It is created by `GameSession`,
  not by the Blazor component. Prefer `new Game(configuration)` with the shared, already-parsed
  configuration; the parameterless `new Game()` parses its own and is for tests/standalone use.
- **Sessions** (`Sessions/GameSession.cs`, `Sessions/GameSessionStore.cs`) are what make a game survive a page reload,
  since a reload starts a brand new Blazor circuit. `GameSessionStore` is a DI singleton keyed by an
  id the browser keeps in `localStorage`. A session holds only `[id, Game, view preferences,
  timestamps]` — all game-authoritative state stays in `Game`/`Map` — and `GameSession.Game` is
  replaceable, so never cache it across a `StartNewGame()`. Sessions are in-memory only and must
  never hold circuit-scoped state such as `SoundManager`.
- **`References`** (`References.cs`) is a static service-locator-style holder for the current
  `Map`, `Configuration`, `SoundManager`, and `EffectsSystem`.
  Code throughout the engine (e.g. `GameObject.Kill()`) reaches these statics directly rather than
  receiving them via DI/constructor injection — keep this pattern in mind when wiring new code.
  **Gotcha:** they are per-process while several sessions may be alive, and constructing a `Game`
  writes them as a side effect — so every handler touching game state must call
  `session.Activate(soundManager)` first, or one player's input mutates another's map. Safe only
  because the game loop is synchronous. Don't read `References.*` during render.
- **`Configuration`** (`Entities/Configuration.cs`) parses all game data from JSON files under `Data/`
  (`monsters.json`, `heroes.json`, `floorsets.json`, `wallsets.json`, `decorations.json`,
  `levels.json`) into strongly-typed dictionaries (`MoveableType`, `StaticDecorativeObjectType`,
  `TileSet`, `LevelConfiguration`). Nearly all visual/audio/combat-stat tuning is data-driven
  through these files rather than hardcoded. `Parse()` fail-fast validates every level's
  `generator_id` against `MapGeneratorFactory` right after loading `levels.json`, so an unknown id
  breaks at app startup rather than mid-game (see *Map generation* below).
- **Entity/component model**: `GameObject` (`GameObjects/GameObject.cs`) is the abstract base for
  everything placed on the map (`Moveable`, `Door`, `Chest`, `Torch`, `HalfWall`, `CaveEdge`,
  `StaticDecorativeObject`). Behavior is composed via optional `Component` (`Components/Component.cs`)
  subclasses (`AIComponent`, `CombatComponent`, `UseableComponent`, `InventoryComponent`) attached at
  construction — a `Component` always knows its `Owner` via `SetOwner`. AI variants live under
  `AI/` (`SimpleAIComponent`, `RandomWalkAIComponent`).
- **Map & rendering**: `World/Map.cs` holds the `Tile` grid; a map generator procedurally builds it
  (see *Map generation* below). `Vision/` implements field-of-view (`AdamMilVisibility` algorithm).
  Rendering is split between a tileset path and an ASCII path — `GameObject.Render(Map map)` is the
  per-object hook, and `Pages/Indoor.razor` is the Blazor page that render the grid, switching
  between tileset and ASCII based on the `renderAscii` flag.
- **Map generation**: each level in `Data/levels.json` (parsed into `LevelConfiguration`) names its
  map generator by a string id (e.g. `"basic_dungeon_generator"`) instead of the game hardcoding
  one. `World/MapGeneratorFactory.cs` maps that id to a concrete `IMapGenerator` via a small
  `Dictionary<string, Func<...>>` registry — each generator exposes its own id as a
  `public const string Id` (e.g. `BasicDungeonGenerator.Id`), so id and class can't drift apart.
  `World/DungeonGeneratorBase.cs` is the shared abstract base for room-and-corridor-style
  generators; `World/BasicDungeonGenerator.cs` (rooms + corridors) and `World/CaveGenerator.cs`
  (cellular automaton) are its two concrete subclasses.
- **Map-generator parameters**: a level's `map_generator.parameters` JSON is parsed into a
  `SettingsMap` (`Entities/SettingsMap.cs`) — a small recursive value tree of int/double/string/
  nested-map, read via typed getters (`GetInt`/`GetDouble`/`GetString`/`GetMap`), each with a
  required form and a `(key, defaultValue)` form. This keeps `System.Text.Json` confined to
  `Configuration.cs` — generators never reference it. Settings shared by every
  `DungeonGeneratorBase` subclass live under a `"common"` key; a generator's own settings live
  under `"layout"`. **Gotcha:** primary-constructor field initializers can't reference another
  instance field/method of the same type (`CS0236`) — only static members and the primary
  constructor's own parameters are visible, so reading `SettingsMap` values in a field initializer
  goes through a `private static` helper (e.g. `BasicDungeonGenerator.LayoutSettings`) rather than
  an intermediate instance field.
- **Input**: keys drive the game via a `document`-level `keyup` listener registered from JS
  (`blazorroguefuncs.registerKeyup`), not a Blazor `@onkeyup` on the map div — so movement works
  regardless of what element has focus, with no click-to-focus step. It calls back into
  `Indoor.OnGlobalKeyUp` (`[JSInvokable]`), which must call `StateHasChanged` itself since a
  JS-invoked callback doesn't auto-render like a Blazor-bound event does.
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
  per-type `Parse*Type` method pattern. Map-generator parameters are the exception — those go
  through `SettingsMap` instead (see *Map-generator parameters* above), not a typed `Parse*Type`.
