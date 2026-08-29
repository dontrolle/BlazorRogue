using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

public class RoomTests
{
    [Fact]
    public void FootprintAreasDefaultsToOwnBoundingBox()
    {
        var room = new Room(2, 3, 5, 4);

        var footprint = Assert.Single(room.FootprintAreas);
        Assert.Equal(new Area(2, 7, 3, 7), footprint);
    }

    [Fact]
    public void ConnectorPointDefaultsToBoundingBoxCenter()
    {
        var room = new Room(2, 3, 5, 4);

        Assert.Equal(new GridPoint(4, 4), room.ConnectorPoint);
    }

    [Fact]
    public void TypeDefaultsToRectangular()
    {
        var room = new Room(0, 0, 5, 5);

        Assert.Equal(RoomType.Rectangular, room.Type);
    }

    [Fact]
    public void IntersectIsTrueWhenRoomsShareAtLeastOneCellOnBothAxes()
    {
        var a = new Room(0, 0, 5, 5); // cells x:0-4, y:0-4
        var b = new Room(4, 4, 5, 5); // cells x:4-8, y:4-8 - shares cell (4,4)

        Assert.True(a.Intersect(b));
        Assert.True(b.Intersect(a));
    }

    [Fact]
    public void IntersectIsFalseWhenRoomsAreSeparatedOnTheXAxis()
    {
        var a = new Room(0, 0, 5, 5); // cells x:0-4
        var b = new Room(5, 0, 5, 5); // cells x:5-9 - adjacent, no shared column

        Assert.False(a.Intersect(b));
        Assert.False(b.Intersect(a));
    }

    [Fact]
    public void IntersectIsFalseWhenRoomsAreSeparatedOnTheYAxis()
    {
        var a = new Room(0, 0, 5, 5); // cells y:0-4
        var b = new Room(0, 5, 5, 5); // cells y:5-9 - adjacent, no shared row

        Assert.False(a.Intersect(b));
        Assert.False(b.Intersect(a));
    }
}
