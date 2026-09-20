using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.Tests.TestSupport;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

// HeadlessPlayDriver/IPlayerPolicy/RandomHazardAvoidingPolicy - issue #88's headless play driver.
// Map.TakeTurn itself (the thing the driver forwards to) is already exercised directly in
// TurnResultTests/MapTests; these tests are about the driver's own thin delegation and the
// built-in policy's action selection, not about re-proving TakeTurn's turn-resolution rules.
public class HeadlessPlayDriverTests
{
    static Moveable NewCreature(
        int x,
        int y,
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
        return new Moveable(x, y, null, type);
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

    // Same technique as MapTests/TurnResultTests's BareFloorMap - a fully walkable map over a real
    // Game (so TakeTurn's FightingSystem/AddMessage/render calls all work). Overwrites the
    // driver's freshly-generated starting map; HeadlessPlayDriver.Map reads Game.Map dynamically,
    // so the driver picks the replacement up automatically.
    static Map BareFloorMap(HeadlessPlayDriver driver, int size = 10)
    {
        var wallSet = new TileSet("w", TileType.Wall, "w", [0]);
        var floorSet = new TileSet("f", TileType.Floor, "f", [0]);
        var map = new Map(size, size, wallSet, driver.Game);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                map.Tiles[x, y].TileSet = floorSet;
                map.Tiles[x, y].Blocking = false;
            }
        }
        driver.Game.Map = map;
        return map;
    }

    static readonly LiquidType Lava = new(
        id: "test_lava",
        name: "lava",
        spriteName: "water_blue",
        frameCount: 1,
        animationDurationSeconds: 1.0,
        lipIndex: 1,
        asciiColor: "#000000",
        effectKind: LiquidEffectKind.Instakill,
        effectMagnitude: 0
    );

    // Walls off every neighbor of (5, 5) except the one at (destX, destY), which is left open (or,
    // if makeLava is true, turned into an instakill liquid) - used to pin down exactly which
    // action(s) RandomHazardAvoidingPolicy has to choose from.
    static void BoxInPlayerExceptOneNeighbor(Map map, int destX, int destY, bool makeLava = false)
    {
        foreach (var direction in Enum.GetValues<Direction>())
        {
            if (direction == Direction.None)
            {
                continue;
            }

            var (dx, dy) = direction.ToDelta();
            int x = 5 + dx;
            int y = 5 + dy;
            if (x == destX && y == destY)
            {
                continue;
            }

            map.Tiles[x, y].Blocking = true;
        }

        if (makeLava)
        {
            map.SetLiquidTile(destX, destY, Lava);
        }
    }

    [Fact]
    public void ConstructorWrapsAFreshlyGeneratedPlayableGame()
    {
        var driver = new HeadlessPlayDriver();

        Assert.False(driver.Map.IsGameOver);
        Assert.NotNull(driver.Map.Player);
    }

    [Fact]
    public void TakeTurnDelegatesToMapTakeTurn()
    {
        var driver = new HeadlessPlayDriver();
        var map = BareFloorMap(driver);
        map.AddPlayer(NewCreature(4, 4));
        map.PostGenInitalize();

        var result = driver.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.True(result.TurnConsumed);
        Assert.Equal((5, 4), (driver.Map.Player.X, driver.Map.Player.Y));
    }

    // Migrated from MapTests.TakeTurnDoesNotAttackAMoveableAcrossABlockedEdge - a blocked edge
    // (e.g. a fence) is supposed to block reaching *through* it, not just walking through it.
    [Fact]
    public void TakeTurnDoesNotAttackAMoveableAcrossABlockedEdge()
    {
        var driver = new HeadlessPlayDriver();
        var map = BareFloorMap(driver);
        map.AddPlayer(NewCreature(4, 4));

        // Blocks the East edge of the player's own tile.
        map.AddGameObject(new StaticDecorativeObject(4, 4, TestFence(Edge.East)));

        var target = NewCreature(5, 4);
        map.AddMoveable(target);

        int messageCountBefore = driver.Game.Messages.Count;

        map.PostGenInitalize();
        var result = driver.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.False(result.TurnConsumed);
        Assert.Equal((4, 4), (driver.Map.Player.X, driver.Map.Player.Y)); // the edge blocks the move too
        Assert.Equal(target.CombatComponent!.MaxWounds, target.CombatComponent.Wounds); // never hit
        Assert.Equal(messageCountBefore, driver.Game.Messages.Count); // CloseCombatAttack never even ran
    }

    // Migrated from MapTests.FlyingPlayerCanMoveAcrossABlockedEdge - a flying creature is
    // unaffected by a fence line the same way it's unaffected by liquid ground effects.
    [Fact]
    public void FlyingPlayerCanMoveAcrossABlockedEdge()
    {
        var driver = new HeadlessPlayDriver();
        var map = BareFloorMap(driver);
        map.AddPlayer(NewCreature(4, 4, abilities: FlyingAbility));
        map.AddGameObject(new StaticDecorativeObject(4, 4, TestFence(Edge.East)));
        map.PostGenInitalize();

        var result = driver.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.True(result.TurnConsumed);
        Assert.Equal((5, 4), (driver.Map.Player.X, driver.Map.Player.Y));
    }

    [Fact]
    public void TakeTurnWithPolicyAsksThePolicyForTheNextActionAndTakesIt()
    {
        var driver = new HeadlessPlayDriver();
        var map = BareFloorMap(driver);
        map.AddPlayer(NewCreature(4, 4));
        map.PostGenInitalize();
        var policy = new FixedActionPolicy(new PlayerAction.Move(Direction.East));

        var result = driver.TakeTurn(policy);

        Assert.True(result.TurnConsumed);
        Assert.Equal((5, 4), (driver.Map.Player.X, driver.Map.Player.Y));
    }

    sealed class FixedActionPolicy(PlayerAction action) : IPlayerPolicy
    {
        public PlayerAction NextAction(Map map) => action;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomHazardAvoidingPolicyNeverStepsIntoALethalLiquid(int seed)
    {
        var driver = new HeadlessPlayDriver();
        var map = BareFloorMap(driver);
        map.AddPlayer(NewCreature(5, 5));
        // Every neighbor blocked except East, which is lava - so the only "move" candidate the
        // policy could otherwise pick is the one it must exclude.
        BoxInPlayerExceptOneNeighbor(map, destX: 6, destY: 5, makeLava: true);
        map.PostGenInitalize();

        var policy = new RandomHazardAvoidingPolicy(new Random(seed));
        var action = policy.NextAction(map);

        // Nothing else was legal either, so the policy must fall back to waiting in place.
        Assert.Equal(new PlayerAction.Move(Direction.None), action);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void RandomHazardAvoidingPolicyPicksUpAnItemWhenNothingElseIsLegal(int seed)
    {
        var driver = new HeadlessPlayDriver();
        var map = BareFloorMap(driver);
        map.AddPlayer(NewCreature(5, 5));
        // Box the player in on every side, with no lava - the item underfoot is the only legal
        // action left.
        BoxInPlayerExceptOneNeighbor(map, destX: -1, destY: -1);
        map.AddGameObject(
            new Item(
                5,
                5,
                new ItemType(
                    id: "test_potion",
                    name: "Test potion",
                    kind: ItemKind.UseOnce,
                    image: "potion_red",
                    character: "!",
                    characterColor: "red",
                    effectKind: ItemEffectKind.Heal,
                    effectMagnitude: 5
                )
            )
        );
        map.PostGenInitalize();

        var policy = new RandomHazardAvoidingPolicy(new Random(seed));
        var action = policy.NextAction(map);

        Assert.Equal(new PlayerAction.PickUp(), action);
    }

    [Fact]
    public void RandomHazardAvoidingPolicyCanPlayARealGeneratedGameForManyTurnsWithoutThrowing()
    {
        var driver = new HeadlessPlayDriver();
        var policy = new RandomHazardAvoidingPolicy(new Random(42));

        const int turnLimit = 300;
        int turnsTaken = 0;
        for (; turnsTaken < turnLimit; turnsTaken++)
        {
            var result = driver.TakeTurn(policy);
            if (result.GameOverThisTurn)
            {
                break;
            }
        }

        // Not asserting a specific outcome (win/lose/turn-limit-reached all count as fine here) -
        // just that hundreds of turns of random hazard-avoiding play never throw, which is the
        // property a future play-balance sweep would actually rely on.
        Assert.True(turnsTaken > 0);
    }
}
