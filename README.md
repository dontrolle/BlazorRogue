# BlazorRogue

[![Build Status](https://dev.azure.com/tedconsultingdk/BlazorRogueAz/_apis/build/status/dontrolle.BlazorRogue?branchName=master)](https://dev.azure.com/tedconsultingdk/BlazorRogueAz/_build/latest?definitionId=1&branchName=master)

A simple little rogue-like built from the bottom-up in C#/Blazor using the beautiful [Ultimate Fantasy Tileset tiles from Oryx](https://www.oryxdesignlab.com/ultimatefantasy).

## Building

Follow the instructions on [Get started with ASP.NET Core Blazor](https://docs.microsoft.com/en-us/aspnet/core/blazor/get-started) to get a working build environment for Blazor for either VS code or Visual Studio. (This very project started with  a `dotnet new blazorserverside -o WebApplication1`, as can probably still be seen here and there.)

## Tileset

This project employs the excellent [Ultimate Fantasy Tileset](https://www.oryxdesignlab.com/ultimatefantasy). I own the tileset and have the commercial license, but the source tiles have been removed from this repo, as it is part of the license-agreement that they should be protected from copying.

If you own the UF Tileset, you can put the subfolders of the `uf_split` folder in the `wwwroot/img/`-folder, and BlazorRogue will be able employ the tileset.

In some future, I might make an effort to make a release where the tileset is suitable protected. Don't expect it, as this was a hobby-project to play around with Blazor and roguelikes. ;)
