using System;
using System.Collections.Generic;

namespace BlazorRogue.World.Generation;

/// <summary>
/// A corridor connecting two points, used internally by map generators. Painted as an L-shaped
/// path between <see cref="From"/> and <see cref="To"/>: a straight run along one axis, then a
/// straight run along the other, bending once at the elbow.
/// </summary>
/// <param name="From">One endpoint of the corridor.</param>
/// <param name="To">The other endpoint of the corridor.</param>
/// <param name="HorizontalFirst">
/// If <c>true</c>, the corridor runs horizontally from <see cref="From"/> before turning to run
/// vertically into <see cref="To"/>; if <c>false</c>, vertically then horizontally.
/// </param>
record struct Corridor(GridPoint From, GridPoint To, bool HorizontalFirst)
{
    /// <summary>
    /// The individual grid cells this corridor occupies, from <see cref="From"/> to
    /// <see cref="To"/> inclusive. The elbow cell is yielded twice (once as the end of the first
    /// run, once as the start of the second); harmless for painting onto a grid.
    /// </summary>
    internal readonly IEnumerable<GridPoint> Points()
    {
        var elbow = HorizontalFirst ? new GridPoint(To.X, From.Y) : new GridPoint(From.X, To.Y);

        foreach (var point in Line(From, elbow))
            yield return point;
        foreach (var point in Line(elbow, To))
            yield return point;
    }

    // Walks the straight line of grid cells between two points that share an X or Y coordinate.
    static IEnumerable<GridPoint> Line(GridPoint from, GridPoint to)
    {
        int stepX = Math.Sign(to.X - from.X);
        int stepY = Math.Sign(to.Y - from.Y);

        var current = from;
        yield return current;
        while (current.X != to.X || current.Y != to.Y)
        {
            current = new GridPoint(current.X + stepX, current.Y + stepY);
            yield return current;
        }
    }
}
