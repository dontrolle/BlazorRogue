using System;
using System.Collections.Generic;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// A room made of two overlapping rectangles, carved by <see cref="OverlaidRectanglesRoomCarver"/>.
/// Its own bounding box (<see cref="Room.X"/>/<see cref="Room.Width"/>/etc.) is the union of both
/// rectangles' bounds, but <see cref="Room.FootprintAreas"/> reports the two rectangles
/// separately, so callers that care about the actual floor shape (e.g. rendering) don't treat the
/// whole bounding box as floor.
/// </summary>
class OverlaidRoom : Room
{
    readonly Area first;
    readonly Area second;
    readonly GridPoint connectorPoint;

    /// <param name="first">The first of the two overlapping rectangles.</param>
    /// <param name="second">The second of the two overlapping rectangles.</param>
    /// <param name="connectorPoint">
    /// Supplied by the carver rather than derived here, since the bounding box's own center can
    /// land in the gap between the two rectangles for a lopsided overlay - the carver is what
    /// knows a point guaranteed to be on the actual footprint (and, down the line, may want to
    /// pick that point some other way than just "wherever the two happen to cross").
    /// </param>
    internal OverlaidRoom(Area first, Area second, GridPoint connectorPoint)
        : base(
            Math.Min(first.XMin, second.XMin),
            Math.Min(first.YMin, second.YMin),
            Math.Max(first.XMax, second.XMax) - Math.Min(first.XMin, second.XMin),
            Math.Max(first.YMax, second.YMax) - Math.Min(first.YMin, second.YMin)
        )
    {
        this.first = first;
        this.second = second;
        this.connectorPoint = connectorPoint;
    }

    internal override IEnumerable<Area> FootprintAreas => [first, second];

    internal override GridPoint ConnectorPoint => connectorPoint;
}
