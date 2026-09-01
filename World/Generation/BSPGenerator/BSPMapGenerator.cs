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
    const int AreaThreshold = 15;
    const int MinSplit = 6;
    const int MinMarginBetweenAreaBorderAndRoom = 0;
    const int MinRoomWidth = 3;
    const int MinRoomHeight = 3;

    // Floor-set pool for rooms and corridors. Resolved once here (like BasicDungeonGenerator); a
    // fresh set is picked per room and per corridor so the map isn't one flat colour. Static for
    // the same field-initializer reason SelectWallSet is - see MapGeneratorBase.
    readonly (TileSet[] TileSets, double[] Weights) floorPool = ResolveFloorPool(
        game.Configuration,
        settings,
        "common",
        game.Configuration.FloorSets
    );

    protected override Tuple<int, int> CreateLayout()
    {
        // Inset the root one cell from the map border so no room or corridor floor can land on the
        // perimeter: the wall ring painted around the plan then always stays in bounds, and the
        // post-generation decoration passes (which assume floors are never on the edge) hold.
        var root = new Node(new Area(1, map.Width - 1, 1, map.Height - 1));

        // Partition the map, then carve one room per leaf area.
        root.SplitUntilThreshold(AreaThreshold, MinSplit, mapGenerationRandomSource);
        root.CarveRooms(
            MinMarginBetweenAreaBorderAndRoom,
            MinRoomWidth,
            MinRoomHeight,
            mapGenerationRandomSource
        );

        // Connect every room into one component with L-shaped corridors.
        _ = root.ConnectRooms(mapGenerationRandomSource);

        TransferPlanToMap(root);

        return PlayerStart(root);
    }

    /// <summary>
    /// Paints the finished BSP plan - room footprints, corridor paths, and a wall ring around them
    /// - onto <see cref="MapGeneratorBase.map"/>. Cells the plan doesn't touch stay void.
    /// </summary>
    void TransferPlanToMap(Node root)
    {
        PaintRoomFloors(root);
        PaintCorridorFloors(root);
        PaintWalls();
    }

    void PaintRoomFloors(Node root)
    {
        foreach (var leaf in root.Leaves())
        {
            if (leaf.Room is not { } room)
            {
                continue;
            }

            var floorSet = GetRandomElementWeighted(floorPool.TileSets, floorPool.Weights);
            foreach (var footprint in room.FootprintAreas)
            {
                for (int x = footprint.XMin; x < footprint.XMax; x++)
                {
                    for (int y = footprint.YMin; y < footprint.YMax; y++)
                    {
                        PlaceFloor(x, y, floorSet);
                    }
                }
            }
        }
    }

    void PaintCorridorFloors(Node root)
    {
        foreach (var node in root.AllNodes())
        {
            if (node.Corridor is not { } corridor)
            {
                continue;
            }

            var floorSet = GetRandomElementWeighted(floorPool.TileSets, floorPool.Weights);
            foreach (var point in corridor.Points())
            {
                if (map.Tiles[point.X, point.Y].TileType != TileType.Floor)
                {
                    PlaceFloor(point.X, point.Y, floorSet);
                }
            }
        }
    }

    void PaintWalls()
    {
        // Any void cell orthogonally or diagonally adjacent to a floor cell becomes a wall, giving
        // every room and corridor a one-cell-thick enclosure. Mirrors BasicDungeonGenerator.
        // AddWalls. Floors never sit on the perimeter (the root area is inset), so the -1/+1
        // neighbour lookups here stay in bounds.
        for (int x = 1; x < map.Width - 1; x++)
        {
            for (int y = 1; y < map.Height - 1; y++)
            {
                if (map.Tiles[x, y].TileType != TileType.Floor)
                {
                    continue;
                }

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (map.Tiles[x + dx, y + dy].TileType == TileType.Black)
                        {
                            PlaceWall(x + dx, y + dy);
                        }
                    }
                }
            }
        }
    }

    Tuple<int, int> PlayerStart(Node root)
    {
        // Drop the player on the connector point of the first room in the plan; that cell is
        // guaranteed to be on the room's floor. Fall back to a random open tile if the plan
        // somehow produced no rooms at all.
        foreach (var leaf in root.Leaves())
        {
            if (leaf.Room is { } room)
            {
                var connector = room.ConnectorPoint;
                return Tuple.Create(connector.X, connector.Y);
            }
        }

        return GetRandomUnblockedMapTile();
    }
}
