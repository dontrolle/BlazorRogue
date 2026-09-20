using System;
using System.Linq;
using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

// Map.TakeTurn(PlayerAction) - the unified entry point for issue #88's headless play driver, and
// (as of Phase 4) GamePage.razor's own turn-taking path too. Exercises the same handlers
// MapTests/ItemInteractionTests/StairTests already cover individually
// (HandlePlayerActionCore/PickUpItemsAtPlayer/UseInventoryItem/DropInventoryItem/stairs), but
// checks the assembled TurnResult itself - in particular that a no-op (a wall bump) correctly
// reports TurnConsumed: false, so the caller knows not to give monsters a free turn.
public class TurnResultTests
{
    static Moveable NewCreature(
        int x,
        int y,
        int weaponSkill = 30,
        int weaponDamage = 5,
        int toughness = 0,
        int armour = 0,
        int wounds = 20,
        AIComponent? ai = null
    )
    {
        var type = new MoveableType(
            id: "dummy",
            name: "Dummy",
            animationClass: "animated_dummy",
            asciiCharacter: "d",
            asciiColour: "white",
            weaponSkill: weaponSkill,
            weaponDamage: weaponDamage,
            toughness: toughness,
            armour: armour,
            wounds: wounds,
            aiComponentId: AIComponentFactory.DefaultId,
            aiComponentSettings: SettingsMap.Empty,
            singular: true
        );
        return new Moveable(x, y, ai, type);
    }

