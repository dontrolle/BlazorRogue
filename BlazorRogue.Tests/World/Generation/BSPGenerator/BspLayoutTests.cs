using BlazorRogue.World.Generation;
using BlazorRogue.World.Generation.BSPGenerator;
using Xunit.Abstractions;

namespace BlazorRogue.Tests.World.Generation.BSPGenerator;

/// <summary>
/// Harness for the BSP room-carving and corridor-connection passes.
/// <para>
/// <see cref="PrintCarvedPlanForManualInspection"/> is the visual feedback loop: run it with
/// <c>dotnet test --filter DisplayName~PrintCarvedPlan --logger "console;verbosity=detailed"</c>
/// to eyeball the <see cref="Node.ToAsciiMap"/> render while tweaking carving. (Filtering on
/// <c>~Print</c> alone would also pull in NodeTests' own manual-inspection test.)
/// </para>
/// </summary>
public class BspLayoutTests(ITestOutputHelper output)
{
    // Mirrors BSPMapGenerator.AreaThreshold / MinSplit.
    const int Threshold = 15;
    const int MinSplit = 6;
    const int Margin = 0;
    const int MinRoomWidth = 3;
    const int MinRoomHeight = 3;
    const double EarlyExitChance = 0.05;

    //const double maxSplitOffsetFromCenterProportion = 0.25;

    /// <summary>
    /// Builds a plan for a <paramref name="width"/> x <paramref name="height"/> map from a fixed
    /// <paramref name="seed"/>, so every step is deterministic and replayable.
    /// </summary>
    static Node CarvedPlan(
        int width,
        int height,
        int seed,
        double chanceOfLeafHavingNoRoom = 0,
        Func<Node, IRoomCarver, Random, IRoomCarver>? selectCarver = null
    )
    {
        var root = new Node(new Area(0, width, 0, height));
        root.SplitUntilThreshold(
            Threshold,
            MinSplit,
            new Random(seed),
            earlyExitChance: EarlyExitChance
        );

        // Each pass gets its own fresh seeded Random so changing one pass doesn't shift the
        // random stream the next one observes.
        root.CarveRooms(
            Margin,
            MinRoomWidth,
            MinRoomHeight,
            new Random(seed),
            chanceOfLeafHavingNoRoom,
            selectCarver: selectCarver
        );
        var _ = root.ConnectRooms(new Random(seed));

        return root;
    }

    // Not a real test - see the class summary for how to run it.
    [Fact]
    public void PrintCarvedPlanForManualInspection()
    {
        // At this fixed seed/threshold/minSplit: node {1} is the whole left half of the map, given
        // OverlaidRectanglesRoomCarver throughout; node {14} is a 9-node/5-leaf subtree on the
        // right given CaveRoomCarver; node {58} is a 3-leaf subtree in the top-right corner given
        // CircularRoomCarver. CaveRoomCarver needs leaves with a bit of room to work with - its
        // cellular automaton can wall a leaf off entirely if both dimensions are small (under ~8),
        // which is why it's pointed at {14} rather than a leafier, smaller subtree. Re-run with
        // DisplayName~PrintCarvedPlan to re-derive node ids if the split parameters change.
        Func<Node, IRoomCarver, Random, IRoomCarver> selectCarver = (node, inherited, _) =>
            node.Id switch
            {
                1 => OverlaidRectanglesRoomCarver.Instance,
                14 => new CaveRoomCarver(),
                58 => CircularRoomCarver.Instance,
                _ => inherited,
            };

        var root = CarvedPlan(
            width: 80,
            height: 50,
            seed: 1,
            chanceOfLeafHavingNoRoom: 0.1,
            selectCarver: selectCarver
        );

        output.WriteLine(root.ToTreeString());
        // Leading newline: the xUnit console logger indents the first physical line of a
        // WriteLine payload one column further than the rest, which would skew the grid's top
        // row. Starting on a fresh line keeps every row aligned.
        output.WriteLine("\n" + root.ToAsciiMap());
    }

    [Fact]
    public void EveryLeafGetsARoom()
    {
        var root = CarvedPlan(80, 50, seed: 1);

        Assert.All(root.Leaves(), leaf => Assert.NotNull(leaf.Room));
    }

