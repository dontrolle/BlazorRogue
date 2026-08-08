using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

public class MapTests
{
    static Map CreateMap(int width = 10, int height = 10)
    {
        var wallSet = new TileSet("test_wall", TileType.Wall, "test", [0]);
        return new Map(width, height, wallSet, game: null!);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 4, 5)]
    [InlineData(6, 8, 10)]
    public void GetDistanceComputesEuclideanDistanceTruncated(int dx, int dy, int expected) =>
        Assert.Equal(expected, Map.GetDistance(dx, dy));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 4, 25)]
    [InlineData(-3, 4, 25)]
    public void GetDistanceSquaredComputesSquaredDistance(int dx, int dy, int expected) =>
        Assert.Equal(expected, Map.GetDistanceSquared(dx, dy));

    [Fact]
    public void ForEachTileVisitsEveryTileExactlyOnce()
    {
        var map = CreateMap(width: 4, height: 3);
        var visited = new List<(int x, int y)>();

        map.ForEachTile((x, y) => visited.Add((x, y)));

        Assert.Equal(12, visited.Count);
        Assert.Equal(12, visited.Distinct().Count());
        Assert.Contains((0, 0), visited);
        Assert.Contains((3, 2), visited);
    }

    [Fact]
    public void ForEachTileClipsBoundsToMapDimensions()
    {
        var map = CreateMap(width: 5, height: 5);
        var visited = new List<(int x, int y)>();

        // Requesting a much larger area than the map should silently clip, not throw.
        map.ForEachTile((x, y) => visited.Add((x, y)), xMin: -10, xMax: 100, yMin: -10, yMax: 100);

        Assert.Equal(25, visited.Count);
    }

    [Fact]
    public void BlocksLightReturnsTrueOutsideMapBounds()
    {
        var map = CreateMap(width: 5, height: 5);

        Assert.True(map.BlocksLight(-1, 0));
        Assert.True(map.BlocksLight(0, -1));
        Assert.True(map.BlocksLight(5, 0));
        Assert.True(map.BlocksLight(0, 5));
    }

    [Fact]
    public void SetVisibleMarksTileAsVisibleAndMapped()
    {
        var map = CreateMap();

        map.SetVisible(2, 3);

        Assert.True(map.IsVisibleMap[2, 3]);
        Assert.True(map.IsMappedMap[2, 3]);
    }

    [Fact]
    public void SetVisibleOutOfBoundsIsANoOp()
    {
        var map = CreateMap(width: 5, height: 5);

        // Should not throw despite being out of bounds.
        map.SetVisible(-1, 0);
        map.SetVisible(0, 10);
    }

    [Fact]
    public void IsBlockedBeforePostGenInitializeReflectsTileBlockingState()
    {
        var map = CreateMap();

        // Tiles start out as blocking "dark" placeholder tiles until dungeon generation carves them out.
        Assert.True(map.IsBlocked(2, 2));

        map.Tiles[2, 2].Blocking = false;

        Assert.False(map.IsBlocked(2, 2));
    }

    // Death drops a blood puddle and re-renders, both of which need a real Game and a post-gen
    // map behind them - so these use a fully generated game rather than CreateMap().
    static int PuddleCount(Map map, Moveable owner) =>
        map.GameObjects.Count(g => g.Name == owner.Name + "_puddle");

    static Decoration PlayerDecoration(Map map) =>
        map.MoveableDecorations[map.Player.X, map.Player.Y].Single(d => d.GameObject == map.Player);

    [Fact]
    public void NewGameIsNotGameOver()
    {
        var game = new Game();

        Assert.False(game.Map.IsGameOver);
    }

    [Fact]
    public void PlayerDyingEndsTheGame()
    {
        var game = new Game();

        game.Map.Player.CombatComponent!.ApplyDamage(1000);

        Assert.True(game.Map.IsGameOver);
    }

    [Fact]
    public void GameOverIsNotTriggeredByAMonsterDying()
    {
        var game = new Game();
        var monster = game.Map.Monsters.First();

        monster.CombatComponent!.ApplyDamage(1000);

        Assert.DoesNotContain(monster, game.Map.Monsters);
        Assert.False(game.Map.IsGameOver);
    }

    [Fact]
    public void DeadPlayerLeavesACorpseOverABloodPuddle()
    {
        var game = new Game();
        var player = game.Map.Player;
        (int x, int y) = (player.X, player.Y);

        Assert.Equal(0, PuddleCount(game.Map, player));

        player.CombatComponent!.ApplyDamage(1000);

        // The corpse stays on the map rather than being removed, unlike a dead monster.
        Assert.Contains(player, game.Map.Moveables);

        var puddle = game
            .Map.Decorations[x, y]
            .Single(d => d.GameObject.Name == player.Name + "_puddle");

        // Behind (z-index 1) puts the puddle under the player's Middleground (2) decoration.
        Assert.Equal(Decoration.Layer.Behind, puddle.DecorationLayer);
    }

    [Fact]
    public void DeadPlayerIsStillDrawnButWithItsAnimationStopped()
    {
        var game = new Game();
        var player = game.Map.Player;

        Assert.False(PlayerDecoration(game.Map).AnimationPaused);

        player.CombatComponent!.ApplyDamage(1000);

        // Rendering moveables is the turn loop's job (Indoor.razor calls it once after
        // PlayerTookTurn()), not the kill handler's - so a direct ApplyDamage() call like this,
        // outside that loop, needs an explicit render to pick up the corpse's stopped animation.
        game.Map.RenderMoveables();

        var corpse = PlayerDecoration(game.Map);
        Assert.True(corpse.AnimationPaused);

        // Frozen, not removed - dropping the animation class would render nothing at all.
        Assert.False(string.IsNullOrEmpty(corpse.AnimationClass));
    }

    [Fact]
    public void FurtherHitsOnADeadPlayerDoNotRepeatTheDeath()
    {
        var game = new Game();
        var player = game.Map.Player;

        // Several monsters attack within one turn, and CombatComponent raises GameObjectKilled on
        // every hit that leaves the player at or below zero - so the death must be handled once,
        // or the puddles (and the lose jingle) would stack up.
        player.CombatComponent!.ApplyDamage(1000);
        player.CombatComponent.ApplyDamage(1000);
        player.CombatComponent.ApplyDamage(1000);

        Assert.True(game.Map.IsGameOver);
        Assert.Equal(1, PuddleCount(game.Map, player));
    }

    [Fact]
    public void HandlePlayerActionIsRefusedOnceTheGameIsOver()
    {
        var game = new Game();
        var player = game.Map.Player;
        player.CombatComponent!.ApplyDamage(1000);
        (int x, int y) = (player.X, player.Y);

        // '6' moves right; a dead player must not be able to take another turn even if the UI
        // somehow sends input.
        Assert.False(game.Map.HandlePlayerAction(shiftKey: false, numKey: '6'));
        Assert.Equal(x, player.X);
        Assert.Equal(y, player.Y);
    }
}
