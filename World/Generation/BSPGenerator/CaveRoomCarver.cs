using System;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Carves a cave-shaped room via the same cellular-automaton algorithm as
/// <see cref="CaveGenerator"/> (see <see cref="CellularAutomatonCave"/>), scoped to a single
/// leaf's area instead of the whole map. The area's outer border is always forced to wall by that
/// algorithm, which gives these rooms a built-in one-cell margin - no extra inset is applied on
/// top of whatever margin the caller already requested.
/// </summary>
/// <param name="percentageChanceOfInitialWall">
/// Raw chance [0,1] that a cell starts out as wall before smoothing. Defaults to
/// <see cref="CaveGenerator"/>'s own default.
/// </param>
/// <param name="smoothingPassOneIterations">
/// See <see cref="CellularAutomatonCave.Generate"/>. Defaults to <see cref="CaveGenerator"/>'s own
/// default.
/// </param>
/// <param name="smoothingPassTwoIterations">
/// See <see cref="CellularAutomatonCave.Generate"/>. Defaults to <see cref="CaveGenerator"/>'s own
/// default.
/// </param>
class CaveRoomCarver(
    double percentageChanceOfInitialWall = 0.4,
    int smoothingPassOneIterations = 4,
    int smoothingPassTwoIterations = 3
) : IRoomCarver
{
    public Room CarveRoom(Area area, int minWidth, int minHeight, Random randomSource)
    {
        bool[,] isWall = CellularAutomatonCave.Generate(
            area.Width,
            area.Height,
            randomSource,
            percentageChanceOfInitialWall,
            smoothingPassOneIterations,
            smoothingPassTwoIterations
        );

        return new CaveRoom(area, isWall, FindConnectorPoint(isWall, area));
    }

    /// <summary>
    /// Picks the floor cell closest to the area's centre as the room's connector point. Doesn't
    /// verify that cell is reachable from every other floor cell in the cave - with reasonable
    /// settings the automaton tends to produce one dominant connected cavern, but a small,
    /// disconnected pocket elsewhere in the room is possible and would end up unreachable via
    /// corridors.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The generated cave has no floor cells at all (possible for a very small area or an
    /// aggressive initial-wall chance).
    /// </exception>
    static GridPoint FindConnectorPoint(bool[,] isWall, Area area)
    {
        int width = isWall.GetLength(0);
        int height = isWall.GetLength(1);
        double centerX = (width - 1) / 2.0;
        double centerY = (height - 1) / 2.0;

        int bestX = -1;
        int bestY = -1;
        double bestDistanceSquared = double.MaxValue;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (isWall[x, y])
                {
                    continue;
                }

                double dx = x - centerX;
                double dy = y - centerY;
                double distanceSquared = (dx * dx) + (dy * dy);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestX = x;
                    bestY = y;
                }
            }
        }

        if (bestX < 0)
        {
            throw new InvalidOperationException(
                $"Cave carving produced no floor cells in a {width}x{height} area to connect to; "
                    + "try a larger area or a lower percentageChanceOfInitialWall."
            );
        }

        return new GridPoint(area.XMin + bestX, area.YMin + bestY);
    }
}