    [Fact]
    public void CircularRoomCarverCanBeAimedAtASmallSubtreeOfThePlan()
    {
        // node {58} is a 3-leaf subtree in the top-right of the seed-1 plan (see
        // PrintCarvedPlanForManualInspection). Aiming CircularRoomCarver at that internal node
        // should give its whole subtree circular rooms and leave every other leaf on the
        // inherited default carver.
        var subtreeLeafIds = new HashSet<int> { 235, 236, 118 };
        Func<Node, IRoomCarver, Random, IRoomCarver> selectCarver = (node, inherited, _) =>
            node.Id == 58 ? CircularRoomCarver.Instance : inherited;

        var root = CarvedPlan(80, 50, seed: 1, selectCarver: selectCarver);

        var circularLeaves = root.Leaves()
            .Where(leaf => leaf.Room!.Type == RoomType.Circular)
            .ToList();

        Assert.InRange(circularLeaves.Count, 2, 4);
        Assert.Equal(subtreeLeafIds, circularLeaves.Select(leaf => leaf.Id).ToHashSet());
        Assert.All(
            root.Leaves().Where(leaf => !subtreeLeafIds.Contains(leaf.Id)),
            leaf => Assert.NotEqual(RoomType.Circular, leaf.Room!.Type)
        );
    }

    [Fact]
    public void EachRoomLiesInsideItsLeafAreaWithTheRequestedMargin()
    {
        // Loop over several seeds so this isn't pinned to one layout.
        for (int seed = 0; seed < 20; seed++)
        {
            var root = CarvedPlan(80, 50, seed);

            Assert.All(
                root.Leaves(),
                leaf =>
                {
                    var room = leaf.Room!;
                    var area = leaf.Area;
                    // Area coords are half-open [Min, Max); the last owned cell is Max - 1.
                    Assert.True(room.Left >= area.XMin + Margin);
                    Assert.True(room.Right <= area.XMax - 1 - Margin);
                    Assert.True(room.Upper >= area.YMin + Margin);
                    Assert.True(room.Lower <= area.YMax - 1 - Margin);
                }
            );
        }
    }

