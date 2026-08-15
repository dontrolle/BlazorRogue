using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

public class AIComponentFactoryTests
{
    static Map NewMap()
    {
        var wallSet = new TileSet("test_wall", TileType.Wall, "test", [0]);
        return new Map(10, 10, wallSet, game: null!);
    }

    [Theory]
    [InlineData(SimpleAIComponent.ComponentId)]
    [InlineData(RandomWalkAIComponent.ComponentId)]
    public void IsKnownReturnsTrueForRegisteredComponentIds(string componentId) =>
        Assert.True(AIComponentFactory.IsKnown(componentId));

    [Fact]
    public void IsKnownReturnsFalseForUnregisteredComponentId() =>
        Assert.False(AIComponentFactory.IsKnown("not_a_real_ai_component"));

    [Fact]
    public void CreateThrowsForUnknownComponentId()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            AIComponentFactory.Create("not_a_real_ai_component", NewMap(), SettingsMap.Empty)
        );
        Assert.Contains("not_a_real_ai_component", ex.Message);
    }

    [Fact]
    public void CreateReturnsSimpleAIComponentForItsRegisteredId()
    {
        var component = AIComponentFactory.Create(
            SimpleAIComponent.ComponentId,
            NewMap(),
            SettingsMap.Empty
        );

        _ = Assert.IsType<SimpleAIComponent>(component);
    }

    [Fact]
    public void CreateReturnsRandomWalkAIComponentForItsRegisteredId()
    {
        var component = AIComponentFactory.Create(
            RandomWalkAIComponent.ComponentId,
            NewMap(),
            SettingsMap.Empty
        );

        _ = Assert.IsType<RandomWalkAIComponent>(component);
    }
}
