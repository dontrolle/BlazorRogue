using System;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

class CaveGenerator(int width, int height, Game game, SettingsMap settings)
    : DungeonGeneratorBase(
        width,
        height,
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
    readonly string floorTileSet = LayoutSettings(settings)
        .GetString("floor_tile_set", "crusted_grey");

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
        var floorset =
            configuration.SpecialFloorSets.FirstOrDefault(t => t.Id == floorTileSet)
            ?? throw new InvalidOperationException(
                "Missing tileset " + floorTileSet + " in read configuration."
            );
        int[] floorIndexes = [1, 2, 3, 4];
        FillMap(genmap, floorset);

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

    protected override void OnNorthHalfWallPlaced(int x, int y)
    {
        // add cave_edge_1 and 2 to halfwall-tiles offset to the left and right respectively
        if (
            x > 0
            && (
                map.Tiles[x - 1, y].TileType == TileType.Floor
                || map.Tiles[x - 1, y].TileType == TileType.Black
            )
        )
        {
            map.AddGameObject(new CaveEdge(x, y, 1, -1, -1));
        }

        if (
            x < map.Width - 1
            && (
                map.Tiles[x + 1, y].TileType == TileType.Floor
                || map.Tiles[x + 1, y].TileType == TileType.Black
            )
        )
        {
            map.AddGameObject(new CaveEdge(x, y, 2, -1, 1));
        }
    }

    protected override void OnSouthWallFrontPlaced(int x, int y)
    {
        // add cave_edge_5 and 6 to wall tiles with front offset to the left and right respectively
        if (
            x > 0
            && (
                map.Tiles[x - 1, y].TileType == TileType.Floor
                || map.Tiles[x - 1, y].TileType == TileType.Black
            )
        )
        {
            map.AddGameObject(new CaveEdge(x, y, 5, 0, -1));
        }

        if (
            x < map.Width - 1
            && (
                map.Tiles[x + 1, y].TileType == TileType.Floor
                || map.Tiles[x + 1, y].TileType == TileType.Black
            )
        )
        {
            map.AddGameObject(new CaveEdge(x, y, 6, 0, 1));
        }
    }

    protected override void OnWallTileVisited(int x, int y)
    {
        // add cave_edge_3 and 4 to normal wall tiles offset to the left and right respectively
        if (
            x > 0
            && (
                map.Tiles[x - 1, y].TileType == TileType.Floor
                || map.Tiles[x - 1, y].TileType == TileType.Black
            )
        )
        {
            map.AddGameObject(new CaveEdge(x, y, 3, 0, -1));
        }

        if (
            x < map.Width - 1
            && (
                map.Tiles[x + 1, y].TileType == TileType.Floor
                || map.Tiles[x + 1, y].TileType == TileType.Black
            )
        )
        {
            map.AddGameObject(new CaveEdge(x, y, 4, 0, 1));
        }
    }
}
