using System;
using BlazorRogue.Entities;
using BlazorRogue.World.Generation;

class TestMapGenerator(
    int width,
    int height,
    int levelNumber,
    BlazorRogue.Game game,
    SettingsMap settings
)
    : MapGeneratorBase(
        width,
        height,
        levelNumber,
        game,
        SelectWallSet(game.Configuration, settings, game.Configuration.DungeonWallSets),
        settings
    )
{
    public const string Id = "test_map_generator";

    readonly (TileSet[] TileSets, double[] Weights) commonFloorPool = ResolveFloorPool(
        game.Configuration,
        settings,
        "common",
        game.Configuration.FloorSets
    );

    protected override Tuple<int, int> CreateLayout()
    {
        var floorSet = GetRandomElementWeighted(commonFloorPool.TileSets, commonFloorPool.Weights);

        // Floor must never reach the outer ring - AddPostGenFloorDecorations (spider-web corner
        // checks, NumberOfSurroundingBlockingSpots) indexes a floor tile's neighbors unguarded,
        // relying on every generator keeping the perimeter as walls.
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                bool isBorder = x == 0 || x == map.Width - 1 || y == 0 || y == map.Height - 1;
                if (isBorder)
                {
                    PlaceWall(x, y);
                }
                else
                {
                    PlaceFloor(x, y, floorSet);
                }
            }
        }

        // place two single (freestanding) walls
        var (x0, y0) = GetRandomUnblockedMapTile();
        var (x1, y1) = GetRandomUnblockedMapTile();

        PlaceWall(x0, y0);
        PlaceWall(x1, y1);
        return GetRandomUnblockedMapTile();
    }
}
