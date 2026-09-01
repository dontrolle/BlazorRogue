using System;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Carves a single filled ellipse (a circle when the chosen bounding box is square - as close to
/// one as a tile grid allows), randomly sized between the requested minimum and the full area and
/// randomly positioned within it, just like <see cref="RectangularRoomCarver"/> but with the
/// bounding box's corners rounded off. The ellipse always touches all four sides of its bounding
/// box, so at the maximum size it fills the whole area edge to edge.
/// </summary>
class CircularRoomCarver : IRoomCarver
{
    internal static readonly CircularRoomCarver Instance = new();

    public Room CarveRoom(Area area, int minWidth, int minHeight, Random randomSource)
    {
        int roomWidth = randomSource.Next(minWidth, area.Width + 1);
        int roomHeight = randomSource.Next(minHeight, area.Height + 1);
        int xMin = area.XMin + randomSource.Next(0, area.Width - roomWidth);
        int yMin = area.YMin + randomSource.Next(0, area.Height - roomHeight);

        return new CircularRoom(new Area(xMin, xMin + roomWidth, yMin, yMin + roomHeight));
    }
}
