using System;
using System.Linq;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Tests;

public class StairTests
{
    [Fact]
    public void UseDescendingIncrementsLevelNumberAndRegeneratesMapWhilePreservingThePlayer()
    {
        var game = new Game();
        var originalMap = game.Map;
        var player = game.Map.Player;
        player.InventoryComponent!.Gold = 7;

        var stair = new Stair(player.X, player.Y, StairDirection.Down);

        Stair.Use(stair);

        Assert.Equal(1, game.CurrentLevelNumber);
        Assert.NotSame(originalMap, game.Map);
        Assert.Same(player, game.Map.Player);
        Assert.Equal(7, game.Map.Player.InventoryComponent!.Gold);
    }

    [Fact]
    public void UseAscendingDecrementsLevelNumber()
    {
        var game = new Game();
        game.TransitionToLevel(StairDirection.Down); // now on level 1
        var player = game.Map.Player;

        var stair = new Stair(player.X, player.Y, StairDirection.Up);

        Stair.Use(stair);

        Assert.Equal(0, game.CurrentLevelNumber);
        Assert.Same(player, game.Map.Player);
    }

    [Fact]
    public void UseAddsATransitionMessage()
    {
        var game = new Game();
        var player = game.Map.Player;
        var stair = new Stair(player.X, player.Y, StairDirection.Down);

        Stair.Use(stair);

        Assert.Contains(
            game.Messages,
            m => m.Contains("descend", StringComparison.OrdinalIgnoreCase)
        );
    }

    [Fact]
    public void UseThrowsWhenCalledWithAGameObjectThatIsNotAStair()
    {
        var game = new Game();
        var notAStair = game.Map.Monsters.First();

        Assert.Throws<InvalidOperationException>(() => Stair.Use(notAStair));
    }
}
