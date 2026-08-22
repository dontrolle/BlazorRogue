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
    static readonly string[] GreyAndDarkFloorSetIds = ["grey", "dark"];

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

    static SettingsMap SettingsWithWallAndFloorTileSet(
        (string Id, double Weight)[] wallWeights,
        (string Id, double Weight)[] commonFloorWeights,
        (string Id, double Weight)[]? specialFloorWeights = null
    ) =>
        new(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["wall_tile_set"] = new List<(string, double)>(wallWeights),
                        ["floor_tile_set"] = new SettingsMap(
                            new Dictionary<string, object>
                            {
                                ["common"] = new List<(string, double)>(commonFloorWeights),
                                ["special"] = new List<(string, double)>(specialFloorWeights ?? []),
                            }
                        ),
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
        // driven "edges" entry (read by Tile.Render, opportunistically for any wall-set/generator
        // combination that defines it) to decorate wall tiles. This exercises that whole path
        // end-to-end without throwing.
        var game = new Game();
        var level = LevelWith(CaveGenerator.Id, SettingsWithWallTileSet(("hedge", 1.0)));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Equal("hedge", map.DungeonWallSet.Id);
    }

    static IEnumerable<TileSet> FloorTileSetsUsedIn(Map map)
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Tiles[x, y].TileType == TileType.Floor)
                {
                    yield return map.Tiles[x, y].TileSet;
                }
            }
        }
    }

    [Fact]
    public void GeneratedMapOnlyEverUsesFloorTileSetIdsListedInCommonPool()
    {
        // Mirrors GeneratedMapOnlyEverUsesWallTileSetIdsListedInLevelSettings, but for
        // "common.floor_tile_set.common". percentage_chance_of_special_room is forced to 0 so no
        // room can pull from the (here empty) "special" pool instead.
        var game = new Game();
        var settings = new SettingsMap(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["wall_tile_set"] = new List<(string, double)> { ("dungeon", 1.0) },
                        ["floor_tile_set"] = new SettingsMap(
                            new Dictionary<string, object>
                            {
                                ["common"] = new List<(string, double)>
                                {
                                    ("grey", 1.0),
                                    ("dark", 1.0),
                                },
                            }
                        ),
                    }
                ),
                ["layout"] = new SettingsMap(
                    new Dictionary<string, object> { ["percentage_chance_of_special_room"] = 0.0 }
                ),
            }
        );
        var level = LevelWith(BasicDungeonGenerator.Id, settings);

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.All(
            FloorTileSetsUsedIn(map),
            floorSet => Assert.Contains(floorSet.Id, GreyAndDarkFloorSetIds)
        );
    }

    [Fact]
    public void GeneratedMapFallsBackToAllFloorSetsWhenFloorTileSetIsUnspecified()
    {
        // SettingsMap.Empty must still resolve to some valid floor-set from every known floorset
        // rather than throwing.
        var game = new Game();
        var level = LevelWith(BasicDungeonGenerator.Id, SettingsMap.Empty);

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var knownFloorSetIds = game.Configuration.FloorSets.Select(t => t.Id).ToHashSet();
        Assert.All(
            FloorTileSetsUsedIn(map),
            floorSet => Assert.Contains(floorSet.Id, knownFloorSetIds)
        );
    }

    [Fact]
    public void CaveGeneratorUsesTheSingleFloorTileSetIdSpecifiedInCommonPool()
    {
        var game = new Game();
        var level = LevelWith(
            CaveGenerator.Id,
            SettingsWithWallAndFloorTileSet(
                wallWeights: [("cave", 1.0)],
                commonFloorWeights: [("crusted_grey", 1.0)]
            )
        );

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.All(FloorTileSetsUsedIn(map), floorSet => Assert.Equal("crusted_grey", floorSet.Id));
    }
}
