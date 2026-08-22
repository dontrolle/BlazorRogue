using System;
using System.Linq;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

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
        var groundDirtBrown = game.Configuration.FloorSetById("extra_2");
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
    public void UseDescendingIncrementsLevelNumberAndPreservesThePlayer()
    {
        var game = new Game();
        var originalMap = game.Map;
        var player = game.Map.Player;
        player.InventoryComponent!.Gold = 7;

        var stair = new Stair(player.X, player.Y, StairDirection.Down);

        Stair.Use(stair);

        Assert.Equal(1, game.CurrentLevelNumber);
        // Level 1 hasn't been visited before, so this is a fresh generation, not a cache hit.
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
    public void RevisitingALevelReturnsTheSameMapInstanceInsteadOfRegenerating()
    {
        var game = new Game();
        var levelZeroMap = game.Map;

        game.TransitionToLevel(StairDirection.Down);
        var levelOneMap = game.Map;
        Assert.NotSame(levelZeroMap, levelOneMap); // level 1's first visit - freshly generated

        game.TransitionToLevel(StairDirection.Up);
        Assert.Same(levelZeroMap, game.Map); // level 0 revisited - restored from cache

        game.TransitionToLevel(StairDirection.Down);
        Assert.Same(levelOneMap, game.Map); // level 1 revisited - restored from cache
    }

    [Fact]
    public void RevisitingALevelPlacesThePlayerOnTheStairLeadingBackTheWayTheyCame()
    {
        var game = new Game();
        var downStairOnLevelZero = game.Map.GetStair(StairDirection.Down);

        game.TransitionToLevel(StairDirection.Down); // level 1, freshly generated
        var upStairOnLevelOne = game.Map.GetStair(StairDirection.Up);

        game.TransitionToLevel(StairDirection.Up); // level 0, restored from cache
        Assert.Equal(downStairOnLevelZero.X, game.Map.Player.X);
        Assert.Equal(downStairOnLevelZero.Y, game.Map.Player.Y);

        game.TransitionToLevel(StairDirection.Down); // level 1, restored from cache
        Assert.Equal(upStairOnLevelOne.X, game.Map.Player.X);
        Assert.Equal(upStairOnLevelOne.Y, game.Map.Player.Y);
    }

    [Fact]
    public void LeavingALevelRemovesThePlayerFromItsMoveablesListUntilItIsRevisited()
    {
        var game = new Game();
        var levelZeroMap = game.Map;
        var player = game.Map.Player;

        game.TransitionToLevel(StairDirection.Down);

        // Otherwise the player would be double-booked: still listed on the now-inactive level 0
        // map (which is cached, not discarded) as well as on the new current map.
        Assert.DoesNotContain(player, levelZeroMap.Moveables);
        Assert.Contains(player, game.Map.Moveables);

        game.TransitionToLevel(StairDirection.Up);

        Assert.Same(levelZeroMap, game.Map);
        Assert.Equal(1, game.Map.Moveables.Count(m => ReferenceEquals(m, player)));
    }

    [Fact]
    public void RevisitingALevelKeepsPreviouslyKilledMonstersDead()
    {
        var game = new Game();
        var levelZeroMap = game.Map;
        var monster = game.Map.Monsters.First();
        int monsterCountBeforeKill = game.Map.Monsters.Count();

        monster.CombatComponent!.ApplyDamage(1000);
        Assert.DoesNotContain(monster, game.Map.Monsters);

        game.TransitionToLevel(StairDirection.Down);
        game.TransitionToLevel(StairDirection.Up);

        Assert.Same(levelZeroMap, game.Map); // confirms this is the same cached map, not a new one
        Assert.DoesNotContain(monster, game.Map.Monsters);
        Assert.Equal(monsterCountBeforeKill - 1, game.Map.Monsters.Count());
    }

    [Fact]
    public void RevisitingALevelPreservesGameObjectStateChanges()
    {
        var game = new Game();
        var (x, y) = FindFreeTile(game.Map);
        var door = new Door(x, y, "wood", 1, Orientation.Horizontal, isOpen: false);
        game.Map.AddGameObject(door);

        Door.Use(door);
        Assert.True(door.IsOpen);

        game.TransitionToLevel(StairDirection.Down);
        game.TransitionToLevel(StairDirection.Up);

        Assert.Same(door, game.Map.GameObjectByCoord[x, y].OfType<Door>().Single());
        Assert.True(door.IsOpen);
    }

    [Fact]
    public void RevisitingALevelNeverForgetsPreviouslyExploredTiles()
    {
        var game = new Game();
        var mappedBeforeLeaving = (bool[,])game.Map.IsMappedMap.Clone();

        game.TransitionToLevel(StairDirection.Down);
        game.TransitionToLevel(StairDirection.Up);

        // Fog of war is only ever revealed, never reset - the player reattaches on level 0's
        // down-stair rather than the original spawn point, so it's expected (and fine) for the
        // revisit to additionally reveal tiles around the stair that weren't mapped before; what
        // must not happen is losing anything that was already explored.
        game.Map.ForEachTile(
            (x, y) =>
            {
                if (mappedBeforeLeaving[x, y])
                {
                    Assert.True(game.Map.IsMappedMap[x, y]);
                }
            }
        );
    }

    static (int X, int Y) FindFreeTile(BlazorRogue.World.Map map)
    {
        int x = -1;
        int y = -1;
        map.ForEachTile(
            (px, py) =>
            {
                if (
                    x == -1
                    && !map.IsBlocked(px, py)
                    && !map.Moveables.Any(m => m.X == px && m.Y == py)
                )
                {
                    x = px;
                    y = py;
                }
            }
        );

        if (x == -1)
        {
            throw new InvalidOperationException("No free tile found on the generated map.");
        }

        return (x, y);
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
