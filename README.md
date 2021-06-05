# BlazorRogue

[![Build Status](https://dev.azure.com/tedconsultingdk/BlazorRogueAz/_apis/build/status/dontrolle.BlazorRogue?branchName=master)](https://dev.azure.com/tedconsultingdk/BlazorRogueAz/_build/latest?definitionId=1&branchName=master)

A little rogue-like built from the bottom-up in a little rogue-like game engine I built on C#/Blazor using the beautiful [Ultimate Fantasy Tileset tiles from Oryx](https://www.oryxdesignlab.com/ultimatefantasy).

### Features

* Dungeon generation, a variety of monsters (animated using CSS-animations), mouse-over descriptions, sounds and music, screen-shake effect, useable environment, field of vision, basic combat (should be balanced at some point), ...

* Engine supports json-configuration for most visuals, audio and attributes for entities, as well as weights and config for map generation.

* Supports tileset renderer that employs using the UF Tileset.

![BlazorRogue Screenshot 1](/img/BlazorRogue1.PNG)

![BlazorRogue Screenshot 2](/img/BlazorRogue2.PNG)

![BlazorRogue Screenshot 3](/img/BlazorRogue3.PNG)

* As well as an ASCII renderer in that old school format (though with colors).

![BlazorRogue Screenshot 3 - in ASCII](/img/BlazorRogue3_ascii.PNG)

Allows fast (client-side) switching between graphical and ASCII rendering.

## Building

Follow the instructions on [Get started with ASP.NET Core Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor/get-started) to get a working build environment for Blazor for either VS code or Visual Studio. 

(This very project started with  a `dotnet new blazorserverside -o WebApplication1`, as can probably still be seen here and there.)

## Tileset

This project employs the excellent [Ultimate Fantasy Tileset](https://www.oryxdesignlab.com/ultimatefantasy). I own the tileset and have the commercial license, but the source tiles have been removed from this repo, as it is part of the license-agreement that they should be protected from copying.

If you own the UF Tileset, you can put the subfolders of the `uf_split` folder in the `wwwroot/img/`-folder, and BlazorRogue will be able employ the tileset.

In some future, I might make an effort to make a release where the tileset is suitable protected. Don't expect it, as this was a hobby-project to play around with Blazor and roguelikes. ;)
