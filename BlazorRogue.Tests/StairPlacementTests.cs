using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests;

/// <summary>
/// Covers MapGeneratorBase.AddStairs: unlike chests (a percentage chance per tile), stairs must be
/// guaranteed - a level missing a down-stair would make the rest of the dungeon unreachable. Uses
/// a synthetic LevelConfiguration for the level number under test, same as
/// DungeonGeneratorBaseTests, while relying on the real Data/levels.json (levels 0, 1, 2) for
/// Configuration.Levels, which AddStairs checks against.
/// </summary>
public class StairPlacementTests
{
    static LevelConfiguration LevelWith(int number) =>
        new(
            number: number,
            id: $"test-level-{number}",
            name: "Test Level",
            height: 40,
            width: 40,
            generatorId: BasicDungeonGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: SettingsMap.Empty
        );

    [Fact]
    public void LevelZeroGetsOnlyADownStair()
    {
        var game = new Game();
        var map = MapGeneratorFactory.Create(LevelWith(0), game).GenerateMap();

        var stairs = map.GameObjects.OfType<Stair>().ToList();

        var stair = Assert.Single(stairs);
        Assert.Equal(StairDirection.Down, stair.Direction);
    }

    [Fact]
    public void MiddleLevelGetsBothADownAndAnUpStairOnDifferentTiles()
    {
        var game = new Game();
        var map = MapGeneratorFactory.Create(LevelWith(1), game).GenerateMap();

        var stairs = map.GameObjects.OfType<Stair>().ToList();

        Assert.Equal(2, stairs.Count);
        Assert.Contains(stairs, s => s.Direction == StairDirection.Down);
        Assert.Contains(stairs, s => s.Direction == StairDirection.Up);
        Assert.NotEqual((stairs[0].X, stairs[0].Y), (stairs[1].X, stairs[1].Y));
    }

    [Fact]
    public void HighestConfiguredLevelGetsOnlyAnUpStair()
    {
        var game = new Game();
        var map = MapGeneratorFactory.Create(LevelWith(2), game).GenerateMap();

        var stairs = map.GameObjects.OfType<Stair>().ToList();

        var stair = Assert.Single(stairs);
        Assert.Equal(StairDirection.Up, stair.Direction);
    }

    [Fact]
    public void LevelWithNoNeighboursGetsNoStairs()
    {
        var game = new Game();
        var map = MapGeneratorFactory.Create(LevelWith(100), game).GenerateMap();

        Assert.Empty(map.GameObjects.OfType<Stair>());
    }
}
