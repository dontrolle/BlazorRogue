using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;
using BlazorRogue.World.Generation.BSPGenerator;

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

    static LevelConfiguration Level(int width, int height, SettingsMap settings, int number) =>
        new(
            number: number,
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

    static Map GenerateMap(SettingsMap settings, int width = 60, int height = 40, int number = 0) =>
        MapGeneratorFactory
            .Create(Level(width, height, settings, number), new Game())
            .GenerateMap();

    /// <summary>Wraps a <c>layout</c> parameter block the way levels.json nests it.</summary>
    static SettingsMap LayoutSettings(Dictionary<string, object> layout) =>
        new(new Dictionary<string, object> { ["layout"] = new SettingsMap(layout) });

    /// <summary>Wraps a <c>common</c> and/or <c>layout</c> block the way levels.json nests them.</summary>
    static SettingsMap Settings(
        Dictionary<string, object>? common = null,
        Dictionary<string, object>? layout = null
    )
    {
        var root = new Dictionary<string, object>();
        if (common is not null)
        {
            root["common"] = new SettingsMap(common);
        }
        if (layout is not null)
        {
            root["layout"] = new SettingsMap(layout);
        }
        return new SettingsMap(root);
    }

    /// <summary>Like <see cref="GenerateMap(SettingsMap, int, int, int)"/> but also hands back the
    /// concrete generator, for assertions against its carved rooms / player room.</summary>
    static (BSPMapGenerator Gen, Map Map) GenerateWithGen(
        SettingsMap settings,
        int width = 72,
        int height = 48,
        int number = 0
    )
    {
        var generator = MapGeneratorFactory.Create(
            Level(width, height, settings, number),
            new Game()
        );
        var map = generator.GenerateMap();
        return ((BSPMapGenerator)generator, map);
    }

    static bool RoomCovers(Room room, int x, int y) =>
        room.FootprintAreas.Any(a => x >= a.XMin && x < a.XMax && y >= a.YMin && y < a.YMax);

    static bool InAnyCarvedRoom(BSPMapGenerator gen, int x, int y) =>
        gen.CarvedRooms.Any(r => RoomCovers(r, x, y));

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

    static HashSet<(int X, int Y)> FloodFillFloorFrom(Map map, (int X, int Y) start)
    {
        var reached = new HashSet<(int X, int Y)>();
        if (!IsFloor(map, start.X, start.Y))
        {
            return reached;
        }

        _ = reached.Add(start);
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
        return reached;
    }

    static void AssertEveryFloorTileReachableFromPlayer(Map map)
    {
        Assert.True(IsFloor(map, map.Player.X, map.Player.Y));

        var reached = FloodFillFloorFrom(map, (map.Player.X, map.Player.Y));
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
        // number 0's only neighbour in levels.json is level 1, so AddStairs adds a single down
        // stair; it must land on floor (a room connector).
        var map = GenerateMap();

        var stairs = map.GameObjects.OfType<Stair>().ToList();
        Assert.NotEmpty(stairs);
        Assert.All(stairs, stair => Assert.True(IsFloor(map, stair.X, stair.Y)));
    }

    [Fact]
    public void PlayerSpawnsFarFromTheDownStair()
    {
        // ChooseKeyRooms puts the player and the down-stair in the farthest-apart pair of rooms,
        // so a fresh level always has a real traversal from entrance to exit.
        for (int i = 0; i < 5; i++)
        {
            var map = GenerateMap(width: 72, height: 48);
            var down = map.GetStair(StairDirection.Down);

            int dx = map.Player.X - down.X;
            int dy = map.Player.Y - down.Y;
            Assert.True(
                (dx * dx) + (dy * dy) > 20 * 20,
                $"player ({map.Player.X},{map.Player.Y}) too close to down-stair ({down.X},{down.Y})"
            );
        }
    }

    [Fact]
    public void UpAndDownStairsLandInDifferentRoomsFarApart()
    {
        // number 1 in levels.json has both a level 0 and a level 2, so both stairs are placed -
        // in the two farthest-apart rooms.
        for (int i = 0; i < 5; i++)
        {
            var map = GenerateMap(SettingsMap.Empty, width: 72, height: 48, number: 1);
            var up = map.GetStair(StairDirection.Up);
            var down = map.GetStair(StairDirection.Down);

            Assert.True(IsFloor(map, up.X, up.Y));
            Assert.True(IsFloor(map, down.X, down.Y));
            Assert.False(up.X == down.X && up.Y == down.Y);

            int dx = up.X - down.X;
            int dy = up.Y - down.Y;
            Assert.True((dx * dx) + (dy * dy) > 20 * 20);
        }
    }

    [Fact]
    public void StairsStayReachableWithCaveHeavyRooms()
    {
        // Regression guard for the Phase 1.5 soft-lock note: stairs go in room connectors, every
        // room connector is on the single connected component, and CaveRoomCarver now walls off
        // disconnected pockets - so a stair can never end up stranded, even in an all-cave pool.
        var settings = LayoutSettings(
            new Dictionary<string, object>
            {
                ["room_carvers"] = new List<(string, double)> { ("cave", 1), ("rectangular", 1) },
            }
        );

        for (int i = 0; i < 6; i++)
        {
            var map = GenerateMap(settings, width: 72, height: 48);
            var down = map.GetStair(StairDirection.Down);

            var reached = FloodFillFloorFrom(map, (map.Player.X, map.Player.Y));
            Assert.Contains((down.X, down.Y), reached);
        }
    }

    [Fact]
    public void PlacesDoorsWhereCorridorsPierceRooms()
    {
        // Every corridor crossing a room's wall ring leaves a one-tile gap; RecordDoorCandidates
        // flags it and the base AddDoors pass fills it. A multi-room BSP map always has some.
        for (int i = 0; i < 5; i++)
        {
            var map = GenerateMap(width: 72, height: 48);

            Assert.NotEmpty(map.GameObjects.OfType<Door>());
        }
    }

    [Fact]
    public void EveryDoorSitsInAOneTileGapInAWall()
    {
        // A door must be on floor with wall on exactly one axis and open passage on the other -
        // a real threshold, never mid-room or mid-corridor.
        var map = GenerateMap(width: 72, height: 48);
        var doors = map.GameObjects.OfType<Door>().ToList();

        Assert.NotEmpty(doors);
        foreach (var door in doors)
        {
            Assert.True(IsFloor(map, door.X, door.Y));

            bool wallOnXAxis =
                map.Tiles[door.X - 1, door.Y].TileType == TileType.Wall
                && map.Tiles[door.X + 1, door.Y].TileType == TileType.Wall;
            bool wallOnYAxis =
                map.Tiles[door.X, door.Y - 1].TileType == TileType.Wall
                && map.Tiles[door.X, door.Y + 1].TileType == TileType.Wall;

            Assert.True(
                wallOnXAxis ^ wallOnYAxis,
                $"Door at ({door.X},{door.Y}) is not in a clean one-tile wall gap."
            );
        }
    }

    [Fact]
    public void NoTwoDoorsShareATile()
    {
        var map = GenerateMap(width: 72, height: 48);

        var doorCells = map.GameObjects.OfType<Door>().Select(d => (d.X, d.Y)).ToList();

        Assert.Equal(doorCells.Count, doorCells.Distinct().Count());
    }

    [Fact]
    public void DoorsLeaveEveryFloorTileReachable()
    {
        // Doors are game objects on floor tiles - TileType stays Floor - so a closed door never
        // severs the map even though it blocks movement until opened.
        AssertEveryFloorTileReachableFromPlayer(GenerateMap(width: 72, height: 48));
    }

    [Fact]
    public void PercentageChanceOfDoorGatesHowManyCandidatesBecomeDoors()
    {
        // common.percentage_chance_of_door is an independent per-candidate roll in the base
        // AddDoors pass. 0 => no doors at all; a fraction => strictly fewer than the "every
        // candidate" default. Pooled over several fresh layouts so the ordering is not a
        // coin-flip (it's not a proportionality assertion - candidate counts vary between maps).
        int DoorsAcrossMaps(double chance)
        {
            var settings = new SettingsMap(
                new Dictionary<string, object>
                {
                    ["common"] = new SettingsMap(
                        new Dictionary<string, object> { ["percentage_chance_of_door"] = chance }
                    ),
                }
            );

            int total = 0;
            for (int i = 0; i < 8; i++)
            {
                total += GenerateMap(settings, width: 72, height: 48)
                    .GameObjects.OfType<Door>()
                    .Count();
            }
            return total;
        }

        Assert.Equal(0, DoorsAcrossMaps(0.0));

        int every = DoorsAcrossMaps(1.0);
        int some = DoorsAcrossMaps(0.3);

        Assert.True(every > 0);
        Assert.InRange(some, 1, every - 1);
    }

    [Fact]
    public void AllRoomCarverShapesStayFullyConnected()
    {
        // Every shape - rectangular, overlaid, circular, and cave (whose carver now walls off any
        // pocket cut off from its connector) - produces a contiguous footprint, so a plan mixing
        // them freely still comes out fully reachable once painted.
        var settings = LayoutSettings(
            new Dictionary<string, object>
            {
                ["room_carvers"] = new List<(string, double)>
                {
                    ("rectangular", 1),
                    ("overlaid", 1),
                    ("circular", 1),
                    ("cave", 1),
                },
            }
        );

        for (int i = 0; i < 6; i++)
        {
            var map = GenerateMap(settings, width: 72, height: 48);
            AssertEveryFloorTileIsEnclosed(map);
            AssertEveryFloorTileReachableFromPlayer(map);
        }
    }

    [Fact]
    public void CaveOnlyPoolStaysFullyConnectedAndNeverAbortsGeneration()
    {
        // Pushes the cave path hard: every leaf rolls cave, so both the small-leaf rectangle
        // fallback (FallbackRoomCarver) and the pocket wall-off run on essentially every map. If
        // the carver ever threw, this test would error rather than fail.
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
            AssertEveryFloorTileIsEnclosed(map);
            AssertEveryFloorTileReachableFromPlayer(map);
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
        // min_room_width/height are pinned large so the lone room fills enough of the map for the
        // base AddMonsters pass (which brute-forces random unblocked tiles) not to time out - a
        // pre-existing fragility that Track A1's room-aware AddMonsters override will remove.
        var settings = LayoutSettings(
            new Dictionary<string, object>
            {
                ["early_exit_chance"] = 1.0,
                ["min_room_width"] = 40,
                ["min_room_height"] = 28,
            }
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

    // ---- Track A1: room-aware AddMonsters ----

    [Fact]
    public void MonstersSpawnOnFloorInsideRoomsAndNeverOnBlockingObjects()
    {
        for (int i = 0; i < 3; i++)
        {
            var (gen, map) = GenerateWithGen(SettingsMap.Empty, width: 72, height: 48);

            Assert.NotEmpty(map.Monsters);
            foreach (var monster in map.Monsters)
            {
                Assert.True(
                    IsFloor(map, monster.X, monster.Y),
                    $"monster at ({monster.X},{monster.Y}) is not on a floor tile"
                );
                Assert.True(
                    InAnyCarvedRoom(gen, monster.X, monster.Y),
                    $"monster at ({monster.X},{monster.Y}) is outside every carved room footprint"
                );

                var here = map.GameObjectByCoord[monster.X, monster.Y];
                Assert.DoesNotContain(here, o => o is Door);
                Assert.DoesNotContain(here, o => o.Blocking);
            }
        }
    }

    [Fact]
    public void NoTwoMonstersShareATile()
    {
        var map = GenerateMap(width: 72, height: 48);

        var cells = map.Monsters.Select(m => (m.X, m.Y)).ToList();

        Assert.Equal(cells.Count, cells.Distinct().Count());
    }

    [Fact]
    public void MonsterCountScalesWithMapSize()
    {
        // Budget is per-room floor area * density, so a bigger map (more / larger rooms) carries
        // strictly more monsters. Pooled over a few layouts so the ordering isn't a coin-flip.
        int TotalOver(int width, int height)
        {
            int total = 0;
            for (int i = 0; i < 3; i++)
            {
                total += GenerateMap(width: width, height: height).Monsters.Count();
            }
            return total;
        }

        Assert.True(
            TotalOver(96, 64) > TotalOver(44, 30),
            "a larger map should carry more monsters than a small one"
        );
    }

    [Fact]
    public void ZeroDensityProducesNoMonsters()
    {
        var settings = Settings(common: new() { ["monsters_per_100_tiles"] = 0.0 });

        for (int i = 0; i < 3; i++)
        {
            Assert.Empty(GenerateMap(settings, width: 72, height: 48).Monsters);
        }
    }

    [Fact]
    public void EmptyRoomChanceOfOneLeavesEveryRoomEmpty()
    {
        var settings = Settings(layout: new() { ["empty_room_chance"] = 1.0 });

        for (int i = 0; i < 3; i++)
        {
            Assert.Empty(GenerateMap(settings, width: 72, height: 48).Monsters);
        }
    }

    [Fact]
    public void HighDensityBeatsTheBaseFlatCountAndKeepsEveryMonsterInARoom()
    {
        var settings = Settings(
            common: new() { ["monsters_per_100_tiles"] = 40.0 },
            layout: new() { ["empty_room_chance"] = 0.0, ["player_room_monster_multiplier"] = 1.0 }
        );

        var (gen, map) = GenerateWithGen(settings, width: 72, height: 48);

        Assert.True(
            map.Monsters.Count() > 10,
            "high density should place more than the base generator's flat 10"
        );
        foreach (var monster in map.Monsters)
        {
            Assert.True(
                InAnyCarvedRoom(gen, monster.X, monster.Y),
                $"monster at ({monster.X},{monster.Y}) spawned outside any room"
            );
        }
    }

    [Fact]
    public void PlayerStartRoomStaysMonsterFreeByDefault()
    {
        // Default player_room_monster_multiplier is 0 - the room you spawn in never gets monsters.
        for (int i = 0; i < 4; i++)
        {
            var (gen, map) = GenerateWithGen(SettingsMap.Empty, width: 72, height: 48);

            Assert.NotNull(gen.PlayerRoom);
            foreach (var monster in map.Monsters)
            {
                Assert.False(
                    RoomCovers(gen.PlayerRoom!, monster.X, monster.Y),
                    $"monster at ({monster.X},{monster.Y}) spawned in the player's start room"
                );
            }
        }
    }
}
