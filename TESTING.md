# Testing

See [`CLAUDE.md`](CLAUDE.md) for the `dotnet test` commands. This file covers conventions for
writing tests and how to manually verify changes that can't be unit tested.

## Tests (`BlazorRogue.Tests`)

xUnit project covering core, UI-independent game logic: dice/combat math (`Combat/`),
`Configuration` JSON parsing, `Map` geometry helpers, item/inventory logic (`ItemTests`,
`ItemInteractionTests`, `Components/InventoryComponentTests`), and end-to-end dungeon-generation
smoke tests.
It references `BlazorRogue.csproj` directly (`InternalsVisibleTo` in `BlazorRogue.csproj` lets tests
wire up internal-setter statics like `References.SoundManager`, mirroring how `Pages/GamePage.razor`
does it at runtime), and mirrors `Data/*.json` into its own output directory since
`Configuration.Parse()` reads them via relative `Data\...` paths at the working directory. Add or
update tests here for changes to game logic (combat, configuration parsing, map/dungeon generation).
For changes that are hard to unit test (rendering, Blazor components, JS interop), describe how you
manually verified the change (screenshot or in-browser testing) in the PR description instead.

`MapTests.CreateMap()` builds a bare `Map` with `game: null!` and without `PostGenInitalize()`. That
is fine for geometry helpers but **not** for anything that kills a `Moveable`: death drops a blood
puddle (needs `Game.Configuration`) and re-renders (needs a post-gen map). Use `new Game()` for
those — it generates a real dungeon in a few ms, as `GameTests` already does.

For a deterministic turn-based scenario (AI pathing, combat, liquid hazards) where a randomly
generated dungeon would make the exact tile layout unpredictable, build a small bare *all-floor*
`Map` wired to a real `Game` instead — see `LiquidPoolTests.BareFloorMap`/`NewCreature` (reused by
`MapTests`/`FightingSystemTests` for the blocked-edge-combat and push-back tests): `new Game()` for
`Game.FightingSystem`/`AddMessage`, then `new Map(size, size, wallSet, game)` with every tile
force-set to a non-blocking floor `TileSet`, and `game.Map = map` (an `internal`-settable property
kept for exactly this) so `Moveable.Move`'s enter-hook and friends - which reach the current map via
`References.Game.Map` - target it too. Exact adjacency/positions are then fully under the test's
control via `PlaceAt`/`AddMonster`/`AddMoveable`, unlike hunting for a suitable spot in a real
generated level.

