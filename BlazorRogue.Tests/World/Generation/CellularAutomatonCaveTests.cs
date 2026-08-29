using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World.Generation;

public class CellularAutomatonCaveTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(4, 3)]
    public void AllWallInitialChanceProducesAnEntirelyWallGrid(
        int smoothingPassOneIterations,
        int smoothingPassTwoIterations
    )
    {
        // Random.NextDouble() never returns exactly 1.0, so a chance of 1.0 always seeds every
        // cell as wall regardless of the random stream - and once every cell starts as wall, both
        // smoothing rules keep it that way (a cell surrounded entirely by wall neighbours always
        // satisfies "5+ within radius 1"). This should hold for any seed and any iteration count.
        bool[,] grid = CellularAutomatonCave.Generate(
            10,
            8,
            new Random(1),
            percentageChanceOfInitialWall: 1.0,
            smoothingPassOneIterations,
            smoothingPassTwoIterations
        );

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Assert.True(grid[x, y], $"Expected ({x},{y}) to be wall.");
            }
        }
    }

    [Fact]
    public void ZeroWallChanceWithNoSmoothingLeavesOnlyTheBorderAsWall()
    {
        // With no initial walls and no smoothing passes, the only step that can still produce
        // wall is the final border-forcing step - so the result should be wall on the border and
        // floor everywhere else, deterministically (no dependence on the random seed).
        const int width = 10;
        const int height = 8;
        bool[,] grid = CellularAutomatonCave.Generate(
            width,
            height,
            new Random(1),
            percentageChanceOfInitialWall: 0.0,
            smoothingPassOneIterations: 0,
            smoothingPassTwoIterations: 0
        );

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isBorder = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                Assert.True(
                    grid[x, y] == isBorder,
                    $"Expected ({x},{y}) to be {(isBorder ? "wall" : "floor")}."
                );
            }
        }
    }

    [Fact]
    public void BorderIsAlwaysForcedToWallRegardlessOfSmoothing()
    {
        // Unlike the previous, fully-deterministic case, this exercises the normal smoothing
        // passes (which are stochastic) - only the border cells are asserted, since those are
        // unconditionally forced to wall as the last step no matter what the smoothing produced.
        const int width = 12;
        const int height = 10;

        for (int seed = 0; seed < 10; seed++)
        {
            bool[,] grid = CellularAutomatonCave.Generate(
                width,
                height,
                new Random(seed),
                percentageChanceOfInitialWall: 0.4,
                smoothingPassOneIterations: 4,
                smoothingPassTwoIterations: 3
            );

            for (int x = 0; x < width; x++)
            {
                Assert.True(grid[x, 0]);
                Assert.True(grid[x, height - 1]);
            }
            for (int y = 0; y < height; y++)
            {
                Assert.True(grid[0, y]);
                Assert.True(grid[width - 1, y]);
            }
        }
    }
}
