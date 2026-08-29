using System;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Default <see cref="IRoomCarver"/>: a single rectangle, randomly sized (between the requested
/// minimum and the full area) and randomly positioned within the area.
/// </summary>
class RectangularRoomCarver : IRoomCarver
{
    internal static readonly RectangularRoomCarver Instance = new();

    public Room CarveRoom(Area area, int minWidth, int minHeight, Random randomSource)
    {
        int roomWidth = randomSource.Next(minWidth, area.Width + 1);
        int roomHeight = randomSource.Next(minHeight, area.Height + 1);
        int xMin = area.XMin + randomSource.Next(0, area.Width - roomWidth);
        int yMin = area.YMin + randomSource.Next(0, area.Height - roomHeight);

        return new Room(new Area(xMin, xMin + roomWidth, yMin, yMin + roomHeight));
    }
}
