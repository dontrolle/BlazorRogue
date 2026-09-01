using System.Collections.Generic;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// An axis-aligned filled ellipse - a circle when its bounding box happens to be square - carved
/// by <see cref="CircularRoomCarver"/>. Its bounding box (<see cref="Room.X"/>/<see cref="Room.Width"/>
/// /etc.) is the rectangle the ellipse is inscribed in and touches on all four sides, but
/// <see cref="Room.FootprintAreas"/> reports only the cells actually inside the ellipse, so the
/// bounding box's corners aren't treated as floor.
/// </summary>
class CircularRoom : Room
{
    // Cells whose ellipse value is within this of the boundary count as inside, so the shape
    // doesn't lose a cell to floating-point rounding when a coordinate sits exactly on the edge.
    const double BoundaryEpsilon = 1e-9;

    readonly Area area;

    /// <param name="area">
    /// The rectangle the ellipse is inscribed in; also this room's bounding box.
    /// </param>
    internal CircularRoom(Area area)
        : base(area.XMin, area.YMin, area.Width, area.Height) => this.area = area;

    internal override IEnumerable<Area> FootprintAreas
    {
        get
        {
            int width = area.Width;
            int height = area.Height;

            // An ellipse meets any horizontal line in a single interval, so each row contributes
            // at most one contiguous run of floor cells - yield that run rather than one Area per
            // cell, matching CaveRoom's approach.
            for (int y = 0; y < height; y++)
            {
                int runStart = -1;
                for (int x = 0; x <= width; x++)
                {
                    bool isFloor = x < width && IsInsideEllipse(x, y, width, height);
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

    internal override RoomType Type => RoomType.Circular;

    /// <summary>
    /// Whether the cell at local <paramref name="x"/>/<paramref name="y"/> falls inside the
    /// ellipse inscribed in a <paramref name="width"/> x <paramref name="height"/> box. The
    /// semi-axes are half the box's size, so the ellipse touches every side at its midpoint; the
    /// centre never lands exactly on a cell for an even dimension, but stays well inside.
    /// </summary>
    internal static bool IsInsideEllipse(int x, int y, int width, int height)
    {
        double centerX = (width - 1) / 2.0;
        double centerY = (height - 1) / 2.0;
        double radiusX = width / 2.0;
        double radiusY = height / 2.0;

        double dx = (x - centerX) / radiusX;
        double dy = (y - centerY) / radiusY;
        return (dx * dx) + (dy * dy) <= 1.0 + BoundaryEpsilon;
    }
}
