using BlazorRogue.World.Generation;
using BlazorRogue.World.Generation.BSPGenerator;

namespace BlazorRogue.Tests.World.Generation.BSPGenerator;

/// <summary>
/// Direct tests of the <see cref="IRoomCarver"/> implementations, bypassing <see cref="Node"/>
/// entirely - each carver is called on its own with a plain <see cref="Area"/>, so these check the
/// carvers' own contracts rather than anything about the BSP tree.
/// </summary>
public class RoomCarverTests
{
    static bool IsOnFootprint(Room room, GridPoint point) =>
        room.FootprintAreas.Any(footprint =>
            point.X >= footprint.XMin
            && point.X < footprint.XMax
            && point.Y >= footprint.YMin
            && point.Y < footprint.YMax
        );

    [Fact]
    public void RectangularRoomCarverFitsWithinTheGivenAreaAndRespectsMinimumSize()
    {
        var area = new Area(0, 12, 0, 10);

        for (int seed = 0; seed < 20; seed++)
        {
            var room = RectangularRoomCarver.Instance.CarveRoom(area, 3, 4, new Random(seed));

            Assert.True(room.Left >= area.XMin);
            Assert.True(room.Right <= area.XMax - 1);
            Assert.True(room.Upper >= area.YMin);
            Assert.True(room.Lower <= area.YMax - 1);
            Assert.True(room.Width >= 3 && room.Width <= area.Width);
            Assert.True(room.Height >= 4 && room.Height <= area.Height);
        }
    }

    [Fact]
    public void OverlaidRectanglesRoomCarverInsetsAtLeastOneCellFromTheGivenArea()
    {
        var area = new Area(0, 14, 0, 12);

        for (int seed = 0; seed < 20; seed++)
        {
            var room = OverlaidRectanglesRoomCarver.Instance.CarveRoom(
                area,
                3,
                3,
                new Random(seed)
            );

            Assert.True(room.Left >= area.XMin + 1);
            Assert.True(room.Right <= area.XMax - 2);
            Assert.True(room.Upper >= area.YMin + 1);
            Assert.True(room.Lower <= area.YMax - 2);
        }
    }

    [Fact]
    public void OverlaidRectanglesRoomCarverProducesTwoFootprintRectangles()
    {
        var room = OverlaidRectanglesRoomCarver.Instance.CarveRoom(
            new Area(0, 14, 0, 12),
            3,
            3,
            new Random(1)
        );

        Assert.Equal(2, room.FootprintAreas.Count());
    }

    [Fact]
    public void OverlaidRectanglesRoomCarverProducesAnOverlaidRoomType()
    {
        var room = OverlaidRectanglesRoomCarver.Instance.CarveRoom(
            new Area(0, 14, 0, 12),
            3,
            3,
            new Random(1)
        );

        Assert.Equal(RoomType.Overlaid, room.Type);
    }

    [Fact]
    public void CaveRoomCarverWithNoNoiseCarvesFloorEverywhereExceptTheBorder()
    {
        // percentageChanceOfInitialWall: 0 with no smoothing passes leaves the cellular automaton
        // with nothing to do, so the only wall cells left are the ones CellularAutomatonCave's
        // final step unconditionally forces - i.e. exactly the area's own border. This is fully
        // deterministic and lets the carver's footprint be checked precisely without needing to
        // reason about the stochastic smoothing passes.
        var area = new Area(0, 10, 0, 8);
        var carver = new CaveRoomCarver(
            percentageChanceOfInitialWall: 0,
            smoothingPassOneIterations: 0,
            smoothingPassTwoIterations: 0
        );

        var room = carver.CarveRoom(area, 3, 3, new Random(1));

        for (int x = area.XMin; x < area.XMax; x++)
        {
            for (int y = area.YMin; y < area.YMax; y++)
            {
                bool isBorder =
                    x == area.XMin || x == area.XMax - 1 || y == area.YMin || y == area.YMax - 1;
                Assert.True(
                    IsOnFootprint(room, new GridPoint(x, y)) == !isBorder,
                    $"Expected ({x},{y}) to be {(isBorder ? "wall" : "floor")}."
                );
            }
        }
    }

    [Fact]
    public void CaveRoomCarverWithNoNoiseHasAConnectorPointOnItsFootprint()
    {
        var area = new Area(0, 10, 0, 8);
        var carver = new CaveRoomCarver(
            percentageChanceOfInitialWall: 0,
            smoothingPassOneIterations: 0,
            smoothingPassTwoIterations: 0
        );

        var room = carver.CarveRoom(area, 3, 3, new Random(1));

        Assert.True(IsOnFootprint(room, room.ConnectorPoint));
    }

    [Fact]
    public void CaveRoomCarverBoundingBoxMatchesTheGivenAreaExactly()
    {
        var area = new Area(5, 15, 3, 11);
        var carver = new CaveRoomCarver(
            percentageChanceOfInitialWall: 0,
            smoothingPassOneIterations: 0,
            smoothingPassTwoIterations: 0
        );

        var room = carver.CarveRoom(area, 3, 3, new Random(1));

        Assert.Equal(area.XMin, room.X);
        Assert.Equal(area.YMin, room.Y);
        Assert.Equal(area.Width, room.Width);
        Assert.Equal(area.Height, room.Height);
    }

    [Fact]
    public void CaveRoomCarverProducesACaveRoomType()
    {
        var carver = new CaveRoomCarver(
            percentageChanceOfInitialWall: 0,
            smoothingPassOneIterations: 0,
            smoothingPassTwoIterations: 0
        );

        var room = carver.CarveRoom(new Area(0, 10, 0, 8), 3, 3, new Random(1));

        Assert.Equal(RoomType.Cave, room.Type);
    }

    [Fact]
    public void CaveRoomCarverThrowsWhenTheInitialWallChanceGuaranteesNoFloor()
    {
        // Random.NextDouble() never returns exactly 1.0, so a chance of 1.0 seeds every cell as
        // wall for any seed - there's never a floor cell to connect to.
        var carver = new CaveRoomCarver(percentageChanceOfInitialWall: 1.0);

        var _ = Assert.Throws<InvalidOperationException>(() =>
            carver.CarveRoom(new Area(0, 10, 0, 8), 3, 3, new Random(1))
        );
    }

    [Fact]
    public void ConnectorPointAlwaysLiesOnTheCarvedFootprint()
    {
        // Regression guard: OverlaidRoom originally derived its connector point from its bounding
        // box's centre, which could land in the gap between its two rectangles for a lopsided
        // overlay. Every built-in carver should guarantee its connector point is actually floor.
        (string Name, IRoomCarver Carver)[] carvers =
        [
            ("Rectangular", RectangularRoomCarver.Instance),
            ("Overlaid", OverlaidRectanglesRoomCarver.Instance),
            ("Cave", new CaveRoomCarver()),
        ];
        var area = new Area(0, 30, 0, 24);

        foreach (var (name, carver) in carvers)
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var room = carver.CarveRoom(area, 3, 3, new Random(seed));

                Assert.True(
                    IsOnFootprint(room, room.ConnectorPoint),
                    $"{name} carver (seed {seed}): connector point {room.ConnectorPoint} isn't "
                        + "on any footprint area."
                );
            }
        }
    }
}
