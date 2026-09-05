# Architecture

Detailed engine internals and gotchas for BlazorRogue. Read this before making structural changes
to `Game`, `Sessions`, `References`, `Configuration`, rendering, map generation, or hosting. See
[`CLAUDE.md`](CLAUDE.md) for commands and universal guardrails, and [`TESTING.md`](TESTING.md) for
how to test changes in these areas.

- **`Game`** (`Game.cs`) is the root object for one playthrough. It owns the `MapGenerator`,
  `Map`, `FightingSystem`, `Configuration`, and `EffectsSystem`. It is created by `GameSession`,
  **not** by the Blazor component (see *Sessions* below). Prefer `new Game(configuration)` with the
  shared, already-parsed configuration; the parameterless `new Game()` parses its own and exists for
  tests and standalone use.
- **Sessions** (`Sessions/GameSession.cs`, `Sessions/GameSessionStore.cs`) are what make a game survive a page reload.
  A reload starts a brand new Blazor circuit, so the game cannot live in the component.
  `GameSessionStore` is a DI singleton holding `GameSession`s keyed by an id the browser keeps in
  `localStorage` (`blazorrogue.sessionId`, created by `ensureSessionId()` in `wwwroot/blazorrogue.js`).
  A session holds `[id, Game, view preferences, timestamps]` and nothing else — all
  game-authoritative state stays inside `Game`/`Map`. `GameSession.Game` is **replaceable**
  (`StartNewGame()`), so never cache it anywhere a swap can't reach. Sessions are in memory only:
  they are lost on process restart, and are evicted after ~2h idle or when the LRU cap is hit
  (swept opportunistically inside `GetOrCreate`, so there is no background timer). Because sessions
  outlive circuits they must never hold circuit-scoped state — notably `SoundManager`, which wraps a
  per-circuit `IJSRuntime` and is therefore passed *in* to `Activate()` rather than stored.
- **`References`** (`References.cs`) is a static service-locator-style holder for the current
  `Map`, `Configuration`, `SoundManager`, and `EffectsSystem`. Code throughout the engine (e.g.
  `GameObject.Kill()`, `Chest.Use()`) reaches these statics directly rather than receiving them via
  DI/constructor injection — keep this pattern in mind when wiring new code.
  **Gotcha:** these are per-process, but more than one `GameSession` can be alive at once, and
  constructing a `Game` writes them as a side effect. So whichever game was built last would
  otherwise win — one player's keypress mutating another's map. Every handler that touches game
  state must therefore call `session.Activate(soundManager)` first (see `KeyUp` in
  `Pages/GamePage.razor`); `Activate` is the single place the statics are written. This is safe only
  because the game loop is fully synchronous — nothing awaits between activation and the end of the
  handler, so no other circuit can interleave. **Do not read `References.*` during render**, which
  happens outside any handler; use the component's own `game` instance instead.
