# BlazorRogue

[![CI](https://github.com/dontrolle/BlazorRogue/actions/workflows/CI.yml/badge.svg)](https://github.com/dontrolle/BlazorRogue/actions/workflows/build.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A small rogue-like built from the bottom up in a custom game engine on C#/Blazor. Features a tileset renderer using the beautiful [Ultimate Fantasy Tileset from Oryx](https://www.oryxdesignlab.com/ultimatefantasy), and a custom-built ASCII renderer, switchable at any time.

## Table of contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Getting started](#getting-started)
- [How to play](#how-to-play)
- [Tileset](#tileset)
- [Project structure](#project-structure)
- [Architecture](#architecture)
- [Game data / configuration](#game-data--configuration)
- [Contributing](#contributing)
- [License](#license)

## Features

- Procedural dungeon generation, including animated liquid pools — water, mud, acid and lava; walkable, but mud/water slow you, acid burns, and lava is instant death.
- A variety of monsters, animated using CSS animations, with mouse-over descriptions.
- Sounds and music, plus a screen-shake effect on hits.
- Useable environment objects (doors, chests) and field-of-view/vision.
- Pick-up items with a lettered inventory: consumable potions and toggle-equippable gear (e.g. a ring of protection), data-driven from `Data/items.json`.
- Basic combat, driven by a Warhammer-inspired ruleset.
- A tileset renderer (using the Ultimate Fantasy Tileset) and a from-scratch ASCII renderer (old-school format, with colors), switchable client-side at any time - and auto-selected on load based on whether tileset assets are present.
- Almost everything (monster/hero stats, floor/wall sets, decorations, map generation weights) is data-driven via JSON, rather than hardcoded — see [Game data / configuration](#game-data--configuration).

## Screenshots

A partially explored sandy dungeon with a number of monsters chasing:

![BlazorRogue Screenshot 1](/img/BlazorRogue1.PNG)

A room with a bunch of chests:

![BlazorRogue Screenshot 2](/img/BlazorRogue2.PNG)

Chased by a skeleton into the arms of a goblin and his two pet black spiders:

![BlazorRogue Screenshot 3](/img/BlazorRogue3.PNG)

The same scene rendered in the ASCII renderer:

![BlazorRogue Screenshot 3 - in ASCII](/img/BlazorRogue3_ascii.PNG)

## Getting started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later.
- No database, no external services, no additional tooling required.

### Clone, build, run

```
git clone https://github.com/dontrolle/BlazorRogue.git
cd BlazorRogue
dotnet build
dotnet run
```

By default the app listens on `https://localhost:5001` (see `Properties/launchSettings.json`) — open either URL in a browser to play.

`BlazorRogue.Tests` is an xUnit test project covering core, UI-independent game logic (dice/combat math, `Configuration` JSON parsing, `Map` geometry helpers, and end-to-end dungeon generation smoke tests). Run it with:

```
dotnet test
```

There is no separate lint step — rely on `.editorconfig` conventions and the compiler's nullable-reference-type warnings (the build is currently warning-free; please keep it that way). CI runs `dotnet build` followed by `dotnet test` on every push/PR to `master` via GitHub Actions (`.github/workflows/build.yml`).

### Docker

```
docker build -t blazorrogue .
docker run -p 8080:8080 blazorrogue
```

Open `http://localhost:8080` in a browser. The image is a Linux container built via multi-stage
`dotnet publish` (see `docker/Dockerfile`) and intentionally excludes the proprietary tileset assets
(`wwwroot/img/uf_*`, gitignored and license-restricted — see [Tileset](#tileset)) via `.dockerignore`,
so the containerized game always runs in ASCII-renderer mode.

## How to play

| Action | Keys |
|---|---|
| Move / attack (8-directional) | Numpad, or `qweasdzxc` |
| Use (open door, chest, etc.) | `Shift` + move towards the object |
| Pick up item(s) on your tile | `g` |
| Open inventory | `i` — then `u` use/equip, `d` drop, `Esc` close |
| Quick use / equip an item | `u` — opens the inventory ready to use/equip |
| Start a new game | "New game" button (left panel) |
| Switch tileset/ASCII rendering | CTRL-A |
| Help overlay | ? |

## Tileset

This project employs the excellent [Ultimate Fantasy Tileset](https://www.oryxdesignlab.com/ultimatefantasy).

If you own the UF Tileset, put the subfolders of the `uf_split` folder from the tileset into the `wwwroot/img/` folder, and BlazorRogue will use it automatically. Without it, the game automatically falls back to the built-in ASCII renderer — no setup needed to get playing.

## Project structure

```
BlazorRogue.csproj / Program.cs   Minimal-hosting entry point, unified Blazor Components hosting
BlazorRogue.Tests/                xUnit test project for core game-logic classes (see below)
App.razor / Routes.razor          Root HTML shell + router
Pages/                            Blazor pages (GamePage.razor is the main game view)
Shared/                           Shared Razor components
GameObjects/                      GameObject and its subclasses (Moveable, Door, Chest, ...)
Components/                       Component base class, InventoryComponent, UseableComponent
Combat/                           Combat system, incl. the Warhammer-inspired ruleset (Combat/Warhammer/)
AI/                               Monster AI components
Effects/                          EffectsSystem (screen shake) and SoundManager (audio cues)
Vision/                           Field-of-view implementation
World/                            Map, Tile, Decoration and related types; World/Generation/ holds
                                   the map generators (IMapGenerator and implementors)
Rendering/                        AnimationCssGenerator (generates @keyframes CSS from
                                   monster/hero and liquid-pool animation data)
Entities/                         Type definitions parsed from configuration (MoveableType,
                                   LevelConfiguration, SettingsMap, etc.), plus Configuration.cs
                                   which parses Data/*.json into them
Sessions/                         Per-browser session state that survives page reloads
Utility/                          Small standalone helpers (e.g. string extension methods)
Data/                             JSON game data: monsters, heroes, floorsets, wallsets,
                                   liquidsets, decorations, items, levels
Game.cs / References.cs           Core game state (see Architecture below)
wwwroot/                          Static assets: CSS, JS interop, sounds, tileset images (gitignored)
docker/                           Dockerfile and Dockerfile.graphics (see Docker below)
```

## Architecture

One playthrough is rooted in a `Game`, which owns the map generator, `Map`, combat system, and `Configuration`. A `Game` is created and held by a `GameSession` for it to survive page reloads (each reload is a fresh Blazor circuit). `GameSessionStore` keeps sessions in memory, keyed by an id the browser holds in `localStorage`. Cross-cutting services (`Map`, `Configuration`, `SoundManager`, `EffectsSystem`) are reached through static holders in `References.cs`. `GameSession.Activate()` re-points them at the active game before any handler runs.

Everything placed on the map is a `GameObject` (`Moveable`, `Door`, `Chest`, …). Inspired by ECS `GameObject`'s have optional `Component`'s added at construction. The `Map` holds the `Tile` grid a map generator builds. `Vision/` does field-of-view. `GameObject`'s are rendered to `Decoration`'s. Rendering has a tileset path and an ASCII path, chosen client-side.

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the full picture — engine internals, the map-generation and rendering pipelines, and the gotchas worth knowing before a structural change.

## Game data / configuration

Most game content is data, not code — new monsters, heroes, floor/wall sets, and decorations can usually be added without touching C#:

- `Data/monsters.json`, `Data/heroes.json` — combat stats, AI behavior, sprites/animations.
- `Data/floorsets.json`, `Data/wallsets.json` — tileset mappings and map-generation weights.
- `Data/liquidsets.json` — animated liquid pools (water/mud/acid/lava): frames, ASCII colour, and hazard effect.
- `Data/decorations.json` — static decorative objects (torches, carpets, etc.).
- `Data/items.json` — pickup-able items: name, kind (`use_once` / `equipable`), sprite + ASCII glyph, and effect (`heal` / `armour_bonus`) with a magnitude.
- `Data/levels.json` — one entry per level: dimensions, which map generator to use (by string id), and that generator's own tuning parameters.

These are parsed in `Entities/Configuration.cs` via a `Parse*Type` method per entity kind — follow the existing pattern (and the `GetRequiredString`/`RequireNonNullString` helpers for required fields) when adding a new data-driven concept.

A level's `map_generator.parameters` in `levels.json` is different from the rest: It's parsed into a `SettingsMap` (`Entities/SettingsMap.cs`) — a small recursive value tree of int/double/string/nested-map, read back via typed getters (`GetInt`, `GetDouble`, `GetString`, `GetMap`), each with a required form and a `(key, defaultValue)` form. This keeps the JSON-parsing library out of the map generators entirely — see the `Map generation` and `Map-generator parameters` entries in [`CLAUDE.md`](CLAUDE.md) for the full design.

## Contributing

- `master` is protected: every change needs to go through a pull request; CI (`dotnet build` + `dotnet test`) must pass before merging.
- Please keep the build warning-free — nullable reference types are enabled project-wide.
- Add or update tests in `BlazorRogue.Tests` for changes to game logic (combat, configuration parsing, map/dungeon generation, etc.); for changes that are hard to unit test (rendering, Blazor components, JS interop), please describe how you manually verified the change (e.g. a screenshot or a description of in-browser testing) in your PR description.
- Small, focused PRs are preferred over large ones, especially for anything touching rendering or the hosting model — those are the areas most likely to have subtle runtime-only breakage that `dotnet build` won't catch.

## License

This project's code is licensed under the [MIT License](LICENSE). The Ultimate Fantasy Tileset assets referenced in [Tileset](#tileset) are © Oryx Design Lab and are **not** covered by this license.
