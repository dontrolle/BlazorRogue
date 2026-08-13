using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

public class MapGeneratorFactoryTests
{
    static LevelConfiguration LevelWithGeneratorId(string generatorId) =>
        new(
            number: 0,
            id: "test-level",
            name: "Test Level",
            height: 10,
            width: 10,
            generatorId: generatorId,
            settingsMap: SettingsMap.Empty
        );

    [Theory]
    [InlineData(BasicDungeonGenerator.Id)]
    [InlineData(CaveGenerator.Id)]
    public void IsKnownReturnsTrueForRegisteredGeneratorIds(string generatorId) =>
        Assert.True(MapGeneratorFactory.IsKnown(generatorId));

    [Fact]
    public void IsKnownReturnsFalseForUnregisteredGeneratorId() =>
        Assert.False(MapGeneratorFactory.IsKnown("not_a_real_generator"));

    [Fact]
    public void CreateThrowsForUnknownGeneratorId()
    {
        var level = LevelWithGeneratorId("not_a_real_generator");

        // The unknown-id branch never dereferences `game`, so passing null! keeps this test
        // independent of Configuration/dungeon generation entirely.
        var ex = Assert.Throws<InvalidOperationException>(() =>
            MapGeneratorFactory.Create(level, null!)
        );
        Assert.Contains("not_a_real_generator", ex.Message);
        Assert.Contains(level.Id, ex.Message);
    }

    [Fact]
    public void CreateReturnsBasicDungeonGeneratorForItsRegisteredId()
    {
        var game = new Game();
        var level = LevelWithGeneratorId(BasicDungeonGenerator.Id);

        var generator = MapGeneratorFactory.Create(level, game);

        _ = Assert.IsType<BasicDungeonGenerator>(generator);
    }

    [Fact]
    public void CreateReturnsCaveGeneratorForItsRegisteredId()
    {
        var game = new Game();
        var level = LevelWithGeneratorId(CaveGenerator.Id);

        var generator = MapGeneratorFactory.Create(level, game);

        _ = Assert.IsType<CaveGenerator>(generator);
    }
}
