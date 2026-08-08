using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

public class DoorTests
{
    [Fact]
    public void UseDoesNotToggleWhileAMonsterStandsOnTheDoorsTile()
    {
        var game = new Game();
        var monster = game.Map.Monsters.First();
        var door = new Door(monster.X, monster.Y, "wood", 1, Orientation.Horizontal, isOpen: true);
        game.Map.AddGameObject(door);

        Door.Use(door);

        Assert.True(door.IsOpen);
    }

    [Fact]
    public void UseDoesNotToggleWhileThePlayerStandsOnTheDoorsTile()
    {
        var game = new Game();
        var player = game.Map.Player;
        var door = new Door(player.X, player.Y, "wood", 1, Orientation.Horizontal, isOpen: true);
        game.Map.AddGameObject(door);

        Door.Use(door);

        Assert.True(door.IsOpen);
    }

    [Fact]
    public void UseTogglesNormallyWhenNoMoveableStandsOnTheDoorsTile()
    {
        var game = new Game();
        (int x, int y) = FindTileWithNoMoveable(game.Map);
        var door = new Door(x, y, "wood", 1, Orientation.Horizontal, isOpen: true);
        game.Map.AddGameObject(door);

        Door.Use(door);

        Assert.False(door.IsOpen);
    }

    static (int x, int y) FindTileWithNoMoveable(Map map)
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (!map.Moveables.Any(m => m.X == x && m.Y == y))
                {
                    return (x, y);
                }
            }
        }

        throw new InvalidOperationException("Every tile has a moveable on it.");
    }
}
