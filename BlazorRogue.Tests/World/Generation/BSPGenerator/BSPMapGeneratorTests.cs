using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation.BSPGenerator;

/// <summary>
/// End-to-end tests for <c>BSPMapGenerator</c>: the BSP plan (split -> carve -> connect) being
/// transferred onto a real <see cref="Map"/>, wall ring and all. The plan layer itself is covered
/// by BspLayoutTests / NodeTests / RoomCarverTests - these check the paint-onto-the-map pass.
/// </summary>
public class BSPMapGeneratorTests
{
    const string BspId = "bsp_map_generator";

    static LevelConfiguration Level(int width, int height) =>
        new(
            number: 0,
            id: "test-level",
            name: "Test Level",
            height: height,
            width: width,
            generatorId: BspId,
            backgroundSoundtrack: "test.mp3",
            settingsMap: SettingsMap.Empty
        );

    static Map GenerateMap(int width = 60, int height = 40)
    {
        var game = new Game();
        return MapGeneratorFactory.Create(Level(width, height), game).GenerateMap();
    }

    static bool IsFloor(Map map, int x, int y) => map.Tiles[x, y].TileType == TileType.Floor;

    [Fact]
    public void GeneratesANonTrivialAmountOfFloor()
    {
        var map = GenerateMap();

        int floorCount = 0;
        map.ForEachTile(
            (x, y) =>
            {
                if (IsFloor(map, x, y))
                {
                    floorCount++;
                }
            }
        );

        Assert.True(
            floorCount > 100,
            $"Expected a carved dungeon, got only {floorCount} floor tiles."
        );
    }

    [Fact]
    public void NoFloorTileSitsOnTheMapPerimeter()
    {
        var map = GenerateMap();

        for (int x = 0; x < map.Width; x++)
        {
            Assert.False(IsFloor(map, x, 0));
            Assert.False(IsFloor(map, x, map.Height - 1));
        }
        for (int y = 0; y < map.Height; y++)
        {
            Assert.False(IsFloor(map, 0, y));
            Assert.False(IsFloor(map, map.Width - 1, y));
        }
    }

    [Fact]
    public void EveryFloorTileIsFullyEnclosedByFloorOrWall()
    {
        // The wall ring pass must leave no floor cell touching the void - otherwise light/vision
        // and the decoration passes leak into unpainted space.
        var map = GenerateMap();

        map.ForEachTile(
            (x, y) =>
            {
                if (!IsFloor(map, x, y))
                {
                    return;
                }

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Assert.NotEqual(TileType.Black, map.Tiles[x + dx, y + dy].TileType);
                    }
                }
            }
        );
    }

    [Fact]
    public void PlayerStartsOnAFloorTile()
    {
        var map = GenerateMap();

        Assert.True(IsFloor(map, map.Player.X, map.Player.Y));
    }

    [Fact]
    public void EveryFloorTileIsReachableFromThePlayerStart()
    {
        // The payoff of wiring in ConnectRooms: flood-fill over floor cells from the player's
        // start tile must reach every floor cell on the map.
        var map = GenerateMap();

        var start = (X: map.Player.X, Y: map.Player.Y);
        var reached = new HashSet<(int X, int Y)> { start };
        var frontier = new Queue<(int X, int Y)>();
        frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            var (x, y) = frontier.Dequeue();
            (int X, int Y)[] neighbours = [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)];
            foreach (var n in neighbours)
            {
                if (IsFloor(map, n.X, n.Y) && reached.Add(n))
                {
                    frontier.Enqueue(n);
                }
            }
        }

        map.ForEachTile(
            (x, y) =>
            {
                if (IsFloor(map, x, y))
                {
                    Assert.Contains((x, y), reached);
                }
            }
        );
    }

    [Fact]
    public void PlacesUpAndDownStairsOnFloorTiles()
    {
        // level 0 has both a level -? (none) and a level 1, so AddStairs adds a single down stair;
        // the base class guarantees it lands on an unblocked tile, which here means floor.
        var map = GenerateMap();

        var stairs = map.GameObjects.OfType<Stair>().ToList();
        Assert.NotEmpty(stairs);
        Assert.All(stairs, stair => Assert.True(IsFloor(map, stair.X, stair.Y)));
    }
}
