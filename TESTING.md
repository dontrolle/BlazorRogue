# Testing

See [`CLAUDE.md`](CLAUDE.md) for the `dotnet test` commands. This file covers conventions for
writing tests and how to manually verify changes that can't be unit tested.

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
app (`dotnet run --urls http://localhost:5000`). Hard-won notes:

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
