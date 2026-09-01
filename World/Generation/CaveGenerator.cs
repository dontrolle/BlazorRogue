using System;
using BlazorRogue.Entities;

namespace BlazorRogue.World.Generation;

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
        bool[,] genmap = CellularAutomatonCave.Generate(
            map.Width,
            map.Height,
            mapGenerationRandomSource,
            percentageChanceOfInitialWall,
            smoothingPassOneIterations,
            smoothingPassTwoIterations
        );

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
}