`Map.TakeTurn(PlayerAction)` (the unified turn entry point - see the *Turn-taking* entry in
[`ARCHITECTURE.md`](ARCHITECTURE.md)) can be called directly on a bare-floor `Map`/`Game` built the
same way, with no driver needed - see `TurnResultTests` for that pattern, exercising `TakeTurn`'s own
turn-resolution rules. `BlazorRogue.Tests/TestSupport/HeadlessPlayDriver.cs` is a thin wrapper around
a `Game` for tests that want to drive many turns without re-deriving the `Game`/bare-floor-`Map`
boilerplate each time, optionally via `RandomHazardAvoidingPolicy` (also under `TestSupport/` - an
`IPlayerPolicy` picking uniformly among the player's currently-legal, hazard-excluding actions)
instead of scripting each `PlayerAction` by hand - see `HeadlessPlayDriverTests` for both, including a
smoke test that hundreds of turns of random play against a real generated dungeon never throw.

For the tick-priority-queue scheduler specifically (see *Tick-priority-queue scheduler* in
[`ARCHITECTURE.md`](ARCHITECTURE.md)) - a monster acting more than once, or sitting out, within a
single `TakeTurn` call - place a moveable with a real `TickCost` (`NewCreature`'s default matches
the player's, so tests that want a faster/slower ratio pass an explicit one) at a controlled
distance via `PlaceAt`/`AddMonster` on a bare-floor `Map`, then assert on
`TurnResult.MonsterActions`' count/contents directly. See
`FasterMonsterActsMultipleTimesWithinASinglePlayerTurn` and the tests after it in
`TurnResultTests` for the pattern, including how they place monsters far enough from the player
that `SimpleAIComponent` only ever moves toward it (so `MonsterActions`' count reflects
action-count, not attack-vs-move outcome).

`ConfigurationTests` deliberately avoids hardcoding tunable data values (monster combat stats, level
dimensions, etc.) pulled from the real `Data/*.json` files it parses — those get retuned often, and
locking in a specific number (e.g. an ogre's exact wounds) makes an unrelated balance change break an
unrelated test. Instead it asserts *shape*: every monster's stats are in a sane range
(`ParseLoadsMonsterStatsWithSaneValues`), every level's `Number` matches its dictionary key and its
id/name/dimensions are non-trivial (`ParseLoadsLevelsWithSaneData`), etc. Follow this pattern for new
data-driven assertions here rather than asserting exact values from the JSON. `MapGeneratorFactoryTests`
covers the generator-id → `IMapGenerator` dispatch contract (`MapGeneratorFactory.Create`/`IsKnown`)
in isolation from `Configuration`, by constructing `LevelConfiguration` directly rather than parsing
JSON — keeping it independent of how `SettingsMap`/parsing evolves.

## Verifying in the browser

Session, rendering and input behaviour can't be unit tested, so it gets checked by driving the real
app. The `run-blazorrogue` skill (`.claude/skills/run-blazorrogue/SKILL.md`) is the current way to
do this for an agent - it wraps a small Playwright-based REPL (`driver.mjs`) that starts the dev
server, dispatches synthetic key events, takes screenshots, and reads the DOM/message log, covering
every technique below; see that file for the driver's own commands and gotchas (notably: a
`messages` query can lag one round-trip behind the actual game state - issue an extra, throwaway
`messages` call and re-check before trusting what it reports as the latest line). Hard-won notes:

- **A dedicated dev-only debug level is worth it for anything with many shapes/variants to eyeball
  at once**, rather than hand-splicing placement code per manual check - see
  `FenceGalleryMapGenerator` for the pattern (a fixed, deterministic `IMapGenerator` wired to
  `Data/game-config.json`'s `debug_level`, reached in a running game via Ctrl+D then Ctrl+G - see
  `Game.ToggleDebugLevelView`, and the *Edge-blocking* entry in [`ARCHITECTURE.md`](ARCHITECTURE.md)
  for the kind of behavioral (not just visual) check this level also enabled: a monster placed
  deliberately across a fence line to confirm combat, not just movement, is blocked by it).
- **Crop and upscale a screenshot region (nearest-neighbor) when checking a pixel-thin visual
  detail** - a full-map screenshot compresses a 1-2px difference (e.g. two adjacent decorations
  interlocking into a slightly thicker line) below what's visible at a glance. A quick
  `System.Drawing` crop via the `PowerShell` tool, scaled up 3-4x with
  `InterpolationMode.NearestNeighbor` (so pixels stay sharp-edged rather than blurring together),
  makes the difference legible; plain eyeballing the original screenshot at that point is
  unreliable enough to draw the wrong conclusion from.
- **Drive the game with synthetic key events, not real keypresses.** Real keypresses can be flaky in
  automation (focus can land somewhere unexpected between steps). Dispatching on `document` matches
  what the game actually listens to and works regardless of focus, and can be looped in a single
  call: `document.dispatchEvent(new KeyboardEvent('keyup', { key: 'd', code: 'KeyD', bubbles: true
  }))`. Blazor Server handles these fine. `OnKeyPress` reads `e.Code` (`KeyD`), so set **both** `key`
  and `code`. Allow ~60ms between events for the circuit round trip. Since input is a document-level
  listener (see *Input* in [`ARCHITECTURE.md`](ARCHITECTURE.md)), a good regression check is
  dispatching a movement key right after clicking some other element (e.g. a button) and confirming
  the player still moves.
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
- **The inventory modal** (`i`, or `u` to open it already armed for use/equip) is Blazor UI, not
  unit-tested. Verify it by dispatching `g` on an item tile, then `u`, and checking the modal lists
  the item — in tileset mode each row has an `<img class="inventory_icon">` (its `uf_items` sprite);
  in ASCII mode a `<span class="inventory_glyph">` with the item's coloured character. Then dispatch
  the item's letter and confirm it's used/equipped and the modal closes.
- **The move-replay JS engine** (see *Turn-visualization* in [`ARCHITECTURE.md`](ARCHITECTURE.md))
  is easiest checked directly rather than by hunting for an organic multi-move monster turn: every
  moveable's decoration carries `data-moveable-id`, so inject a synthetic payload -
  `window.blazorRogueReplay.play([{ id: <a moveable's data-moveable-id>, offsets: [[-96, 0], [-48,
  0]] }], 300)` - and screenshot at 0ms/~320ms/~640ms to confirm the element's `style.transform`
  steps through each offset and clears at the end. To instead observe it end-to-end from a real
  keypress, monkey-patch the hook first (`const orig = window.blazorRogueReplay.play;
  window.blazorRogueReplay.play = (entries, delay) => { /* record entries */ return
  orig(entries, delay); }`) and check what it recorded after each key. Triggering a *genuine*
  multi-`Moved` turn organically is fiddly: `SimpleAIComponent` closes distance by exactly one tile
  per action regardless of direction (including diagonals), so a monster within 2 tiles of the
  player converts its second action into an `Attacked`, not a `Moved` (no replay entry - correctly,
  since `BuildMoveReplay` skips anything with ≤1 waypoint) - it takes real open ground and a
  starting distance of 3+ tiles (not the `fence_gallery` debug level, whose fixed goblin sits behind
  blocked fence edges that tend to produce a movement stalemate instead) to see two consecutive
  `Moved` outcomes in one player turn.
