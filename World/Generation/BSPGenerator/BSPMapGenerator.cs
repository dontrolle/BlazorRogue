using System;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.World.Generation.BSPGenerator;

/// <summary>
/// Dungeon generator that lays out rooms via binary space partitioning.
/// </summary>
/// <remarks>
/// <para>Data-driven knobs, all under the level's <c>map_generator.parameters.layout</c>:</para>
/// <list type="bullet">
/// <item><c>early_exit_chance</c> (double, default 0): per-node chance the partition stops early,
/// leaving a larger leaf. See <see cref="Node.SplitUntilThreshold"/>.</item>
/// <item><c>max_split_offset_from_center_proportion</c> (double 0-0.5, default unset): when set,
/// keeps each split near the middle of its area rather than anywhere in the legal range.</item>
/// <item><c>chance_of_leaf_having_no_room</c> (double, default 0): chance a given leaf is left
/// empty. Values must still leave at least one room somewhere.</item>
/// <item><c>min_room_width</c> / <c>min_room_height</c> (int, default 3): smallest room a carver
/// may produce.</item>
/// <item><c>room_carvers</c> (weighted id list, default all rectangular): shapes to pick from,
/// independently, per room. Ids: <c>rectangular</c>, <c>overlaid</c>, <c>circular</c>,
/// <c>cave</c>.</item>
/// </list>
/// </remarks>
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
    const int DefaultMinRoomWidth = 3;
    const int DefaultMinRoomHeight = 3;

    // LayoutSettings() and the Read* helpers are static for the same field-initializer reason
    // SelectWallSet is - see MapGeneratorBase: a field initializer may only touch static members
    // and the primary constructor's own parameters.
    readonly double earlyExitChance = LayoutSettings(settings).GetDouble("early_exit_chance", 0);
    readonly double? maxSplitOffsetFromCenterProportion = ReadOptionalProportion(settings);
    readonly double chanceOfLeafHavingNoRoom = LayoutSettings(settings)
        .GetDouble("chance_of_leaf_having_no_room", 0);
    readonly int minRoomWidth = LayoutSettings(settings)
        .GetInt("min_room_width", DefaultMinRoomWidth);
    readonly int minRoomHeight = LayoutSettings(settings)
        .GetInt("min_room_height", DefaultMinRoomHeight);
    readonly (string[] Ids, double[] Weights) roomCarverPool = ReadRoomCarverPool(settings);

    // Floor-set pool for rooms and corridors. Resolved once here (like BasicDungeonGenerator); a
    // fresh set is picked per room and per corridor so the map isn't one flat colour.
    readonly (TileSet[] TileSets, double[] Weights) floorPool = ResolveFloorPool(
        game.Configuration,
        settings,
        "common",
        game.Configuration.FloorSets
    );

    static SettingsMap LayoutSettings(SettingsMap settings) =>
        settings.GetMap("layout", SettingsMap.Empty);

    static double? ReadOptionalProportion(SettingsMap settings)
    {
        double raw = LayoutSettings(settings)
            .GetDouble("max_split_offset_from_center_proportion", double.NaN);
        return double.IsNaN(raw) ? null : raw;
    }

    static (string[] Ids, double[] Weights) ReadRoomCarverPool(SettingsMap settings)
    {
        var weighted = LayoutSettings(settings).GetWeightedIds("room_carvers", []);
        return ([.. weighted.Select(w => w.Id)], [.. weighted.Select(w => w.Weight)]);
    }

    protected override Tuple<int, int> CreateLayout()
    {
        // Inset the root one cell from the map border so no room or corridor floor can land on the
        // perimeter: the wall ring painted around the plan then always stays in bounds, and the
        // post-generation decoration passes (which assume floors are never on the edge) hold.
        var root = new Node(new Area(1, map.Width - 1, 1, map.Height - 1));

        // Partition the map, then carve one room per leaf area.
        root.SplitUntilThreshold(
            AreaThreshold,
            MinSplit,
            mapGenerationRandomSource,
            maxSplitOffsetFromCenterProportion,
            earlyExitChance
        );
        root.CarveRooms(
            MinMarginBetweenAreaBorderAndRoom,
            minRoomWidth,
            minRoomHeight,
            mapGenerationRandomSource,
            chanceOfLeafHavingNoRoom,
            selectCarver: BuildSelectCarver()
        );

        // Connect every room into one component with L-shaped corridors.
        _ = root.ConnectRooms(mapGenerationRandomSource);

        TransferPlanToMap(root);

        return PlayerStart(root);
    }

    /// <summary>
    /// Builds the per-node carver hook for <see cref="Node.CarveRooms"/> from the configured
    /// <c>room_carvers</c> pool, or <c>null</c> when none is configured (every room then uses the
    /// default <see cref="RectangularRoomCarver"/>). Each leaf rolls its own shape independently;
    /// internal nodes just pass the inherited carver through.
    /// </summary>
    Func<Node, IRoomCarver, Random, IRoomCarver>? BuildSelectCarver()
    {
        if (roomCarverPool.Ids.Length == 0)
        {
            return null;
        }

        return (node, inherited, _) =>
        {
            if (node.Left is not null || node.Right is not null)
            {
                return inherited;
            }

            return CarverForId(
                GetRandomElementWeighted(roomCarverPool.Ids, roomCarverPool.Weights)
            );
        };
    }

    static IRoomCarver CarverForId(string id) =>
        id switch
        {
            "rectangular" => RectangularRoomCarver.Instance,
            "overlaid" => OverlaidRectanglesRoomCarver.Instance,
            "circular" => CircularRoomCarver.Instance,
            // CaveRoomCarver's automaton can wall a small leaf off entirely (it throws when it
            // does); fall back to a plain rectangle for that leaf rather than aborting the map.
            "cave" => new FallbackRoomCarver(new CaveRoomCarver(), RectangularRoomCarver.Instance),
            _ => throw new InvalidOperationException(
                $"Unknown room carver id '{id}' in the bsp_map_generator 'room_carvers' setting; "
                    + "expected one of: rectangular, overlaid, circular, cave."
            ),
        };

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
