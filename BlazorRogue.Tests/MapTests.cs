using BlazorRogue.AI;
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

    // A small all-floor map wired to a real Game (so Game.FightingSystem/AddMessage work) - same
    // technique as LiquidPoolTests.BareFloorMap, needed for the two combat-across-a-blocked-edge
    // tests below since they exercise the full HandlePlayerAction/SimpleAIComponent.TakeTurn paths,
    // not just the IsMovementBlockedAcrossEdge primitive.
    static Map BareFloorMap(Game game, int size = 10)
    {
        var wallSet = new TileSet("w", TileType.Wall, "w", [0]);
        var floorSet = new TileSet("f", TileType.Floor, "f", [0]);
        var map = new Map(size, size, wallSet, game);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                map.Tiles[x, y].TileSet = floorSet;
                map.Tiles[x, y].Blocking = false;
            }
        }
        References.Map = map;
        return map;
    }

    static Moveable NewCreature(
        int x,
        int y,
        AIComponent? ai = null,
        IReadOnlyDictionary<AbilityId, SettingsMap>? abilities = null
    )
    {
        var type = new MoveableType(
            id: "dummy",
            name: "Dummy",
            animationClass: "animated_dummy",
            asciiCharacter: "d",
            asciiColour: "white",
            weaponSkill: 30,
            weaponDamage: 5,
            toughness: 0,
            armour: 0,
            wounds: 20,
            aiComponentId: AIComponentFactory.DefaultId,
            aiComponentSettings: SettingsMap.Empty,
            singular: true,
            abilities: abilities
        );
        return new Moveable(x, y, ai, type);
    }

    static readonly Dictionary<AbilityId, SettingsMap> FlyingAbility = new()
    {
        [AbilityId.Flying] = SettingsMap.Empty,
    };

    static StaticDecorativeObjectType TestFence(Edge blockedEdges) =>
        new(
            id: "test_fence",
            name: "Test Fence",
            image: new Dictionary<string, string> { [""] = "img" },
            animationClasses: [],
            infoText: "",
            verticalOffset: 0,
            character: "",
            characterColor: "",
            blocking: false,
            makeCoveringOffsetDecsTransparent: false,
            blockedEdges: blockedEdges
        );

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

    // The shared "fence" primitive (see Edge/GameObject.BlockedEdges) - a GameObject blocking
    // movement across one edge of its own tile without necessarily being Blocking itself. Statue
    // is the first consumer; a future single-tile fence would declare the same way.
    [Fact]
    public void IsMovementBlockedAcrossEdgeOnlyBlocksTheDeclaredEdgeAndNeverAnUnsealedDiagonal()
    {
        var map = CreateMap();
        var fenceType = new StaticDecorativeObjectType(
            id: "test_fence",
            name: "Test Fence",
            image: new Dictionary<string, string> { [""] = "img" },
            animationClasses: [],
            infoText: "",
            verticalOffset: 0,
            character: "",
            characterColor: "",
            blocking: false,
            makeCoveringOffsetDecsTransparent: false,
            blockedEdges: Edge.North
        );
        map.AddGameObject(new StaticDecorativeObject(3, 3, fenceType));

        // Blocked across the declared (North) edge, regardless of which side is "from"...
        Assert.True(map.IsMovementBlockedAcrossEdge(3, 3, 3, 2));
        Assert.True(map.IsMovementBlockedAcrossEdge(3, 2, 3, 3));

        // ...but no other orthogonal edge of that tile is affected. A single blocked edge can only
        // ever seal one of a diagonal's two detour paths (see IsDiagonalCornerSealed) - never both -
        // so diagonal moves stay open around it too.
        Assert.False(map.IsMovementBlockedAcrossEdge(3, 3, 3, 4));
        Assert.False(map.IsMovementBlockedAcrossEdge(3, 3, 2, 3));
        Assert.False(map.IsMovementBlockedAcrossEdge(3, 3, 4, 3));
        Assert.False(map.IsMovementBlockedAcrossEdge(3, 3, 2, 2));
    }

    // A diagonal move that would otherwise cut straight through a sealed corner - both detours
    // around it blocked - must be denied too, or a mover can bypass two blocked edges at once by
    // stepping onto/through the corner tile diagonally. Reported live: a monster walked diagonally
    // through a fence enclosure corner (dontrolle/BlazorRogue-internal#86).
    [Fact]
    public void IsMovementBlockedAcrossEdgeDeniesADiagonalThatCutsASealedCorner()
    {
        var map = CreateMap();
        // A corner like fence_corner_sw: one GameObject blocking two edges of its own tile (West
        // and South), at (3,3). Interior is north/east of it; exterior is south/west.
        var cornerType = new StaticDecorativeObjectType(
            id: "test_corner",
            name: "Test Corner",
            image: new Dictionary<string, string> { [""] = "img" },
            animationClasses: [],
            infoText: "",
            verticalOffset: 0,
            character: "",
            characterColor: "",
            blocking: false,
            makeCoveringOffsetDecsTransparent: false,
            blockedEdges: Edge.West | Edge.South
        );
        map.AddGameObject(new StaticDecorativeObject(3, 3, cornerType));

        // From exterior (2,4), southwest of the corner, straight onto the corner tile (3,3) - both
        // straight-line detours around it (via (3,4) then north, or via (2,3) then east) are
        // blocked by the corner's own South/West edges, so the shortcut must be denied too.
        Assert.True(map.IsMovementBlockedAcrossEdge(2, 4, 3, 3));
        Assert.True(map.IsMovementBlockedAcrossEdge(3, 3, 2, 4));

        // Approaching the same corner tile diagonally from the southeast (interior-ish side) stays
        // open: the vertical-first detour ((4,4)->(4,3)->(3,3)) is fully clear even though the
        // horizontal-first one isn't, so only one of the two detours is blocked, not both - this
        // isn't a blanket "diagonals near a fence are blocked" rule, only a fully sealed corner is.
        Assert.False(map.IsMovementBlockedAcrossEdge(4, 4, 3, 3));
    }

    // A blocked edge (e.g. a fence) is supposed to block reaching *through* it, not just walking
    // through it - reported live: a moveable standing just across a fence line could still be
    // attacked, because the player-move handler only checked IsBlocked(dest) (true here regardless
    // of the fence, since the target moveable itself blocks the tile) before falling into "nothing
    // to move onto, try attacking what's there" - it never re-checked the edge for the attack case.
    [Fact]
    public void HandlePlayerActionDoesNotAttackAMoveableAcrossABlockedEdge()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        map.AddPlayer(NewCreature(4, 4));

        // Blocks the East edge of the player's own tile - the shared "fence" primitive (see
        // Edge/GameObject.BlockedEdges), not tied to the real fence_* catalog.
        map.AddGameObject(new StaticDecorativeObject(4, 4, TestFence(Edge.East)));

        var target = NewCreature(5, 4);
        map.AddMoveable(target);

        int messageCountBefore = game.Messages.Count;

        map.HandlePlayerAction(shiftKey: false, numKey: '6'); // '6' = move/attack east

        Assert.Equal((4, 4), (map.Player.X, map.Player.Y)); // the edge blocks the move too
        Assert.Equal(target.CombatComponent!.MaxWounds, target.CombatComponent.Wounds); // never hit
        Assert.Equal(messageCountBefore, game.Messages.Count); // CloseCombatAttack never even ran
    }

    // Same bug, the other direction: a monster adjacent to the player across a blocked edge must
    // not be able to attack through it either (SimpleAIComponent.TakeTurn had the identical gap).
    [Fact]
    public void SimpleAiCannotAttackThePlayerAcrossABlockedEdge()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        map.AddPlayer(NewCreature(5, 4));

        // Blocks the West edge of the player's own tile, the edge shared with the monster at (4,4).
        map.AddGameObject(new StaticDecorativeObject(5, 4, TestFence(Edge.West)));

        var ai = (SimpleAIComponent)
            AIComponentFactory.Create(SimpleAIComponent.ComponentId, map, SettingsMap.Empty);
        var monster = NewCreature(4, 4, ai);
        ai.Wake();

        int messageCountBefore = game.Messages.Count;

        ai.TakeTurn();

        Assert.Equal((4, 4), (monster.X, monster.Y)); // the edge blocks the move too
        Assert.Equal(map.Player.CombatComponent!.MaxWounds, map.Player.CombatComponent.Wounds); // never hit
        Assert.Equal(messageCountBefore, game.Messages.Count); // CloseCombatAttack never even ran
    }

    // A flying creature (AbilityId.Flying) is unaffected by a fence line the same way it's
    // unaffected by liquid ground effects - it can move and attack across a blocked edge that
    // would otherwise stop it.
    [Fact]
    public void FlyingPlayerCanMoveAcrossABlockedEdge()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        map.AddPlayer(NewCreature(4, 4, abilities: FlyingAbility));

        map.AddGameObject(new StaticDecorativeObject(4, 4, TestFence(Edge.East)));

        map.HandlePlayerAction(shiftKey: false, numKey: '6'); // '6' = move east

        Assert.Equal((5, 4), (map.Player.X, map.Player.Y));
    }

    [Fact]
    public void FlyingSimpleAiCanAttackThePlayerAcrossABlockedEdge()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        map.AddPlayer(NewCreature(5, 4));

        // Blocks the West edge of the player's own tile, the edge shared with the monster at (4,4).
        map.AddGameObject(new StaticDecorativeObject(5, 4, TestFence(Edge.West)));

        var ai = (SimpleAIComponent)
            AIComponentFactory.Create(SimpleAIComponent.ComponentId, map, SettingsMap.Empty);
        var monster = NewCreature(4, 4, ai, FlyingAbility);
        ai.Wake();

        int messageCountBefore = game.Messages.Count;

        ai.TakeTurn();

        Assert.Equal((4, 4), (monster.X, monster.Y)); // still a melee attack, not a move
        Assert.True(game.Messages.Count > messageCountBefore); // CloseCombatAttack ran (hit or miss)
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

        // Rendering moveables is the turn loop's job (GamePage.razor calls it once after
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
    public void PlayerDeathMessageRefersToThePlayerAsYou()
    {
        var game = new Game();
        var player = game.Map.Player;

        player.CombatComponent!.ApplyDamage(1000);

        Assert.Contains("You were killed!", game.Messages);
        Assert.DoesNotContain(game.Messages, m => m == $"{player.Name} was killed!");
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
