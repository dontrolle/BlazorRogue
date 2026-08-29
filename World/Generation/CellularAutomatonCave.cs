using System;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Cellular-automaton cave shape generator, shared by <see cref="CaveGenerator"/> (which stamps
/// one across the whole map) and <see cref="BSPGenerator.CaveRoomCarver"/> (which stamps one into
/// a single BSP leaf's area).
/// </summary>
static class CellularAutomatonCave
{
    /// <summary>
    /// Generates a <paramref name="width"/> x <paramref name="height"/> wall/floor grid: seeds
    /// each cell as a wall with probability <paramref name="percentageChanceOfInitialWall"/>,
    /// smooths it with two successive rounds of cellular-automaton passes, then forces the outer
    /// border to wall.
    /// </summary>
    /// <param name="width">Width of the grid to generate.</param>
    /// <param name="height">Height of the grid to generate.</param>
    /// <param name="randomSource">Random source used for the initial wall seeding.</param>
    /// <param name="percentageChanceOfInitialWall">
    /// Raw chance [0,1] that a cell starts out as wall before smoothing.
    /// </param>
    /// <param name="smoothingPassOneIterations">
    /// Number of iterations of the first smoothing rule: a cell becomes wall if it has 5+ wall
    /// neighbours within a 1-cell radius, or 1 or fewer within a 2-cell radius (this rule both
    /// rounds off noise and erodes small isolated caverns).
    /// </param>
    /// <param name="smoothingPassTwoIterations">
    /// Number of iterations of the second smoothing rule: a cell becomes wall if it has 5+ wall
    /// neighbours within a 1-cell radius (a gentler pass that just rounds off remaining noise).
    /// </param>
    /// <returns>A <c>[width, height]</c> grid; <c>true</c> means wall, <c>false</c> means floor.</returns>
    internal static bool[,] Generate(
        int width,
        int height,
        Random randomSource,
        double percentageChanceOfInitialWall,
        int smoothingPassOneIterations,
        int smoothingPassTwoIterations
    )
    {
        bool[,] genmap = new bool[width, height];

        void ForEachCell(Action<int, int> apply)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    apply(x, y);
                }
            }
        }

        ForEachCell(
            (x, y) => genmap[x, y] = randomSource.NextDouble() < percentageChanceOfInitialWall
        );

        bool[,]? newmap = null;
        void Generation1Fill(int x, int y) =>
            newmap[x, y] =
                SurroundingWallCount(genmap, width, height, x, y, 1) >= 5
                || SurroundingWallCount(genmap, width, height, x, y, 2) <= 1;

        for (int i = 0; i < smoothingPassOneIterations; i++)
        {
            newmap = new bool[width, height];
            ForEachCell(Generation1Fill);
            genmap = newmap;
        }

        void Generation2Fill(int x, int y) =>
            newmap[x, y] = SurroundingWallCount(genmap, width, height, x, y, 1) >= 5;

        for (int i = 0; i < smoothingPassTwoIterations; i++)
        {
            newmap = new bool[width, height];
            ForEachCell(Generation2Fill);
            genmap = newmap;
        }

        ForEachCell(
            (x, y) =>
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    genmap[x, y] = true;
                }
            }
        );

        return genmap;
    }

    static int SurroundingWallCount(
        bool[,] genmap,
        int width,
        int height,
        int x,
        int y,
        int distance
    )
    {
        int noOfWalls = 0;

        for (int dx = -distance; dx < distance + 1; dx++)
        {
            for (int dy = -distance; dy < distance + 1; dy++)
            {
                // consider outside of the area as walls
                if (x + dx < 0 || x + dx > width - 1 || y + dy < 0 || y + dy > height - 1)
                {
                    noOfWalls++;
                }
                else if (genmap[x + dx, y + dy])
                {
                    noOfWalls++;
                }
            }
        }

        return noOfWalls;
    }
}
