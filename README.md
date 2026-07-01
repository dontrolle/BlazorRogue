# BlazorRogue

[![Build](https://github.com/dontrolle/BlazorRogue/actions/workflows/build.yml/badge.svg)](https://github.com/dontrolle/BlazorRogue/actions/workflows/build.yml)
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

- Procedural dungeon generation.
- A variety of monsters, animated using CSS animations, with mouse-over descriptions.
- Sounds and music, plus a screen-shake effect on hits.
- Useable environment objects (doors, chests) and field-of-view/vision.
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

By default the app listens on `https://localhost:5001` and `http://localhost:5000` (see `Properties/launchSettings.json`) — open either URL in a browser to play. There's no database or seed step; a fresh dungeon is generated on every page load.

There is no test project in this repository (`dotnet test` has nothing to run) and no separate lint step — rely on `.editorconfig` conventions and the compiler's nullable-reference-type warnings (the build is currently warning-free; please keep it that way). CI runs `dotnet build` on every push/PR to `master` via GitHub Actions (`.github/workflows/build.yml`).

(This very project started with a `dotnet new blazorserverside -o WebApplication1`, as can probably still be seen here and there.)

## How to play

| Action | Keys |
|---|---|
| Move / attack (8-directional) | Numpad, or `qweasdzxc` |
| Use (open door, chest, etc.) | `Shift` + move towards the object |
| Set keyboard focus on the map | Click the map |
| Start a new game | Refresh the page |
| Switch tileset/ASCII rendering | "Switch mode" button (left panel) |

## Tileset

This project employs the excellent [Ultimate Fantasy Tileset](https://www.oryxdesignlab.com/ultimatefantasy). The author owns the tileset under a commercial license, but the tileset graphics used by this project have been removed from this repo (and are excluded via `.gitignore`), since the license agreement requires them to be protected from copying.

If you own the UF Tileset, put the subfolders of the `uf_split` folder from the tileset into the `wwwroot/img/` folder, and BlazorRogue will use it automatically. Without it, the game automatically falls back to the built-in ASCII renderer — no setup needed to get playing.

## Project structure

```
BlazorRogue.csproj / Program.cs   Minimal-hosting entry point, unified Blazor Components hosting
App.razor / Routes.razor          Root HTML shell + router
Pages/                            Blazor pages (Indoor.razor is the main game view)
Shared/                           Shared Razor components
GameObjects/                      GameObject and its subclasses (Moveable, Door, Chest, ...)
Combat/                           Combat system, incl. the Warhammer-inspired ruleset (Combat/Warhammer/)
AI/                               Monster AI components
Vision/                           Field-of-view implementation
Entities/                         Type definitions parsed from configuration (MoveableType, etc.)
Data/                             JSON game data: monsters, heroes, floorsets, wallsets, decorations
Configuration.cs                  Parses Data/*.json into the strongly-typed Entities
Game.cs / Map.cs / References.cs  Core game/session state (see Architecture below)
wwwroot/                          Static assets: CSS, JS interop, sounds, tileset images (gitignored)
```

## Architecture

- **`Game`** (`Game.cs`) is the root object built once per game session. It owns the `DungeonGenerator`, `Map`, `FightingSystem`, `Configuration`, and `EffectsSystem`.
- **`References`** (`References.cs`) is a static service-locator-style holder for the current `Map`, `Configuration`, `SoundManager`, and `EffectsSystem`, set up during `Game`'s constructor. Code throughout the engine (e.g. `GameObject.Kill()`) reaches these statics directly rather than receiving them via DI/constructor injection.
- **`Configuration`** (`Configuration.cs`) parses all game data from JSON files under `Data/` into strongly-typed dictionaries (`MoveableType`, `StaticDecorativeObjectType`, `TileSet`). Nearly all visual/audio/combat-stat tuning is data-driven through these files rather than hardcoded.
- **Entity/component model**: `GameObject` (`GameObjects/GameObject.cs`) is the abstract base for everything placed on the map (`Moveable`, `Door`, `Chest`, `Torch`, `HalfWall`, `CaveEdge`, `StaticDecorativeObject`). Behavior is composed via optional `Component` subclasses (`AIComponent`, `CombatComponent`, `UseableComponent`, `InventoryComponent`) attached at construction — a `Component` always knows its `Owner` via `SetOwner`.
- **Map & rendering**: `Map.cs` holds the `Tile` grid; `DungeonGenerator.cs` procedurally builds it. `Vision/` implements field-of-view (the Adam Milne visibility algorithm). Rendering is split between a tileset path and an ASCII path — `GameObject.Render(Map map)` is the per-object hook, and `Pages/Indoor.razor` is the Blazor page that renders the grid, switching between tileset and ASCII based on the `renderAscii` flag.
- **Combat**: lives under `Combat/`, with a specific ruleset in `Combat/Warhammer/` (`FightingSystem`, `Dice`) — combat stats (weapon skill, damage, toughness, armour, wounds) are parsed from the same `Configuration` JSON files.
- **Hosting**: `Program.cs` uses the minimal hosting API plus the unified Blazor Components model (`AddRazorComponents().AddInteractiveServerComponents()` / `MapRazorComponents<App>().AddInteractiveServerRenderMode()`). `App.razor` is the root HTML shell (`<HeadOutlet>` + `<Routes>`), and `Routes.razor` holds the `<Router>`.

A more detailed, AI-agent-oriented version of this section (including gotchas like the `~/`-style Tag Helper URL resolution not working inside `.razor` components) lives in [`.github/copilot-instructions.md`](.github/copilot-instructions.md) — worth a read before making structural changes.

## Game data / configuration

Most game content is data, not code — new monsters, heroes, floor/wall sets, and decorations can usually be added without touching C#:

- `Data/monsters.json`, `Data/heroes.json` — combat stats, AI behavior, sprites/animations.
- `Data/floorsets.json`, `Data/wallsets.json` — tileset mappings and map-generation weights.
- `Data/decorations.json` — static decorative objects (torches, carpets, etc.).

These are parsed in `Configuration.cs` via a `Parse*Type` method per entity kind — follow the existing pattern (and the `GetRequiredString`/`RequireNonNullString` helpers for required fields) when adding a new data-driven concept.

## Contributing

- `master` is protected: everyone (including the maintainer) needs to go through a pull request; CI (`dotnet build`) must pass before merging.
- Please keep the build warning-free — nullable reference types are enabled project-wide.
- Since there's no test suite yet, please describe how you manually verified a change (e.g. build + a screenshot or a description of in-browser testing) in your PR description.
- Small, focused PRs are preferred over large ones, especially for anything touching rendering or the hosting model — those are the areas most likely to have subtle runtime-only breakage that `dotnet build` won't catch.

## License

This project's code is licensed under the [MIT License](LICENSE). The Ultimate Fantasy Tileset assets referenced in [Tileset](#tileset) are © Oryx Design Lab and are **not** covered by this license — they are proprietary, excluded from this repo, and require their own separate license to use.