- **`Configuration`** (`Entities/Configuration.cs`) parses all game data from JSON files under `Data/`
  (`monsters.json`, `heroes.json`, `floorsets.json`, `wallsets.json`, `liquidsets.json`,
  `decorations.json`, `items.json`, `levels.json`) into strongly-typed dictionaries (`MoveableType`,
  `StaticDecorativeObjectType`, `TileSet`, `LiquidType`, `ItemType`, `LevelConfiguration`). File paths are resolved relative to `AppContext.BaseDirectory`
  (not the process's current working directory), so `Data/*.json` is a `CopyToOutputDirectory`
  content item in `BlazorRogue.csproj` — it ships next to the built assembly in both `dotnet
  build`/`dotnet publish` output. Nearly all visual/audio/combat-stat tuning is data-driven through
  these files rather than hardcoded — new monsters, heroes, floor/wall sets, and decorations can
  usually be added without touching C#. New JSON-configurable data goes in the matching file under
  `Data/`, parsed via a `Parse*Type` method in `Entities/Configuration.cs` following the existing
  pattern (and the `GetRequiredString`/`RequireNonNullString` helpers for required fields).
  `Configuration` is immutable once parsed and is registered as a **DI singleton** shared by every
  `Game` — don't re-parse it per game. `Parse()` also fail-fast validates every level's
  `generator_id` against `MapGeneratorFactory` right after loading `levels.json` (see *Map
  generation* below) and every monster's `ai_component.id` against `AIComponentFactory` right after
  loading `monsters.json` (see *AI components* below), so an unknown id breaks at app startup rather
  than mid-game.
- **Entity/component model**: `GameObject` (`GameObjects/GameObject.cs`) is the abstract base for
  everything placed on the map (`Moveable`, `Door`, `Chest`, `Torch`, `HalfWall`, `WallEdge`,
  `StaticDecorativeObject`, `Item`). Behavior is composed via optional `Component` (`Components/Component.cs`)
  subclasses (`AIComponent`, `CombatComponent`, `UseableComponent`, `InventoryComponent`,
  `PickupableComponent`; the last three live in `Components/` alongside the base class) attached at
  construction — a `Component` always knows its `Owner` via `SetOwner`. AI variants live under `AI/`
  (`SimpleAIComponent`, `RandomWalkAIComponent`).
- **Items & inventory**: an `Item` on the floor carries a `PickupableComponent` wrapping its
  `ItemType` (from `Data/items.json`). `Map.PickUpItemsAtPlayer()` (the `g` key) moves it into the
  player's `InventoryComponent`, which keeps a `SortedDictionary<char, InventoryEntry>` — `use_once`
  items stack on one letter, `equipable` items get their own and toggle `IsEquipped`. `Map`'s
  `UseInventoryItem`/`DropInventoryItem` are the turn-consuming entry points the UI calls; effects
  (`heal`, `armour_bonus`) are applied in `InventoryComponent.ApplyEffect` against the owner's
  `CombatComponent`.
- **Map & rendering**: `World/Map.cs` holds the `Tile` grid; a map generator (see *Map generation*
  below) procedurally builds it — `World/` also holds `Tile.cs`, `TileType.cs`, `Decoration.cs`
  and `Orientation.cs`.
  `Vision/` implements field-of-view (the Adam Milazzo visibility algorithm, `AdamMilVisibility`).
  Rendering is split between a tileset path and an ASCII path — `GameObject.Render(Map map)` is the
  per-object hook, and `Pages/GamePage.razor` is the Blazor page that renders the grid, switching
  between tileset and ASCII based on the `renderAscii` flag.
  Most map state is *derived*: `Decorations`, `MoveableDecorations`, `BlocksLightMap`,
  `BlocksMovementMap` and `IsVisibleMap` are all rebuilt by `PostGenInitalize()`/the `Render*`
  methods. Only `Tiles`, the game-object list, moveables and `IsMappedMap` are authoritative.
  `KeyUp` calls `RenderMoveables()` once, **after** `PlayerTookTurn()`, so a single render always
  reflects the fully-resolved turn (the player's move plus every monster's move/attack/death) —
  nothing in the player- or monster-turn logic needs to re-render itself. Keep it that way: an
  earlier version rendered moveables *before* `PlayerTookTurn()`, which delayed a monster's moved
  position from becoming visible until the *following* turn — by which point it might also be
  attacking, making a two-turn move-then-attack look like it happened in one turn.
- **Input**: keys drive the game via a `document`-level `keyup` listener
  (`blazorroguefuncs.registerKeyup` in `wwwroot/blazorrogue.js`), not a Blazor `@onkeyup` on the map
  div — so movement works no matter what has focus (or nothing does), with no click-to-focus step.
  The listener is registered once in `OnAfterRenderAsync` against a shared `DotNetObjectReference`
  (the same one `blazorViewport.registerResize` uses) and calls back into `GamePage.OnGlobalKeyUp`,
  which forwards to the same `KeyUp`/`OnKeyPress` logic and then calls `StateHasChanged` itself,
  since a JS-invoked callback doesn't auto-render the way a Blazor-bound event does. Both listeners
  are unregistered in `DisposeAsync`.
- **Game over**: `Map.IsGameOver` is set when the player dies — `AddPlayer` subscribes to the
  player's `GameObjectKilled`, mirroring `AddMonster`/`MonsterKilled`. The handler must be
  idempotent: several monsters attack within one `PlayerTookTurn()` and `CombatComponent` re-raises
  `GameObjectKilled` on *every* hit at or below zero wounds. The corpse is left on the map (unlike a
  dead monster, which is removed) over a blood puddle from the shared `PlaceBloodPuddle` helper, with
  its sprite frozen via `Decoration.AnimationPaused`. Note a paused animation class is used rather
  than dropping it — a decoration with neither an animation class nor an image name renders nothing
  at all. `HandlePlayerAction` refuses input once the game is over, backing up the UI's own guards.
- **Map generation**: each level in `Data/levels.json` (parsed into `LevelConfiguration`, see
  `Entities/LevelConfiguration.cs`) names its map generator by a string id (`map_generator
  .generator_id`, e.g. `"basic_dungeon_generator"`) rather than the game hardcoding one.
  `World/Generation/MapGeneratorFactory.cs` maps that id to a concrete `IMapGenerator`
  (`World/Generation/IMapGenerator.cs`) via a small `Dictionary<string, Func<...>>` registry — each
  generator exposes its own id as a `public const string Id` (e.g. `BasicDungeonGenerator.Id`), so
  the id and the class stay in sync without a separate lookup table to maintain by hand.
  `Game`'s constructor resolves the generator via `MapGeneratorFactory.Create(level, this)` and
  exposes it as `Game.MapGenerator` (`IMapGenerator`), not a hardcoded concrete type.
  `World/Generation/MapGeneratorBase.cs` is the shared abstract base for room-and-corridor-style
  generators (rendering/decoration helpers, door placement, `GenerateMap()`'s overall flow), each
  concrete generator implementing `CreateLayout()`.
  `World/Generation/BSPGenerator/BSPMapGenerator.cs` (binary space partitioning) is the primary
  generator going forward; `World/Generation/BasicDungeonGenerator.cs` (rooms + corridors) and
  `World/Generation/CaveGenerator.cs` (cellular automaton) are earlier prototypes, and
  `World/Generation/TestMapGenerator.cs` is a fixed layout used by tests.
  All of `World/Generation/` (plus its types' use of `Map`/`Tile`/`TileType`/`Orientation` from the
  parent `World` namespace, visible without a `using` since C# namespace lookup searches enclosing
  namespaces) lives in the `BlazorRogue.World.Generation` namespace, one level under
  `BlazorRogue.World`.
- **`SettingsMap` / component parameters**: `SettingsMap` (`Entities/SettingsMap.cs`) is the general
  mechanism for a data-driven component's free-form `parameters` JSON — a small recursive value tree
  restricted to int, double, string, and nested maps of the same. It's not specific to map
  generators: a level's `map_generator.parameters` parses into one (see *Map generation* above), and
  so does a monster's `ai_component.parameters` (see *AI components* below); any future
  data-driven-by-id mechanism can reuse it the same way. This keeps `System.Text.Json`/`JsonElement`
  confined to `Configuration.cs` (via the recursive `ParseSettingsMap` helper); consumers never
  reference the JSON library, they just call typed getters (`GetInt`/`GetDouble`/`GetString`/
  `GetMap`), each with a required form (throws a clear error naming the missing/mistyped key) and a
  `(key, defaultValue)` form. For map generators specifically: settings shared by every
  `MapGeneratorBase` subclass (the decoration percentage-chance fields) are grouped under a
  `"common"` key in `parameters`; a generator's own settings (e.g. `BasicDungeonGenerator`'s
  room-size bounds, `CaveGenerator`'s smoothing-pass iteration counts) live under `"layout"` — see
  `Data/levels.json` for the shape. Because every `SettingsMap` lookup used by the generators is the
  `(key, default)` form, an omitted `parameters` block (or an omitted `"common"`/`"layout"` group)
  silently falls back to sensible defaults rather than breaking; the trade-off is that
  `Configuration` can't validate individual parameter keys at startup the way it validates
  `generator_id`, since it has no way to know what keys a given generator expects — a typo only
  surfaces when that level is actually generated.
  **Gotcha:** primary-constructor field initializers can't reference another instance field/method
  of the same type being constructed (`CS0236`) — only static members and the primary constructor's
  own parameters are visible at that point. That's why each generator that reads `SettingsMap`
  values in a field initializer (e.g. `BasicDungeonGenerator.LayoutSettings`,
  `MapGeneratorBase.CommonSettings`) does so via a `static` helper method rather than an
  intermediate instance field (`private`, or `protected` where a subclass reads the same group —
  as `BSPMapGenerator` does for its extra `common` monster-density knob).
- **AI components**: a monster's AI is chosen the same way map generators are — `monsters.json` (and
  `heroes.json`, though it's unused there since the player's `AIComponent` is always `null`) names it
  by an optional `ai_component.id` string, parsed into `MoveableType.AIComponentId` /
  `AIComponentSettings` (`Entities/MoveableType.cs`). Omitting `ai_component` defaults to
  `AIComponentFactory.DefaultId` (`SimpleAIComponent.ComponentId`). `AI/AIComponentFactory.cs` maps
  the id to a concrete `AIComponent` via a small `Dictionary<string, Func<...>>` registry, just like
  `MapGeneratorFactory` — each component exposes its own id as `public const string ComponentId`
  (e.g. `RandomWalkAIComponent.ComponentId`). `Configuration.Parse()` validates every monster's id
  against `AIComponentFactory.IsKnown` right after loading `monsters.json`, the same fail-fast
  pattern used for `generator_id`. `World/Generation/MapGeneratorBase.cs` builds each monster's
  `AIComponent` via `AIComponentFactory.Create(monsterType.AIComponentId, map,
  monsterType.AIComponentSettings)` rather than hardcoding a component type.
- **Combat**: lives under `Combat/`, with a specific ruleset in `Combat/Warhammer/`
  (`FightingSystem`, `Dice`) — combat stats (weapon skill, damage, toughness, armour, wounds) are
  parsed from the same `Configuration` JSON files.
- **Hosting**: `Program.cs` uses the minimal hosting API plus the unified Blazor Components model
  (`AddRazorComponents().AddInteractiveServerComponents()` /
  `MapRazorComponents<App>().AddInteractiveServerRenderMode()`), and registers the `Configuration`,
  `TimeProvider` and `GameSessionStore` singletons. `GameSessionStore` is registered with an explicit
  factory so the container can't pick its tests-only constructor overload. Note that prerendering
  constructs the page component twice, so anything expensive in a field initializer runs twice per
  page load — which is why the game is resolved in `OnAfterRenderAsync` (also the earliest point JS
  interop, and therefore the session id, is available). `App.razor` is the root HTML shell
  (`<HeadOutlet>` + `<Routes>`), and `Routes.razor` holds the `<Router>`. Gotcha: `~/`-style Tag
  Helper URL resolution (e.g. `<base href="~/">`) does **not** work inside `.razor` components —
  only inside `.cshtml` Razor Pages — so static asset URLs in `App.razor` must be plain absolute
  paths (e.g. `<base href="/">`).