    [Fact]
    public void RoomsInDifferentLeavesNeverTouch()
    {
        // Consequence of the margin invariant: no two rooms are edge- or corner-adjacent, so the
        // divider cells between leaf areas stay free for walls. Every pair of leaf rooms must
        // have a gap of >= 1 tile on at least one axis.
        var root = CarvedPlan(80, 50, seed: 1);
        var rooms = root.Leaves().Select(leaf => leaf.Room!).ToList();

        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var a = rooms[i];
                var b = rooms[j];
                bool separated =
                    a.Right + 1 < b.Left
                    || b.Right + 1 < a.Left
                    || a.Lower + 1 < b.Upper
                    || b.Lower + 1 < a.Upper;

                Assert.True(
                    separated,
                    $"Rooms touch or overlap: [{a.Left},{a.Upper}]-[{a.Right},{a.Lower}] vs "
                        + $"[{b.Left},{b.Upper}]-[{b.Right},{b.Lower}]"
                );
            }
        }
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void ChanceOfLeafHavingNoRoomOutOfRangeThrows(double chance)
    {
        var root = new Node(new Area(0, 20, 0, 20));

        var _ = Assert.Throws<ArgumentOutOfRangeException>(() =>
            root.CarveRooms(Margin, MinRoomWidth, MinRoomHeight, new Random(1), chance)
        );
    }

    [Fact]
    public void ZeroChanceOfLeafHavingNoRoomStillGivesEveryLeafARoom()
    {
        // Regression guard for the default: an explicit 0 must behave identically to omitting
        // the parameter (as EveryLeafGetsARoom already checks implicitly).
        var root = CarvedPlan(80, 50, seed: 1, chanceOfLeafHavingNoRoom: 0);

        Assert.All(root.Leaves(), leaf => Assert.NotNull(leaf.Room));
    }

    [Fact]
    public void FullChanceOfLeafHavingNoRoomLeavesEveryLeafRoomless()
    {
        var root = CarvedPlan(80, 50, seed: 1, chanceOfLeafHavingNoRoom: 1);

        Assert.All(root.Leaves(), leaf => Assert.Null(leaf.Room));
    }

    [Fact]
    public void PartialChanceOfLeafHavingNoRoomProducesAMixOfRoomedAndRoomlessLeaves()
    {
        // A single seed/layout could plausibly roll all-or-nothing by chance, so pool leaves
        // across several seeds before asserting both outcomes are actually represented.
        var allLeaves = Enumerable
            .Range(0, 20)
            .SelectMany(seed => CarvedPlan(80, 50, seed, chanceOfLeafHavingNoRoom: 0.5).Leaves())
            .ToList();

        Assert.Contains(allLeaves, leaf => leaf.Room is not null);
        Assert.Contains(allLeaves, leaf => leaf.Room is null);
    }

    [Fact]
    public void RoomlessLeavesDoNotBreakMarginOrSeparationInvariantsForTheRemainingRooms()
    {
        // Same checks as EachRoomLiesInsideItsLeafAreaWithTheRequestedMargin /
        // RoomsInDifferentLeavesNeverTouch, but with some leaves opted out - carving a room for
        // one leaf shouldn't depend on, or be affected by, a sibling leaf having none.
        var root = CarvedPlan(80, 50, seed: 1, chanceOfLeafHavingNoRoom: 0.5);
        var roomedLeaves = root.Leaves().Where(leaf => leaf.Room is not null).ToList();

        Assert.NotEmpty(roomedLeaves);

        Assert.All(
            roomedLeaves,
            leaf =>
            {
                var room = leaf.Room!;
                var area = leaf.Area;
                // Area coords are half-open [Min, Max); the last owned cell is Max - 1.
                Assert.True(room.Left >= area.XMin + Margin);
                Assert.True(room.Right <= area.XMax - 1 - Margin);
                Assert.True(room.Upper >= area.YMin + Margin);
                Assert.True(room.Lower <= area.YMax - 1 - Margin);
            }
        );

        var rooms = roomedLeaves.Select(leaf => leaf.Room!).ToList();
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var a = rooms[i];
                var b = rooms[j];
                bool separated =
                    a.Right + 1 < b.Left
                    || b.Right + 1 < a.Left
                    || a.Lower + 1 < b.Upper
                    || b.Lower + 1 < a.Upper;

                Assert.True(
                    separated,
                    $"Rooms touch or overlap: [{a.Left},{a.Upper}]-[{a.Right},{a.Lower}] vs "
                        + $"[{b.Left},{b.Upper}]-[{b.Right},{b.Lower}]"
                );
            }
        }
    }

    [Fact]
    public void EveryCorridorEndpointTouchesARoomOrAnotherCorridor()
    {
        // No corridor should dead-end in void: for each corridor, both ends must land exactly on
        // a room's centre or on a cell painted by some other corridor. Holds by construction of
        // ConnectRooms (an endpoint is always a value handed up unchanged from a child), so this
        // is a regression guard against that invariant quietly breaking.
        var root = CarvedPlan(80, 50, seed: 1, chanceOfLeafHavingNoRoom: 0.3);

        var roomCenters = root.Leaves()
            .Where(leaf => leaf.Room is not null)
            .Select(leaf => leaf.Room!.ConnectorPoint)
            .ToHashSet();

        var corridors = root.AllNodes()
            .Select(node => node.Corridor)
            .Where(corridor => corridor is not null)
            .Select(corridor => corridor!.Value)
            .ToList();

        // Sanity check that this seed/chance combination actually exercises corridors.
        Assert.NotEmpty(corridors);

        foreach (var corridor in corridors)
        {
            var otherCorridorCells = corridors
                .Where(other => !other.Equals(corridor))
                .SelectMany(other => other.Points())
                .ToHashSet();

            Assert.True(
                roomCenters.Contains(corridor.From) || otherCorridorCells.Contains(corridor.From),
                $"Corridor endpoint {corridor.From} touches neither a room nor another corridor."
            );
            Assert.True(
                roomCenters.Contains(corridor.To) || otherCorridorCells.Contains(corridor.To),
                $"Corridor endpoint {corridor.To} touches neither a room nor another corridor."
            );
        }
    }

    [Fact]
    public void AllRoomsAreReachableFromAnyRoom()
    {
        // The payoff check for the corridor pass: DFS order over Leaves() does not imply spatial
        // adjacency, so connectivity has to be proven. Flood-fill from one room's floor over
        // room+corridor cells and assert the reached set covers every room's centre.
        var root = CarvedPlan(80, 50, seed: 1, chanceOfLeafHavingNoRoom: 0.3);

        var roomedLeaves = root.Leaves().Where(leaf => leaf.Room is not null).ToList();
        Assert.NotEmpty(roomedLeaves);

        var floorCells = new HashSet<GridPoint>();
        foreach (var leaf in roomedLeaves)
        {
            var room = leaf.Room!;
            for (int x = room.Left; x <= room.Right; x++)
            {
                for (int y = room.Upper; y <= room.Lower; y++)
                {
                    bool _ = floorCells.Add(new GridPoint(x, y));
                }
            }
        }
        foreach (var node in root.AllNodes())
        {
            if (node.Corridor is { } corridor)
            {
                foreach (var point in corridor.Points())
                {
                    bool _ = floorCells.Add(point);
                }
            }
        }

        var start = roomedLeaves[0].Room!.ConnectorPoint;
        var reached = new HashSet<GridPoint> { start };
        var frontier = new Queue<GridPoint>();
        frontier.Enqueue(start);
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            GridPoint[] neighbors =
            [
                current with
                {
                    X = current.X + 1,
                },
                current with
                {
                    X = current.X - 1,
                },
                current with
                {
                    Y = current.Y + 1,
                },
                current with
                {
                    Y = current.Y - 1,
                },
            ];
            foreach (var neighbor in neighbors)
            {
                if (floorCells.Contains(neighbor) && reached.Add(neighbor))
                {
                    frontier.Enqueue(neighbor);
                }
            }
        }

        Assert.All(roomedLeaves, leaf => Assert.Contains(leaf.Room!.ConnectorPoint, reached));
    }
}
