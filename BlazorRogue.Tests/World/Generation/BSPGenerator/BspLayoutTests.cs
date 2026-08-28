using BlazorRogue.World.Generation;
using BlazorRogue.World.Generation.BSPGenerator;
using Xunit.Abstractions;

namespace BlazorRogue.Tests.World.Generation.BSPGenerator;

/// <summary>
/// Harness for the BSP room-carving and corridor-connection passes. The property tests are
/// skipped stubs until <c>Node.CarveRooms</c> / <c>Node.ConnectRooms</c> exist - fill in each
/// body and drop its <c>Skip</c> as that pass lands.
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
    const int Margin = 1;
    const int MinRoomWidth = 3;
    const int MinRoomHeight = 3;

    /// <summary>
    /// Builds a plan for a <paramref name="width"/> x <paramref name="height"/> map from a fixed
    /// <paramref name="seed"/>, so every step is deterministic and replayable.
    /// </summary>
    static Node CarvedPlan(int width, int height, int seed)
    {
        var root = new Node(new Area(0, width, 0, height));
        root.SplitUntilThreshold(Threshold, MinSplit, new Random(seed));

        // Each pass gets its own fresh seeded Random so changing one pass doesn't shift the
        // random stream the next one observes.
        root.CarveRooms(Margin, MinRoomWidth, MinRoomHeight, new Random(seed));
        // TODO: uncomment once Node.ConnectRooms exists.
        // root.ConnectRooms(new Random(seed));

        return root;
    }

    // Not a real test - see the class summary for how to run it.
    [Fact]
    public void PrintCarvedPlanForManualInspection()
    {
        var root = CarvedPlan(width: 80, height: 50, seed: 1);

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

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Implement once Node.ConnectRooms exists")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public void EveryCorridorEndpointTouchesARoomOrAnotherCorridor()
    {
        // No corridor should dead-end in void: walk the internal nodes, and for each corridor
        // check that both ends sit on a room floor or on another corridor cell.
        var root = CarvedPlan(80, 50, seed: 1);
        _ = root;
    }

#pragma warning disable xUnit1004 // Test methods should not be skipped
    [Fact(Skip = "Implement once Node.ConnectRooms exists")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
    public void AllRoomsAreReachableFromAnyRoom()
    {
        // The payoff check for the corridor pass: DFS order over Leaves() does not imply spatial
        // adjacency, so connectivity has to be proven. Flood-fill from one room's floor over
        // room+corridor cells and assert the reached set covers every room's centre.
        var root = CarvedPlan(80, 50, seed: 1);
        _ = root;
    }
}
