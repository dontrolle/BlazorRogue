using System;
using System.Collections.Generic;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Carves a cave-shaped room via the same cellular-automaton algorithm as
/// <see cref="CaveGenerator"/> (see <see cref="CellularAutomatonCave"/>), scoped to a single
/// leaf's area instead of the whole map. The area's outer border is always forced to wall by that
/// algorithm, which gives these rooms a built-in one-cell margin - no extra inset is applied on
/// top of whatever margin the caller already requested. Any cavern the automaton leaves
/// disconnected from the room's connector point is walled back in, so the footprint is always a
/// single connected region.
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

        var (connectorX, connectorY) = FindConnectorCell(isWall);

        // The automaton can leave small caverns cut off from the main one. Wall those off so the
        // footprint is a single region reachable from the connector point - Node.ConnectRooms
        // (and any reachability check) can then treat the whole footprint as connected.
        FillPocketsDisconnectedFrom(isWall, connectorX, connectorY);

        return new CaveRoom(
            area,
            isWall,
            new GridPoint(area.XMin + connectorX, area.YMin + connectorY)
        );
    }

    /// <summary>
    /// Picks the floor cell closest to the area's centre as the room's connector point, in
    /// area-local coordinates.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The generated cave has no floor cells at all (possible for a very small area or an
    /// aggressive initial-wall chance).
    /// </exception>
    static (int X, int Y) FindConnectorCell(bool[,] isWall)
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

        return (bestX, bestY);
    }

    /// <summary>
    /// Flood-fills the floor region 4-connected from (<paramref name="startX"/>,
    /// <paramref name="startY"/>) and turns every floor cell the fill doesn't reach back into
    /// wall, leaving a single connected cavern.
    /// </summary>
    static void FillPocketsDisconnectedFrom(bool[,] isWall, int startX, int startY)
    {
        int width = isWall.GetLength(0);
        int height = isWall.GetLength(1);

        bool[,] reached = new bool[width, height];
        var frontier = new Queue<(int X, int Y)>();
        reached[startX, startY] = true;
        frontier.Enqueue((startX, startY));

        while (frontier.Count > 0)
        {
            var (x, y) = frontier.Dequeue();
            (int X, int Y)[] neighbours = [(x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)];
            foreach (var (nx, ny) in neighbours)
            {
                if (
                    nx >= 0
                    && nx < width
                    && ny >= 0
                    && ny < height
                    && !isWall[nx, ny]
                    && !reached[nx, ny]
                )
                {
                    reached[nx, ny] = true;
                    frontier.Enqueue((nx, ny));
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!isWall[x, y] && !reached[x, y])
                {
                    isWall[x, y] = true;
                }
            }
        }
    }
}
