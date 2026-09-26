using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests.AI;

public class RandomWalkAIComponentTests
{
    // Wired to a real Game (not game: null!) because AIComponent.Wake() reaches
    // References.Game.AddMessage - same technique as MapTests.BareFloorMap.
    static Map BareFloorMap(int size = 10)
    {
        var game = new Game();
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

    static Moveable NewCreature(Map map, int x, int y, out RandomWalkAIComponent ai, Random random)
    {
        ai = new RandomWalkAIComponent(map, random);
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
            singular: true
        );
        var moveable = new Moveable(x, y, ai, type);
        map.AddMoveable(moveable);
        return moveable;
    }

    static (int x, int y) TakeAwakeTurn(Map map, int seed)
    {
        var monster = NewCreature(map, 5, 5, out var ai, new Random(seed));
        ai.Wake();
        _ = ai.TakeTurn();
        return (monster.X, monster.Y);
    }

    [Fact]
    public void TakeTurnIsDeterministicGivenTheSameSeed()
    {
        var first = TakeAwakeTurn(BareFloorMap(), seed: 12345);
        var second = TakeAwakeTurn(BareFloorMap(), seed: 12345);

        Assert.Equal(first, second);
    }

    [Fact]
    public void TakeTurnMovesAtMostOneTileInEachDirection()
    {
        var map = BareFloorMap();
        var (x, y) = TakeAwakeTurn(map, seed: 42);

        Assert.InRange(x, 4, 6);
        Assert.InRange(y, 4, 6);
    }

    [Fact]
    public void TakeTurnDoesNothingWhileAsleep()
    {
        var map = BareFloorMap();
        var monster = NewCreature(map, 5, 5, out var ai, new Random(1));

        _ = ai.TakeTurn();

        Assert.Equal((5, 5), (monster.X, monster.Y));
    }
}
