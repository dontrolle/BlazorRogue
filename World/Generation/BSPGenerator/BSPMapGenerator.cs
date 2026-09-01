using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

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
        RecordDoorCandidates(root);
        ChooseKeyRooms(root);

        return PlayerStart();
    }

    // The two rooms whose connector points are farthest apart: the player spawns in one (and, on
    // a level that has an up-stair, so does that stair), the down-stair goes in the other - so
    // there's always a traversal between where you come in and where you leave. Null only when the
    // plan produced no rooms at all, in which case the base fallbacks kick in.
    Room? playerRoom;
    Room? downStairRoom;

    /// <summary>
    /// Picks <see cref="playerRoom"/> / <see cref="downStairRoom"/> as the farthest-apart pair of
    /// carved rooms, so stairs and spawn end up in real rooms (never a corridor or an unreachable
    /// cave pocket) with distance between them.
    /// </summary>
    void ChooseKeyRooms(Node root)
    {
        var rooms = new List<Room>();
        foreach (var leaf in root.Leaves())
        {
            if (leaf.Room is { } room)
            {
                rooms.Add(room);
            }
        }

        if (rooms.Count == 0)
        {
            return;
        }
        if (rooms.Count == 1)
        {
            playerRoom = downStairRoom = rooms[0];
            return;
        }

        int bestDistanceSquared = -1;
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var a = rooms[i].ConnectorPoint;
                var b = rooms[j].ConnectorPoint;
                int dx = a.X - b.X;
                int dy = a.Y - b.Y;
                int distanceSquared = (dx * dx) + (dy * dy);
                if (distanceSquared > bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    playerRoom = rooms[i];
                    downStairRoom = rooms[j];
                }
            }
        }
    }

    Tuple<int, int> PlayerStart()
    {
        if (playerRoom is not { } room)
        {
            return GetRandomUnblockedMapTile();
        }

        var start = room.ConnectorPoint;
        return Tuple.Create(start.X, start.Y);
    }

    /// <summary>
    /// Places the up/down stairs (when the neighbouring levels exist) at the centre of
    /// <see cref="playerRoom"/> / <see cref="downStairRoom"/> rather than the base class's random
    /// unblocked tile - keeping them in rooms and, since every room connector is reachable, never
    /// stranded. Falls back to <see cref="MapGeneratorBase.AddStairs"/> if no rooms were carved.
    /// </summary>
    protected override void AddStairs()
    {
        if (playerRoom is null || downStairRoom is null)
        {
            base.AddStairs();
            return;
        }

        if (configuration.Levels.ContainsKey(levelNumber + 1))
        {
            var down = UnblockedFloorCellIn(downStairRoom);
            map.AddGameObject(new Stair(down.X, down.Y, StairDirection.Down));
        }

        if (configuration.Levels.ContainsKey(levelNumber - 1))
        {
            var up = UnblockedFloorCellIn(playerRoom);
            map.AddGameObject(new Stair(up.X, up.Y, StairDirection.Up));
        }
    }

    /// <summary>
    /// The room's connector point if nothing blocks it, otherwise the first unblocked cell in its
    /// footprint - AddStairs runs after the decoration passes, so a chest/altar could sit on the
    /// connector by the time a stair needs the cell.
    /// </summary>
    GridPoint UnblockedFloorCellIn(Room room)
    {
        if (!map.IsBlocked(room.ConnectorPoint.X, room.ConnectorPoint.Y))
        {
            return room.ConnectorPoint;
        }

        foreach (var footprint in room.FootprintAreas)
        {
            for (int x = footprint.XMin; x < footprint.XMax; x++)
            {
                for (int y = footprint.YMin; y < footprint.YMax; y++)
                {
                    if (!map.IsBlocked(x, y))
                    {
                        return new GridPoint(x, y);
                    }
                }
            }
        }

        return room.ConnectorPoint;
    }

    /// <summary>
    /// Flags the cell where each corridor crosses out of a room it connects as a candidate door
    /// spot, for the base <see cref="MapGeneratorBase.AddDoors"/> pass to fill. A corridor
    /// piercing a room's wall ring always leaves a clean one-tile gap there - the geometry
    /// <see cref="MapGeneratorBase.AddDoors"/> looks for - except where the crossing happens to
    /// coincide with the corridor's own elbow, which it then simply skips.
    /// </summary>
    void RecordDoorCandidates(Node root)
    {
        var roomFootprintCells = new HashSet<GridPoint>();
        foreach (var leaf in root.Leaves())
        {
            if (leaf.Room is not { } room)
            {
                continue;
            }

            foreach (var footprint in room.FootprintAreas)
            {
                for (int x = footprint.XMin; x < footprint.XMax; x++)
                {
                    for (int y = footprint.YMin; y < footprint.YMax; y++)
                    {
                        _ = roomFootprintCells.Add(new GridPoint(x, y));
                    }
                }
            }
        }

        var recorded = new HashSet<GridPoint>();
        foreach (var node in root.AllNodes())
        {
            if (node.Corridor is not { } corridor)
            {
                continue;
            }

            GridPoint? previous = null;
            foreach (var point in corridor.Points())
            {
                if (
                    previous is { } prev
                    && roomFootprintCells.Contains(prev) != roomFootprintCells.Contains(point)
                )
                {
                    // The cell on the corridor side of the boundary is the doorway.
                    var doorway = roomFootprintCells.Contains(prev) ? point : prev;
                    if (recorded.Add(doorway))
                    {
                        candidateDoors.Add(Tuple.Create(doorway.X, doorway.Y));
                    }
                }

                previous = point;
            }
        }
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
}
