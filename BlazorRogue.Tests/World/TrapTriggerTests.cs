using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests.World;

/// <summary>
/// Stepping onto a trap: it fires once (damage, message, shake, TurnResult.TrapTriggered), is
/// drawn once revealed and - while hidden - emits no decoration at all.
/// </summary>
public class TrapTriggerTests
{
    static TrapType SpikeTrap(bool startsHidden = true, bool reusable = false) =>
        new(
            id: "test_spikes",
            name: "spike trap",
            images: ["trap"],
            character: "^",
            characterColor: "#c0c0c0",
            infoText: "A spike trap, set into the floor",
            effectKind: TrapEffectKind.Damage,
            effectMagnitude: 3,
            startsHidden: startsHidden,
            reusable: reusable
        );

    static Moveable NewPlayer(int x, int y, int wounds = 20, int armour = 0) =>
        new(
            x,
            y,
            null,
            new MoveableType(
                id: "dummy",
                name: "Dummy",
                animationClass: "animated_dummy",
                asciiCharacter: "d",
                asciiColour: "white",
                weaponSkill: 30,
                weaponDamage: 5,
                toughness: 0,
                armour: armour,
                wounds: wounds,
                aiComponentId: AIComponentFactory.DefaultId,
                aiComponentSettings: SettingsMap.Empty,
                singular: true,
                tickCost: 6
            )
        );

    // The player starts at (4, 4); the trap is on (5, 4), one step east.
    static Map MapWithTrap(Game game, Trap trap, int wounds = 20, int armour = 0)
    {
        var wallSet = new TileSet("w", TileType.Wall, "w", [0]);
        var floorSet = new TileSet("f", TileType.Floor, "f", [0]);
        var map = new Map(10, 10, wallSet, game);
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                map.Tiles[x, y].TileSet = floorSet;
                map.Tiles[x, y].Blocking = false;
            }
        }
        game.Map = map;
        map.AddPlayer(NewPlayer(4, 4, wounds, armour));
        map.Tiles[5, 4].Trap = trap;
        map.PostGenInitalize();
        return map;
    }

    [Fact]
    public void SteppingOnAHiddenTrapDamagesThePlayerAndRevealsAndSpendsIt()
    {
        var game = new Game();
        var trap = new Trap(SpikeTrap());
        var map = MapWithTrap(game, trap);

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.Same(trap.Type, result.TrapTriggered);
        // 3 damage, plus the random +1 move-healing that follows any plain move.
        Assert.InRange(map.Player.CombatComponent!.Wounds, 17, 18);
        Assert.Equal(TrapState.Spent, trap.State);
        Assert.True(game.EffectsSystem.Shake);
        Assert.Contains(game.Messages, m => m.Contains("spike trap"));
        _ = Assert.Single(map.Decorations[5, 4], d => d.Character == "^");
    }

    [Fact]
    public void TrapDamageIgnoresArmourSoak()
    {
        var game = new Game();
        var map = MapWithTrap(game, new Trap(SpikeTrap()), armour: 10);

        _ = map.TakeTurn(new PlayerAction.Move(Direction.East));

        // 3 damage against 10 armour would be soaked to nothing if it went through ApplyDamage.
        Assert.InRange(map.Player.CombatComponent!.Wounds, 17, 18);
        Assert.Contains(game.Messages, m => m.Contains("You take 3 damage"));
    }

    [Fact]
    public void ASpentTrapDoesNotFireAgain()
    {
        var game = new Game();
        var trap = new Trap(SpikeTrap());
        var map = MapWithTrap(game, trap);
        _ = map.TakeTurn(new PlayerAction.Move(Direction.East));
        _ = map.TakeTurn(new PlayerAction.Move(Direction.West));

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.Null(result.TrapTriggered);
        _ = Assert.Single(game.Messages, m => m.Contains("You trigger"));
    }

    [Fact]
    public void AReusableTrapStaysArmedAndFiresEveryTime()
    {
        var game = new Game();
        var trap = new Trap(SpikeTrap(reusable: true));
        var map = MapWithTrap(game, trap);
        _ = map.TakeTurn(new PlayerAction.Move(Direction.East));
        _ = map.TakeTurn(new PlayerAction.Move(Direction.West));

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.Same(trap.Type, result.TrapTriggered);
        Assert.Equal(2, game.Messages.Count(m => m.Contains("You trigger")));
        Assert.Equal(TrapState.Revealed, trap.State);
    }

    [Fact]
    public void ATrapThatKillsThePlayerEndsTheGame()
    {
        var game = new Game();
        var map = MapWithTrap(game, new Trap(SpikeTrap()), wounds: 3);

        var result = map.TakeTurn(new PlayerAction.Move(Direction.East));

        Assert.True(result.GameOverThisTurn);
        Assert.True(map.IsGameOver);
        Assert.Equal("a spike trap", map.CauseOfDeath);

        // The killed message closes the story rather than coming before the damage that caused it.
        Assert.Equal("You were killed!", game.Messages[^1]);
        Assert.Contains(game.Messages.SkipLast(1), m => m.Contains("You take 3 damage"));
    }

    [Fact]
    public void AHiddenTrapProducesNoDecoration()
    {
        var game = new Game();
        _ = MapWithTrap(game, new Trap(SpikeTrap(startsHidden: true)));

        Assert.Empty(game.Map.Decorations[5, 4]);
    }

    [Fact]
    public void AnAlwaysVisibleTrapIsDrawnWithItsSpriteGlyphAndTooltip()
    {
        var game = new Game();
        _ = MapWithTrap(game, new Trap(SpikeTrap(startsHidden: false)));

        var decoration = Assert.Single(game.Map.Decorations[5, 4]);
        Assert.Equal("trap", decoration.ImageName);
        Assert.Equal("^", decoration.Character);
        Assert.Equal("A spike trap, set into the floor", decoration.GameObject.InfoText);
    }
}
