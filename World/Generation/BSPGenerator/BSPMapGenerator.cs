using System;
using BlazorRogue.Entities;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Dungeon generator that lays out rooms via binary space partitioning.
/// </summary>
/// <param name="width">Dungeon width</param>
/// <param name="height">Dungeon height</param>
/// <param name="levelNumber">The level's "no" in levels.json, used e.g. to decide which stairs exist</param>
/// <param name="game">Game instance</param>
/// <param name="settings">Map settings</param>
class BSPMapGenerator(int width, int height, int levelNumber, Game game, SettingsMap settings)
    : MapGeneratorBase(
        width,
        height,
        levelNumber,
        game,
        SelectWallSet(game.Configuration, settings, game.Configuration.DungeonWallSets),
        settings
    )
{
    public const string Id = "bsp_map_generator";

    protected override Tuple<int, int> CreateLayout()
    {
        var root = new Node(new Area(0, map.Width, 0, map.Height));

        root.SplitUntilThreshold(15, 6, mapGenerationRandomSource, []);

        // player position random for now
        return GetRandomUnblockedMapTile();
    }
}
