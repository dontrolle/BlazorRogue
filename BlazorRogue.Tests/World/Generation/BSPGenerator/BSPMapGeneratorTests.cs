using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation.BSPGenerator;

/// <summary>
/// End-to-end tests for <c>BSPMapGenerator</c>: the BSP plan (split -> carve -> connect) being
/// transferred onto a real <see cref="Map"/>, wall ring and all, plus the data-driven
/// <c>layout</c> knobs (early exit, split centring, roomless leaves, room-carver pool). The plan
/// layer itself is covered by BspLayoutTests / NodeTests / RoomCarverTests - these check the
/// paint-onto-the-map pass and the settings wiring.
/// </summary>
public class BSPMapGeneratorTests
{
    const string BspId = "bsp_map_generator";

    static LevelConfiguration Level(int width, int height, SettingsMap settings) =>
        new(
            number: 0,
            id: "test-level",
            name: "Test Level",
            height: height,
            width: width,
            generatorId: BspId,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settings
        );

    static Map GenerateMap(int width = 60, int height = 40) =>
        GenerateMap(SettingsMap.Empty, width, height);

    static Map GenerateMap(SettingsMap settings, int width = 60, int height = 40) =>
        MapGeneratorFactory.Create(Level(width, height, settings), new Game()).GenerateMap();

    /// <summary>Wraps a <c>layout</c> parameter block the way levels.json nests it.</summary>
    static SettingsMap LayoutSettings(Dictionary<string, object> layout) =>
        new(new Dictionary<string, object> { ["layout"] = new SettingsMap(layout) });

    static bool IsFloor(Map map, int x, int y) => map.Tiles[x, y].TileType == TileType.Floor;

    static List<(int X, int Y)> FloorCells(Map map)
    {
        var cells = new List<(int X, int Y)>();
        map.ForEachTile(
            (x, y) =>
            {
                if (IsFloor(map, x, y))
                {
                    cells.Add((x, y));
                }
            }
        );
        return cells;
    }

    static void AssertEveryFloorTileIsEnclosed(Map map) =>
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

    static void AssertEveryFloorTileReachableFromPlayer(Map map)
    {
        var start = (map.Player.X, map.Player.Y);
        Assert.True(IsFloor(map, start.Item1, start.Item2));

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

        foreach (var cell in FloorCells(map))
        {
            Assert.Contains(cell, reached);
        }
    }

    [Fact]
    public void GeneratesANonTrivialAmountOfFloor()
    {
        var map = GenerateMap();

        Assert.True(
            FloorCells(map).Count > 100,
            $"Expected a carved dungeon, got only {FloorCells(map).Count} floor tiles."
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
        AssertEveryFloorTileIsEnclosed(GenerateMap());
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
        AssertEveryFloorTileReachableFromPlayer(GenerateMap());
    }

    [Fact]
    public void PlacesStairsOnFloorTiles()
    {
        // level 0's only neighbour here is level 1, so AddStairs adds a single down stair; the
        // base class guarantees it lands on an unblocked tile, which here means floor.
        var map = GenerateMap();

        var stairs = map.GameObjects.OfType<Stair>().ToList();
        Assert.NotEmpty(stairs);
        Assert.All(stairs, stair => Assert.True(IsFloor(map, stair.X, stair.Y)));
    }

    [Fact]
    public void RectangularOverlaidAndCircularPoolStaysFullyConnected()
    {
        // These three carvers all produce a contiguous footprint with the connector point on it,
        // so a plan built from any mix of them must come out fully connected. (Cave is excluded
        // here - its automaton can leave disconnected floor pockets; see the cave test below.)
        var settings = LayoutSettings(
            new Dictionary<string, object>
            {
                ["room_carvers"] = new List<(string, double)>
                {
                    ("rectangular", 1),
                    ("overlaid", 1),
                    ("circular", 1),
                },
            }
        );

        for (int i = 0; i < 5; i++)
        {
            var map = GenerateMap(settings, width: 72, height: 48);
            AssertEveryFloorTileIsEnclosed(map);
            AssertEveryFloorTileReachableFromPlayer(map);
        }
    }

    [Fact]
    public void CaveOnlyPoolGeneratesWithoutThrowingAndStaysEnclosed()
    {
        // CaveRoomCarver can wall a small leaf off entirely - BSPMapGenerator wraps it in a
        // rectangle fallback (FallbackRoomCarver) so that never aborts generation. Repeated over
        // fresh unseeded layouts. Full connectivity isn't asserted: a cave can also leave a
        // reachable-looking but disconnected pocket, which is inherent to the shape.
        var settings = LayoutSettings(
            new Dictionary<string, object>
            {
                ["room_carvers"] = new List<(string, double)> { ("cave", 1) },
            }
        );

        for (int i = 0; i < 8; i++)
        {
            var map = GenerateMap(settings, width: 72, height: 48);
            Assert.True(FloorCells(map).Count > 50);
            Assert.True(IsFloor(map, map.Player.X, map.Player.Y));
            AssertEveryFloorTileIsEnclosed(map);
        }
    }

    [Fact]
    public void UnknownRoomCarverIdIsRejectedWithAHelpfulMessage()
    {
        var settings = LayoutSettings(
            new Dictionary<string, object>
            {
                ["room_carvers"] = new List<(string, double)> { ("hexagonal", 1) },
            }
        );

        var ex = Assert.Throws<InvalidOperationException>(() => GenerateMap(settings));
        Assert.Contains("hexagonal", ex.Message);
        Assert.Contains("room_carvers", ex.Message);
    }

    [Fact]
    public void EarlyExitChanceOfOneProducesASingleSolidRectangularRoom()
    {
        // early_exit_chance 1.0 stops the partition at the root, so the plan is one leaf: a single
        // rectangular room, no corridors. Its footprint must therefore be a filled rectangle.
        var settings = LayoutSettings(
            new Dictionary<string, object> { ["early_exit_chance"] = 1.0 }
        );

        var map = GenerateMap(settings);
        var floor = FloorCells(map);

        Assert.NotEmpty(floor);
        int minX = floor.Min(c => c.X);
        int maxX = floor.Max(c => c.X);
        int minY = floor.Min(c => c.Y);
        int maxY = floor.Max(c => c.Y);
        Assert.Equal((maxX - minX + 1) * (maxY - minY + 1), floor.Count);
    }

    [Fact]
    public void RoomlessLeavesDoNotDisconnectTheMap()
    {
        // chance_of_leaf_having_no_room leaves gaps in the plan; ConnectRooms must still stitch
        // every remaining room into one component.
        var settings = LayoutSettings(
            new Dictionary<string, object> { ["chance_of_leaf_having_no_room"] = 0.4 }
        );

        for (int i = 0; i < 5; i++)
        {
            AssertEveryFloorTileReachableFromPlayer(GenerateMap(settings, width: 72, height: 48));
        }
    }
}
