using System;
using BlazorRogue.Entities;

namespace BlazorRogue.World;

/// <summary>
/// Cave generator, using cellular automate.
/// </summary>
/// <param name="width">Map width</param>
/// <param name="height">Map height</param>
/// <param name="levelNumber">The level's "no" in levels.json, used e.g. to decide which stairs exist</param>
/// <param name="game">Game instance</param>
/// <param name="settings">Map settings</param>
class CaveGenerator(int width, int height, int levelNumber, Game game, SettingsMap settings)
    : MapGeneratorBase(
        width,
        height,
        levelNumber,
        game,
        SelectWallSet(game.Configuration, settings, game.Configuration.CaveWallSets),
        settings
    )
{
    public const string Id = "cave_generator";

    // LayoutSettings() is static because field initializers can't reference another instance
    // field/method of the same type being constructed - only static members and the primary
    // constructor's own parameters (e.g. `settings`) are allowed at this point.
    readonly double percentageChanceOfInitialWall = LayoutSettings(settings)
        .GetDouble("percentage_chance_of_initial_wall", 0.4);
    readonly int smoothingPassOneIterations = LayoutSettings(settings)
        .GetInt("smoothing_pass_one_iterations", 4);
    readonly int smoothingPassTwoIterations = LayoutSettings(settings)
        .GetInt("smoothing_pass_two_iterations", 3);
    readonly TileSet floorTileSet = SelectFloorTileSet(game.Configuration, settings);

    static TileSet SelectFloorTileSet(Configuration configuration, SettingsMap settings)
    {
        var (tileSets, weights) = ResolveFloorPool(
            configuration,
            settings,
            "common",
            configuration.FloorSets
        );
        return SelectRandomWeighted(tileSets, weights);
    }

    static SettingsMap LayoutSettings(SettingsMap settings) =>
        settings.GetMap("layout", SettingsMap.Empty);

    protected override Tuple<int, int> CreateLayout() => CreateCave();

    Tuple<int, int> CreateCave()
    {
        bool[,] genmap = new bool[map.Width, map.Height];

        void InitFill(int x, int y) =>
            genmap[x, y] = random.NextDouble() < percentageChanceOfInitialWall;

        map.ForEachTile(InitFill);

        bool[,]? newmap = null;
        void Generation1Fill(int x, int y) =>
            newmap[x, y] =
                SurroundingWallNumberWithinN(genmap, x, y, 1) >= 5
                || SurroundingWallNumberWithinN(genmap, x, y, 2) <= 1;

        for (int i = 0; i < smoothingPassOneIterations; i++)
        {
            newmap = new bool[map.Width, map.Height];
            map.ForEachTile(Generation1Fill);
            genmap = newmap;
        }

        void Generation2Fill(int x, int y) =>
            newmap[x, y] = SurroundingWallNumberWithinN(genmap, x, y, 1) >= 5;

        for (int i = 0; i < smoothingPassTwoIterations; i++)
        {
            newmap = new bool[map.Width, map.Height];
            map.ForEachTile(Generation2Fill);
            genmap = newmap;
        }

        // fill border area
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (x == 0 || x == map.Width - 1 || y == 0 || y == map.Height - 1)
                    genmap[x, y] = true;
            }
        }

        return FinalizeCaveGen(genmap);
    }

    Tuple<int, int> FinalizeCaveGen(bool[,] genmap)
    {
        FillMap(genmap, floorTileSet);

        return GetRandomUnblockedMapTile();
    }

    void FillMap(bool[,] genmap, TileSet floorset) =>
        map.ForEachTile(
            (x, y) =>
            {
                if (genmap[x, y])
                    PlaceWall(x, y);
                else
                    PlaceFloor(x, y, floorset);
            }
        );

    int SurroundingWallNumberWithinN(bool[,] genmap, int x, int y, int distance)
    {
        int noOfWalls = 0;

        for (int dx = -distance; dx < distance + 1; dx++)
        {
            for (int dy = -distance; dy < distance + 1; dy++)
            {
                // consider outside of map as walls
                // TODO: check map - slightly wrong...
                if (x + dx < 0 || x + dx > map.Width - 1 || y + dy < 0 || y + dy > map.Height - 1)
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
