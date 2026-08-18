using System;
using System.Linq;
using BlazorRogue.GameObjects;

namespace BlazorRogue.Tests;

public class StairTests
{
    static readonly string[] GroundGrassDownStairOptions =
    [
        "door_trap_closed_brown",
        "door_trap_closed_tan",
    ];

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
        var groundDirtBrown = game.Configuration.FloorSetById("ground_dirt_brown");
        Assert.Null(groundDirtBrown.StairImages); // guards the premise of this test
        game.Map.Tiles[x, y].TileSet = groundDirtBrown;
        game.Map.Decorations[x, y].Clear();

        var stair = new Stair(x, y, StairDirection.Up);
        stair.Render(game.Map);

        var decoration = Assert.Single(game.Map.Decorations[x, y]);
        var (upOptions, _) = game.Configuration.DefaultStairsFloorSet.StairImages!.Value;
        Assert.Contains(decoration.ImageName, upOptions.Select(o => o.Name));
    }

    [Fact]
    public void RenderPicksAmongMultipleWeightedOptionsForADirection()
    {
        var game = new Game();
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        game.Map.Tiles[x, y].TileSet = game.Configuration.FloorSetById("ground_grass");
        game.Map.Decorations[x, y].Clear();

        var stair = new Stair(x, y, StairDirection.Down);
        stair.Render(game.Map);

        var decoration = Assert.Single(game.Map.Decorations[x, y]);
        Assert.Contains(decoration.ImageName, GroundGrassDownStairOptions);
    }

    [Fact]
    public void RenderReusesTheSamePickedImageAcrossRepeatedRenderCalls()
    {
        var game = new Game();
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        game.Map.Tiles[x, y].TileSet = game.Configuration.FloorSetById("ground_grass");
        game.Map.Decorations[x, y].Clear();

        var stair = new Stair(x, y, StairDirection.Down);
        stair.Render(game.Map);
        stair.Render(game.Map);

        Assert.Equal(2, game.Map.Decorations[x, y].Count);
        var imageNames = game.Map.Decorations[x, y].Select(d => d.ImageName).Distinct();
        Assert.Single(imageNames);
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
