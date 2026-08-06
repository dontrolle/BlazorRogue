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
docker build -t blazorrogue .   # Build the Linux container image (see Dockerfile)
docker run -p 8080:8080 blazorrogue   # Run it, then open http://localhost:8080
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

A `Dockerfile`/`.dockerignore` at the repo root build a Linux container image via multi-stage
`dotnet publish` (`mcr.microsoft.com/dotnet/sdk:10.0` → `mcr.microsoft.com/dotnet/aspnet:10.0`).
`.dockerignore` deliberately excludes `wwwroot/img/uf_*` so the tileset can never end up in the
image even if it's present on the build machine's disk — the containerized game always runs in
ASCII mode.

`Dockerfile.graphics`/`Dockerfile.graphics.dockerignore` are a **local-build-only** variant for
producing a tileset-enabled image (e.g. for a hosted playtest build) — identical build, but the
paired `.dockerignore` doesn't exclude `wwwroot/img/uf_*`, so it bundles whatever tileset is present
on disk (Docker prefers a `<dockerfile-name>.dockerignore` sibling over `.dockerignore` when built
with `-f`). Never build this in CI (the tileset isn't there) and never push the resulting image to a
public registry — it embeds proprietary, licensed assets. Build and ship it with:

```
az acr login --name <registry>
docker build -f Dockerfile.graphics -t <registry>.azurecr.io/blazorrogue:graphics .
docker push <registry>.azurecr.io/blazorrogue:graphics
az containerapp update -n <app> -g <resource-group> --image <registry>.azurecr.io/blazorrogue:graphics
```

## Architecture

- **`Game`** (`Game.cs`) is the root object for one playthrough. It owns the `DungeonGenerator`,
  `Map`, `FightingSystem`, `Configuration`, and `EffectsSystem`. It is created by `GameSession`,
  **not** by the Blazor component (see *Sessions* below). Prefer `new Game(configuration)` with the
  shared, already-parsed configuration; the parameterless `new Game()` parses its own and exists for
  tests and standalone use.
- **Sessions** (`GameSession.cs`, `GameSessionStore.cs`) are what make a game survive a page reload.
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
  `Pages/Indoor.razor`); `Activate` is the single place the statics are written. This is safe only
  because the game loop is fully synchronous — nothing awaits between activation and the end of the
  handler, so no other circuit can interleave. **Do not read `References.*` during render**, which
  happens outside any handler; use the component's own `game` instance instead.
- **`Configuration`** (`Configuration.cs`) parses all game data from JSON files under `Data/`
  (`monsters.json`, `heroes.json`, `floorsets.json`, `wallsets.json`, `decorations.json`) into
  strongly-typed dictionaries (`MoveableType`, `StaticDecorativeObjectType`, `TileSet`). File paths
  are resolved relative to `AppContext.BaseDirectory` (not the process's current working directory),
  so `Data/*.json` is a `CopyToOutputDirectory` content item in `BlazorRogue.csproj` — it ships next
  to the built assembly in both `dotnet build`/`dotnet publish` output. Nearly all
  visual/audio/combat-stat tuning is data-driven through these files rather than hardcoded — new
  monsters, heroes, floor/wall sets, and decorations can usually be added without touching C#. New
  JSON-configurable data goes in the matching file under `Data/`, parsed via a `Parse*Type` method
  in `Configuration.cs` following the existing pattern (and the `GetRequiredString`/
  `RequireNonNullString` helpers for required fields). `Configuration` is immutable once parsed and
  is registered as a **DI singleton** shared by every `Game` — don't re-parse it per game.
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
  (the same one `blazorViewport.registerResize` uses) and calls back into `Indoor.OnGlobalKeyUp`,
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

`MapTests.CreateMap()` builds a bare `Map` with `game: null!` and without `PostGenInitalize()`. That
is fine for geometry helpers but **not** for anything that kills a `Moveable`: death drops a blood
puddle (needs `Game.Configuration`) and re-renders (needs a post-gen map). Use `new Game()` for
those — it generates a real dungeon in a few ms, as `GameTests` already does.

## Verifying in the browser

Session, rendering and input behaviour can't be unit tested, so it gets checked by driving the real
app (`dotnet run --urls http://localhost:5000`). Hard-won notes:

- **Drive the game with synthetic key events, not real keypresses.** Real keypresses can be flaky in
  automation (focus can land somewhere unexpected between steps). Dispatching on `document` matches
  what the game actually listens to and works regardless of focus, and can be looped in a single
  call: `document.dispatchEvent(new KeyboardEvent('keyup', { key: 'd', code: 'KeyD', bubbles: true
  }))`. Blazor Server handles these fine. `OnKeyPress` reads `e.Code` (`KeyD`), so set **both** `key`
  and `code`. Allow ~60ms between events for the circuit round trip. Since input is a document-level
  listener (see *Input* above), a good regression check is dispatching a movement key right after
  clicking some other element (e.g. a button) and confirming the player still moves.
- **Assert on the DOM, not on screenshots.** Every tile is `<div id="x,y" class="cell">` and every
  decoration carries `alt="x,y (Name=..., Blocking=...)"`, so map state can be snapshotted as a
  dictionary keyed by cell id. Computed styles are the way to check rendering (`animationPlayState`,
  `zIndex`, `backgroundImage`).
- **Any such snapshot is viewport-dependent.** The visible window is re-fitted on resize, so a
  browser-window change alters how many cells render and makes two snapshots incomparable. Compare
  only cells present in *both*, or keep the window size fixed.
- **To test two independent players**, set a different `blazorrogue.sessionId` in `localStorage`
  before opening a second tab. The sharp check for the `References` cross-wiring bug is to make the
  *second* tab's game the most recently constructed, then confirm keys in the *first* tab still move
  the first tab's player and leave the second untouched.
- **To reach player death**, temporarily set the hero to `"wounds": 1, "toughness": 0, "armour": 0`
  in `Data/heroes.json`, then `git checkout Data/heroes.json` and re-run the build/tests against the
  real data. Wandering to find a monster is unreliable (400 random steps in one dungeon found none);
  cycling **New game** and waiting a few turns in each is far faster, since any hit is then lethal.
- Screenshot and zoom regions are in **device** pixels, while `getBoundingClientRect()` returns CSS
  pixels — scale by the display factor (e.g. 1568/1280 ≈ 1.225) or the zoom will miss its target.

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
