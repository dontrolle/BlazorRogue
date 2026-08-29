using System.Collections.Generic;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// A cave-shaped room: an irregular floor/wall pattern carved by <see cref="CaveRoomCarver"/>'s
/// cellular automaton, rather than one or more solid rectangles.
/// </summary>
class CaveRoom : Room
{
    readonly Area area;
    readonly bool[,] isWall;
    readonly GridPoint connectorPoint;

    /// <param name="area">The area the cave was carved into; also this room's bounding box.</param>
    /// <param name="isWall">
    /// <c>[area.Width, area.Height]</c>-sized grid, in <paramref name="area"/>-local coordinates:
    /// <c>true</c> is wall, <c>false</c> is floor.
    /// </param>
    /// <param name="connectorPoint">
    /// The point corridors should connect to - supplied by the carver, since it's the one that
    /// knows which cell is guaranteed to actually be floor. See <see cref="Room.ConnectorPoint"/>.
    /// </param>
    internal CaveRoom(Area area, bool[,] isWall, GridPoint connectorPoint)
        : base(area.XMin, area.YMin, area.Width, area.Height)
    {
        this.area = area;
        this.isWall = isWall;
        this.connectorPoint = connectorPoint;
    }

    internal override IEnumerable<Area> FootprintAreas
    {
        get
        {
            int width = isWall.GetLength(0);
            int height = isWall.GetLength(1);

            // Yield contiguous runs of floor cells per row, rather than one Area per cell, to
            // keep the footprint from ballooning into hundreds of 1x1 areas.
            for (int y = 0; y < height; y++)
            {
                int runStart = -1;
                for (int x = 0; x <= width; x++)
                {
                    bool isFloor = x < width && !isWall[x, y];
                    if (isFloor && runStart < 0)
                    {
                        runStart = x;
                    }
                    else if (!isFloor && runStart >= 0)
                    {
                        yield return new Area(
                            area.XMin + runStart,
                            area.XMin + x,
                            area.YMin + y,
                            area.YMin + y + 1
                        );
                        runStart = -1;
                    }
                }
            }
        }
    }

    internal override GridPoint ConnectorPoint => connectorPoint;

    internal override RoomType Type => RoomType.Cave;
}