    // Same technique as MapTests/LiquidPoolTests/FightingSystemTests's BareFloorMap - a real Game
    // (so Game.FightingSystem/AddMessage/TransitionToLevel work) over a fully walkable map. Callers
    // must call PostGenInitalize() themselves once every game object (monsters, liquid tiles, ...)
    // is placed - TakeTurn's unconditional RenderMoveables() call requires it.
    static Map BareFloorMap(Game game, Moveable player, int size = 10)
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
        game.Map = map;
        map.AddPlayer(player);
        return map;
    }

    // Map generation can drop items/chests/decorations on any floor tile, the player's own start
    // tile included - same technique as ItemInteractionTests.PlacePlayerOnEmptyTile.
    static void PlacePlayerOnEmptyTile(Game game)
    {
        var map = game.Map;
        for (int x = 1; x < map.Width - 1; x++)
        {
            for (int y = 1; y < map.Height - 1; y++)
            {
                if (
                    map.Tiles[x, y].TileType == TileType.Floor
                    && !map.IsBlocked(x, y)
                    && !map.GameObjectByCoord[x, y].Any()
                )
                {
                    map.Player.PlaceAt(x, y);
                    return;
                }
            }
        }

        throw new InvalidOperationException("No empty floor tile found to place the player on.");
    }

    static readonly ItemType TestPotion = new(
        id: "test_potion",
        name: "Test potion",
        kind: ItemKind.UseOnce,
        image: "potion_red",
        character: "!",
        characterColor: "red",
        effectKind: ItemEffectKind.Heal,
        effectMagnitude: 5
    );

    [Fact]
    public void MoveIntoOpenSpaceConsumesTheTurnAndMovesThePlayer()
    {
        var game = new Game();
        var map = BareFloorMap(game, NewCreature(4, 4));
        map.PostGenInitalize();

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.True(result.TurnConsumed);
        Assert.Equal((5, 4), (map.Player.X, map.Player.Y));
        Assert.Null(result.PlayerAttack);
        Assert.Empty(result.MonsterAttacks);
        Assert.False(result.GameOverThisTurn);
        Assert.False(result.LevelChangedThisTurn);
    }

    [Fact]
    public void MoveIntoAWallDoesNotConsumeTheTurn()
    {
        var game = new Game();
        var map = BareFloorMap(game, NewCreature(4, 4));
        map.Tiles[5, 4].Blocking = true;
        map.PostGenInitalize();

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.False(result.TurnConsumed);
        Assert.Equal((4, 4), (map.Player.X, map.Player.Y));
        Assert.Empty(result.MonsterAttacks); // monsters get no free turn from a wall bump
    }

    [Fact]
    public void MoveIntoAMonsterAttacksAndReturnsAKillingPlayerAttackResult()
    {
        var game = new Game();
        var monster = NewCreature(5, 4, weaponSkill: 1, wounds: 1);
        var map = BareFloorMap(game, NewCreature(4, 4, weaponSkill: 100));
        map.AddMoveable(monster);
        map.PostGenInitalize();

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.True(result.TurnConsumed);
        Assert.Equal((4, 4), (map.Player.X, map.Player.Y)); // attacking, not moving onto the tile
        Assert.NotNull(result.PlayerAttack);
        Assert.True(result.PlayerAttack!.Value.Hit);
        Assert.True(result.PlayerAttack.Value.DefenderKilled);
        Assert.Equal(0, monster.CombatComponent!.Wounds);
    }

    [Fact]
    public void MonsterCounterAttacksDuringPlayerTookTurnAppearInMonsterAttacks()
    {
        var game = new Game();
        var map = BareFloorMap(game, NewCreature(4, 4));
        var ai = (SimpleAIComponent)
            AIComponentFactory.Create(SimpleAIComponent.ComponentId, map, SettingsMap.Empty);
        // Not adjacent to the player's start tile (4,4), but adjacent to where the player is about
        // to move (5,4) - so it attacks on its own turn right after, rather than closing distance.
        var monster = NewCreature(6, 4, ai: ai);
        map.AddMonster(monster);
        ai.Wake();
        map.PostGenInitalize();

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.Equal((5, 4), (map.Player.X, map.Player.Y));
        var monsterAttack = Assert.Single(result.MonsterAttacks);
        Assert.Same(monster, monsterAttack.Attacker);
    }

    [Fact]
    public void MoveOntoAnInstakillLiquidEndsTheGameAndSkipsTheMonstersTurn()
    {
        var game = new Game();
        var map = BareFloorMap(game, NewCreature(4, 4));
        var ai = (SimpleAIComponent)
            AIComponentFactory.Create(SimpleAIComponent.ComponentId, map, SettingsMap.Empty);
        var monster = NewCreature(6, 4, ai: ai);
        map.AddMonster(monster);
        ai.Wake();
        map.SetLiquidTile(
            5,
            4,
            new LiquidType(
                id: "test_lava",
                name: "lava",
                spriteName: "water_blue",
                frameCount: 1,
                animationDurationSeconds: 1.0,
                lipIndex: 1,
                asciiColor: "#000000",
                effectKind: LiquidEffectKind.Instakill,
                effectMagnitude: 0
            )
        );
        map.PostGenInitalize();

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.True(result.TurnConsumed);
        Assert.True(result.GameOverThisTurn);
        Assert.True(map.IsGameOver);
        Assert.Empty(result.MonsterAttacks); // no free turn for monsters once the player is dead
    }

    [Fact]
    public void TakeTurnIsANoOpOnceTheGameIsAlreadyOver()
    {
        var game = new Game();
        var map = BareFloorMap(game, NewCreature(4, 4, wounds: 1));
        map.PostGenInitalize();
        map.Player.CombatComponent!.ApplyDamage(1000);
        Assert.True(map.IsGameOver); // guards the premise

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.False(result.TurnConsumed);
        Assert.False(result.GameOverThisTurn); // it was already over, not newly over this turn
        Assert.Equal((4, 4), (map.Player.X, map.Player.Y));
    }

    [Fact]
    public void PickUpConsumesTheTurnAndAddsTheItemToInventory()
    {
        var game = new Game();
        PlacePlayerOnEmptyTile(game);
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        game.Map.AddGameObject(new Item(x, y, TestPotion));

        var result = game.Map.TakeTurn(new PlayerAction.PickUp());

        Assert.True(result.TurnConsumed);
        Assert.Contains(
            game.Map.Player.InventoryComponent!.Items.Values,
            entry => entry.ItemType == TestPotion
        );
    }

    [Fact]
    public void PickUpOnAnEmptyTileDoesNotConsumeTheTurn()
    {
        var game = new Game();
        PlacePlayerOnEmptyTile(game);

        var result = game.Map.TakeTurn(new PlayerAction.PickUp());

        Assert.False(result.TurnConsumed);
    }

    [Fact]
    public void UseItemConsumesTheTurnAndAppliesItsEffect()
    {
        var game = new Game();
        game.Map.Player.CombatComponent!.ApplyDamage(10);
        game.Map.Player.InventoryComponent!.TryPickUp(TestPotion, out char letter);
        int woundsBeforeUse = game.Map.Player.CombatComponent.Wounds;

        var result = game.Map.TakeTurn(new PlayerAction.UseItem(letter));

        Assert.True(result.TurnConsumed);
        Assert.True(game.Map.Player.CombatComponent.Wounds > woundsBeforeUse);
    }

    [Fact]
    public void DropItemConsumesTheTurnAndPlacesTheItemOnTheFloor()
    {
        var game = new Game();
        PlacePlayerOnEmptyTile(game);
        var (x, y) = (game.Map.Player.X, game.Map.Player.Y);
        game.Map.Player.InventoryComponent!.TryPickUp(TestPotion, out char letter);

        var result = game.Map.TakeTurn(new PlayerAction.DropItem(letter));

        Assert.True(result.TurnConsumed);
        Assert.DoesNotContain(letter, game.Map.Player.InventoryComponent.Items.Keys);
        Assert.Contains(
            game.Map.GameObjectByCoord[x, y],
            go => go is Item item && item.PickupableComponent!.ItemType == TestPotion
        );
    }

    [Fact]
    public void DropItemWithNoSuchLetterDoesNotConsumeTheTurn()
    {
        var game = new Game();

        var result = game.Map.TakeTurn(new PlayerAction.DropItem('z'));

        Assert.False(result.TurnConsumed);
    }

    [Fact]
    public void UseDirectionOnStairsTransitionsTheLevelAndReportsLevelChanged()
    {
        var game = new Game();
        var originalMap = game.Map;
        var player = game.Map.Player;
        game.Map.AddGameObject(new Stair(player.X, player.Y, StairDirection.Down));

        var result = game.Map.TakeTurn(new PlayerAction.UseDirection(Direction.None));

        Assert.True(result.TurnConsumed);
        Assert.True(result.LevelChangedThisTurn);
        Assert.NotSame(originalMap, game.Map);
        Assert.Equal(1, game.CurrentLevelNumber);
    }
}
