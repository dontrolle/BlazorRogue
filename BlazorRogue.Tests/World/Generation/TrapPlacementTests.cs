using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

/// <summary>
/// Covers MapGeneratorBase.AddTraps' placement rules - an opt-in percentage knob, and traps only on
/// plain unoccupied floor away from the start and the stairs - plus Map.IsChokepoint, which keeps
/// reusable traps out of corridors and doorways.
/// </summary>
public class TrapPlacementTests
{
    const int MinDistance = 4;

    static LevelConfiguration LevelWithSettings(SettingsMap settingsMap) =>
        new(
            number: 0,
            id: "trap-placement-test-level",
            name: "Trap Placement Test Level",
            height: 40,
            width: 40,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    // Plenty of other placed objects, so the eligibility rules have something to steer around.
    static SettingsMap SettingsWith(Dictionary<string, object> common) =>
        new(new Dictionary<string, object> { ["common"] = new SettingsMap(common) });

    static SettingsMap CrowdedLevelWithTrapChance(double chance) =>
        SettingsWith(
            new Dictionary<string, object>
            {
                ["percentage_chance_of_traps"] = chance,
                ["trap_min_distance_from_start"] = MinDistance,
                ["percentage_chance_of_statues"] = 0.1,
                ["percentage_chance_of_fountains"] = 0.1,
                ["percentage_chance_of_chests"] = 0.1,
                ["percentage_chance_of_barrels"] = 0.1,
                ["percentage_chance_of_items"] = 0.1,
                ["percentage_chance_of_door"] = 1.0,
            }
        );

    static List<(int X, int Y, Trap Trap)> TrapsOn(Map map)
    {
        var traps = new List<(int, int, Trap)>();
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Tiles[x, y].Trap is { } trap)
                {
                    traps.Add((x, y, trap));
                }
            }
        }
        return traps;
    }

    [Fact]
    public void NoTrapsArePlacedUnlessTheLevelOptsIn()
    {
        var game = new Game();
        var level = LevelWithSettings(SettingsMap.Empty);

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        Assert.Empty(TrapsOn(map));
    }

    [Fact]
    public void ACertainChancePlacesTrapsOfAConfiguredTypeOnly()
    {
        var game = new Game();
        var level = LevelWithSettings(CrowdedLevelWithTrapChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var traps = TrapsOn(map);
        Assert.NotEmpty(traps);
        Assert.All(traps, t => Assert.Contains(t.Trap.Type, game.Configuration.TrapTypes));
        Assert.All(
            traps,
            t =>
                Assert.Equal(
                    t.Trap.Type.StartsHidden ? TrapState.Hidden : TrapState.Revealed,
                    t.Trap.State
                )
        );
    }

    [Fact]
    public void TrapsOnlyLandOnPlainUnoccupiedFloor()
    {
        var game = new Game();
        var level = LevelWithSettings(CrowdedLevelWithTrapChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var traps = TrapsOn(map);
        Assert.NotEmpty(traps);
        Assert.All(
            traps,
            t =>
            {
                Assert.Equal(TileType.Floor, map.Tiles[t.X, t.Y].TileType);
                Assert.False(map.Tiles[t.X, t.Y].Blocking);
                Assert.DoesNotContain(
                    map.GameObjectByCoord[t.X, t.Y],
                    g =>
                        g is Door or Stair or Item or Chest or Statue or Fountain
                        || g.Blocking
                        || g.OccupiesTile
                );
                Assert.DoesNotContain(map.Moveables, m => m.X == t.X && m.Y == t.Y);
            }
        );
    }

    [Fact]
    public void TrapsKeepTheirDistanceFromThePlayerAndTheStairs()
    {
        var game = new Game();
        var level = LevelWithSettings(CrowdedLevelWithTrapChance(1.0));

        var map = MapGeneratorFactory.Create(level, game).GenerateMap();

        var keepClear = map
            .GameObjects.OfType<Stair>()
            .Select(s => (s.X, s.Y))
            .Append((map.Player.X, map.Player.Y))
            .ToList();
        Assert.True(keepClear.Count > 1, "Expected the level to have at least one stair.");

        Assert.All(
            TrapsOn(map),
            t =>
                Assert.All(
                    keepClear,
                    p =>
                        Assert.True(
                            Math.Max(Math.Abs(p.X - t.X), Math.Abs(p.Y - t.Y)) >= MinDistance,
                            $"Trap at ({t.X},{t.Y}) is too close to ({p.X},{p.Y})."
                        )
                )
        );
    }

    // --- Map.IsChokepoint --------------------------------------------------------------------

    // Walls everywhere except the floor tiles listed, e.g. a corridor or a room.
    static Map MapWithFloorAt(params (int X, int Y)[] floorTiles)
    {
        var wallSet = new TileSet("w", TileType.Wall, "w", [0]);
        var floorSet = new TileSet("f", TileType.Floor, "f", [0]);
        var map = new Map(10, 10, wallSet, game: null!);
        foreach (var (x, y) in floorTiles)
        {
            map.Tiles[x, y].TileSet = floorSet;
            map.Tiles[x, y].Blocking = false;
        }
        return map;
    }

    static (int, int)[] Room(int x0, int y0, int x1, int y1) =>
        [
            .. from x in Enumerable.Range(x0, x1 - x0 + 1)
            from y in Enumerable.Range(y0, y1 - y0 + 1)
            select (x, y),
        ];

    [Fact]
    public void ACorridorTileIsAChokepoint()
    {
        var map = MapWithFloorAt([.. Room(1, 5, 8, 5)]);

        Assert.True(map.IsChokepoint(4, 5));
    }

    [Fact]
    public void ADoorwayBetweenTwoRoomsIsAChokepoint()
    {
        var map = MapWithFloorAt([.. Room(1, 1, 3, 3), (4, 2), .. Room(5, 1, 7, 3)]);

        Assert.True(map.IsChokepoint(4, 2));
    }

    [Fact]
    public void OpenRoomTilesAreNotChokepoints()
    {
        var map = MapWithFloorAt(Room(1, 1, 6, 6));

        Assert.False(map.IsChokepoint(3, 3));
        Assert.False(map.IsChokepoint(1, 1));
        Assert.False(map.IsChokepoint(3, 1));
    }

    [Fact]
    public void ADeadEndIsNotAChokepoint()
    {
        var map = MapWithFloorAt([.. Room(1, 5, 5, 5)]);

        Assert.False(map.IsChokepoint(5, 5));
    }
}
