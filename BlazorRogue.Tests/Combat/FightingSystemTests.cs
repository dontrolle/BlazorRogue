using BlazorRogue.AI;
using BlazorRogue.Combat.Warhammer;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests.Combat;

public class FightingSystemTests
{
    static Moveable CreateMoveable(
        string id,
        int weaponSkill,
        int weaponDamage = 8,
        int toughness = 100,
        int armour = 0,
        int wounds = 1000,
        int x = 0,
        int y = 0,
        IReadOnlyDictionary<AbilityId, SettingsMap>? abilities = null
    )
    {
        var type = new MoveableType(
            id: id,
            name: id,
            animationClass: "animated_test",
            asciiCharacter: "t",
            asciiColour: "white",
            weaponSkill: weaponSkill,
            weaponDamage: weaponDamage,
            toughness: toughness,
            armour: armour,
            wounds: wounds,
            aiComponentId: AIComponentFactory.DefaultId,
            aiComponentSettings: SettingsMap.Empty,
            singular: true,
            abilities: abilities
        );

        return new Moveable(x, y, aIComponent: null, type);
    }

    // A small all-floor map wired to a real Game (so Game.AddMessage/DebugMode work) - same
    // technique as LiquidPoolTests.BareFloorMap/MapTests.BareFloorMap. Assigning onto game.Map (not
    // the dungeon Game generated itself) means References.Game.Map - which a pushed-back
    // Moveable.Move's enter-hook reads - resolves to this bare map too.
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
        game.Map = map;
        return map;
    }

    static Dictionary<AbilityId, SettingsMap> PushBackAbility(int chance) =>
        new()
        {
            [AbilityId.PushBack] = new SettingsMap(
                new Dictionary<string, object> { ["chance"] = chance }
            ),
        };

    [Fact]
    public void CloseCombatAttackThrowsForNullAttacker()
    {
        var fightingSystem = new FightingSystem(game: null!);
        var defender = CreateMoveable("defender", 30);

        _ = Assert.Throws<ArgumentNullException>(() =>
            fightingSystem.CloseCombatAttack(null!, defender.CombatComponent!)
        );
    }

    [Fact]
    public void CloseCombatAttackThrowsForNullDefender()
    {
        var fightingSystem = new FightingSystem(game: null!);
        var attacker = CreateMoveable("attacker", 30);

        _ = Assert.Throws<ArgumentNullException>(() =>
            fightingSystem.CloseCombatAttack(attacker.CombatComponent!, null!)
        );
    }

    [Fact]
    public void CloseCombatAttackHigherWeaponSkillWinsMoreOftenOverManyRounds()
    {
        // High toughness/wounds so nobody actually dies mid-way through the sample, which would
        // otherwise stop generating attacks against that combatant (a fresh dummy is used per round instead).
        var fightingSystem = new FightingSystem(game: null!);

        const int rounds = 2000;
        int strongAttackerHits = 0;
        int weakAttackerHits = 0;

        for (int i = 0; i < rounds; i++)
        {
            var strongAttacker = CreateMoveable("strong", weaponSkill: 70);
            var weakDefender = CreateMoveable("weakDefender", weaponSkill: 20);
            if (
                fightingSystem
                    .CloseCombatAttack(
                        strongAttacker.CombatComponent!,
                        weakDefender.CombatComponent!
                    )
                    .Hit
            )
            {
                strongAttackerHits++;
            }

            var weakAttacker = CreateMoveable("weak", weaponSkill: 20);
            var strongDefender = CreateMoveable("strongDefender", weaponSkill: 70);
            if (
                fightingSystem
                    .CloseCombatAttack(
                        weakAttacker.CombatComponent!,
                        strongDefender.CombatComponent!
                    )
                    .Hit
            )
            {
                weakAttackerHits++;
            }
        }

        Assert.True(
            strongAttackerHits > weakAttackerHits,
            $"Expected an attacker with much higher weapon skill to land more hits ({strongAttackerHits}) than one with much lower weapon skill ({weakAttackerHits}) over {rounds} rounds."
        );
    }

    [Fact]
    public void CombatMessageCallsThePlayerYouWhenItIsTheAttacker()
    {
        var game = new Game();
        var player = game.Map.Player;
        var goblin = CreateMoveable("Goblin", weaponSkill: 30, weaponDamage: 1);

        _ = game.FightingSystem.CloseCombatAttack(player.CombatComponent!, goblin.CombatComponent!);

        string message = game.Messages[^1];
        Assert.StartsWith("You ", message);
        Assert.Contains("the Goblin", message);
        Assert.DoesNotContain(player.Name, message);
    }

    [Fact]
    public void CombatMessageCallsThePlayerYouWhenItIsTheDefender()
    {
        var game = new Game();
        var player = game.Map.Player;
        var goblin = CreateMoveable("Goblin", weaponSkill: 30, weaponDamage: 1);

        _ = game.FightingSystem.CloseCombatAttack(goblin.CombatComponent!, player.CombatComponent!);

        string message = game.Messages[^1];
        Assert.StartsWith("The Goblin ", message);
        Assert.Contains(" you", message);
        Assert.DoesNotContain(player.Name, message);
    }

    // A weaponSkill of 100 against 1 guarantees a hit regardless of either d100 roll (see
    // Dice.GetSuccessLevel) - the same trick CloseCombatAttackHigherWeaponSkillWinsMoreOftenOverManyRounds
    // relies on statistically, but deterministic here since these tests need a single guaranteed hit.
    [Fact]
    public void CloseCombatAttackReturnsHitAndDamageWhenAttackLands()
    {
        var fightingSystem = new FightingSystem(game: null!);
        var attacker = CreateMoveable("attacker", weaponSkill: 100, weaponDamage: 8);
        var defender = CreateMoveable("defender", weaponSkill: 1, toughness: 0, armour: 0);

        var result = fightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.True(result.Hit);
        Assert.True(result.Damage > 0);
        Assert.Equal(
            result.Damage,
            defender.CombatComponent!.MaxWounds - defender.CombatComponent.Wounds
        );
        Assert.False(result.DefenderKilled);
    }

    [Fact]
    public void CloseCombatAttackReturnsDefenderKilledWhenWoundsReachZero()
    {
        var fightingSystem = new FightingSystem(game: null!);
        var attacker = CreateMoveable("attacker", weaponSkill: 100, weaponDamage: 8);
        var defender = CreateMoveable(
            "defender",
            weaponSkill: 1,
            toughness: 0,
            armour: 0,
            wounds: 1
        );

        var result = fightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.True(result.Hit);
        Assert.True(result.DefenderKilled);
        Assert.Equal(0, defender.CombatComponent!.Wounds);
    }

    [Fact]
    public void PushBackAlwaysMovesTheDefenderWhenChanceIs100()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        var attacker = CreateMoveable(
            "attacker",
            weaponSkill: 100,
            x: 4,
            y: 4,
            abilities: PushBackAbility(100)
        );
        var defender = CreateMoveable("defender", weaponSkill: 1, x: 5, y: 4);
        map.AddMoveable(attacker);
        map.AddMoveable(defender);

        _ = game.FightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.Equal((6, 4), (defender.X, defender.Y));
        Assert.Contains(game.Messages, m => m.Contains("backward"));
    }

    [Fact]
    public void PushBackNeverMovesTheDefenderWhenChanceIs0()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        var attacker = CreateMoveable(
            "attacker",
            weaponSkill: 100,
            x: 4,
            y: 4,
            abilities: PushBackAbility(0)
        );
        var defender = CreateMoveable("defender", weaponSkill: 1, x: 5, y: 4);
        map.AddMoveable(attacker);
        map.AddMoveable(defender);

        _ = game.FightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.Equal((5, 4), (defender.X, defender.Y));
    }

    [Fact]
    public void AMoveableWithoutThePushBackAbilityNeverPushesEvenWhenItHits()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        var attacker = CreateMoveable("attacker", weaponSkill: 100, x: 4, y: 4);
        var defender = CreateMoveable("defender", weaponSkill: 1, x: 5, y: 4);
        map.AddMoveable(attacker);
        map.AddMoveable(defender);

        _ = game.FightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.Equal((5, 4), (defender.X, defender.Y));
    }

    [Fact]
    public void PushBackFizzlesWithAMessageWhenTheDestinationIsBlocked()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        var attacker = CreateMoveable(
            "attacker",
            weaponSkill: 100,
            x: 4,
            y: 4,
            abilities: PushBackAbility(100)
        );
        var defender = CreateMoveable("defender", weaponSkill: 1, x: 5, y: 4);
        map.AddMoveable(attacker);
        map.AddMoveable(defender);
        map.Tiles[6, 4].Blocking = true; // a wall at the push destination

        _ = game.FightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.Equal((5, 4), (defender.X, defender.Y));
        Assert.Contains(game.Messages, m => m.Contains("nowhere to go"));
    }

    [Fact]
    public void PushBackIntoLavaKillsTheDefender()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        var attacker = CreateMoveable(
            "attacker",
            weaponSkill: 100,
            x: 4,
            y: 4,
            abilities: PushBackAbility(100)
        );
        var defender = CreateMoveable("defender", weaponSkill: 1, x: 5, y: 4);
        map.AddPlayer(defender); // Kill() only ends the game for the player - used as the observable signal
        map.AddMoveable(attacker);
        map.SetLiquidTile(
            6,
            4,
            new LiquidType(
                id: "test_lava",
                name: "lava",
                spriteName: "water_lava",
                frameCount: 4,
                animationDurationSeconds: 1.0,
                lipIndex: 1,
                asciiColor: "#ff0000",
                effectKind: LiquidEffectKind.Instakill,
                effectMagnitude: 0
            )
        );

        _ = game.FightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.True(map.IsGameOver);
    }

    [Fact]
    public void FlyingDefenderCanBePushedAcrossABlockedEdge()
    {
        var game = new Game();
        var map = BareFloorMap(game);
        var flying = new Dictionary<AbilityId, SettingsMap>
        {
            [AbilityId.Flying] = SettingsMap.Empty,
        };
        var attacker = CreateMoveable(
            "attacker",
            weaponSkill: 100,
            x: 4,
            y: 4,
            abilities: PushBackAbility(100)
        );
        var defender = CreateMoveable("defender", weaponSkill: 1, x: 5, y: 4, abilities: flying);
        map.AddMoveable(attacker);
        map.AddMoveable(defender);
        // Blocks the East edge of the defender's own tile - the push destination is (6, 4).
        map.AddGameObject(
            new StaticDecorativeObject(
                5,
                4,
                new StaticDecorativeObjectType(
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
                    blockedEdges: Edge.East
                )
            )
        );

        _ = game.FightingSystem.CloseCombatAttack(
            attacker.CombatComponent!,
            defender.CombatComponent!
        );

        Assert.Equal((6, 4), (defender.X, defender.Y));
    }
}
