using System;
using System.Linq;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Tests;

public class StairTests
{
    [Fact]
    public void RenderUsesTheStairsOwnFloorSetImageWhenItDefinesStairImages()
    {
        var game = new Game();
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        game.Map.Tiles[x, y].TileSet = game.Configuration.FloorSetById("blue");
        game.Map.Decorations[x, y].Clear();

        var stair = new Stair(x, y, StairDirection.Down);
        stair.Render(game.Map);

        var decoration = Assert.Single(game.Map.Decorations[x, y]);
        Assert.Equal("floor_set_blue_9", decoration.ImageName);
    }

    [Fact]
    public void RenderFallsBackToDefaultStairsFloorSetWhenTheTilesFloorSetHasNoStairImages()
    {
        var game = new Game();
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        var groundGrass = game.Configuration.FloorSetById("ground_grass");
        Assert.Null(groundGrass.StairImageIndexes); // guards the premise of this test
        game.Map.Tiles[x, y].TileSet = groundGrass;
        game.Map.Decorations[x, y].Clear();

        var stair = new Stair(x, y, StairDirection.Up);
        stair.Render(game.Map);

        var decoration = Assert.Single(game.Map.Decorations[x, y]);
        Assert.Equal(
            game.Configuration.DefaultStairsFloorSet.ImageName(
                game.Configuration.DefaultStairsFloorSet.StairImageIndexes!.Value.Up
            ),
            decoration.ImageName
        );
    }

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
