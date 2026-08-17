using System;
using System.Collections.Generic;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

/// <summary>
/// Covers DungeonGeneratorBase.SelectWallSet (invoked indirectly through BasicDungeonGenerator's
/// and CaveGenerator's base-constructor arguments) resolving a level's wall tile-set from the
/// data-driven "common.wall_tile_set" weights in levels.json, rather than always picking uniformly
/// across every wall-set of the generator's level type.
/// </summary>
public class DungeonGeneratorBaseTests
{
    static readonly string[] DungeonAndCryptIds = ["dungeon", "crypt"];

    static SettingsMap SettingsWithWallTileSet(params (string Id, double Weight)[] weights) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["wall_tile_set"] = new List<(string, double)>(weights),
                    }
                ),
            }
        );

    static LevelConfiguration LevelWith(string generatorId, SettingsMap settingsMap) =>
        new(
            number: 0,
            id: "test-level",
            name: "Test Level",
            height: 40,
            width: 40,
            generatorId: generatorId,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    [Fact]
    public void GeneratedMapUsesTheSingleWallTileSetIdSpecifiedInLevelSettings()
    {
        var game = new Game();
        var level = LevelWith(BasicDungeonGenerator.Id, SettingsWithWallTileSet(("dungeon", 1.0)));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Equal("dungeon", map.DungeonWallSet.Id);
    }

    [Fact]
    public void GeneratedMapOnlyEverUsesWallTileSetIdsListedInLevelSettings()
    {
        // Weighted-random pick is non-deterministic, so run it repeatedly and assert the result is
        // always one of the two listed ids, never e.g. "ruins" which isn't listed here even though
        // it's a valid dungeon wall-set.
        var game = new Game();
        var level = LevelWith(
            BasicDungeonGenerator.Id,
            SettingsWithWallTileSet(("dungeon", 1.0), ("crypt", 1.0))
        );

        for (int i = 0; i < 20; i++)
        {
            var map = MapGeneratorFactory.Create(level, game).GenerateMap();
            Assert.Contains(map.DungeonWallSet.Id, DungeonAndCryptIds);
        }
    }

    [Fact]
    public void GeneratedMapFallsBackToDefaultPoolWhenWallTileSetIsUnspecified()
    {
        // SettingsMap.Empty must still resolve to some valid wall-set from the generator's whole
        // level-type pool rather than throwing.
        var game = new Game();
        var level = LevelWith(BasicDungeonGenerator.Id, SettingsMap.Empty);

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Contains(map.DungeonWallSet, game.Configuration.DungeonWallSets);
    }

    [Fact]
    public void CaveGeneratorGeneratesMapWithOutdoorHedgeWallSet()
    {
        // Mirrors levels.json's level0: cave_generator with the "hedge" wall-set, whose
        // level_type is "outdoor" rather than "cave" - and which relies on wallsets.json's data-
        // driven "edges" entry (rather than CaveGenerator hardcoding cave-specific indexes) to
        // decorate wall tiles via WallEdge. This exercises that whole path end-to-end without
        // throwing.
        var game = new Game();
        var level = LevelWith(CaveGenerator.Id, SettingsWithWallTileSet(("hedge", 1.0)));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Equal("hedge", map.DungeonWallSet.Id);
    }

    [Fact]
    public void CaveGeneratorThrowsWhenWallSetHasNoEdgeIndexes()
    {
        // "crypt" never defines an "edges" entry in wallsets.json (only the dungeon generator
        // uses it, which never reads edge data) - CaveGenerator should fail clearly rather than
        // throwing a raw NullReferenceException if it's ever pointed at such a wall-set.
        var game = new Game();
        var level = LevelWith(CaveGenerator.Id, SettingsWithWallTileSet(("crypt", 1.0)));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            MapGeneratorFactory.Create(level, game).GenerateMap()
        );
        Assert.Contains("crypt", ex.Message);
    }
}
