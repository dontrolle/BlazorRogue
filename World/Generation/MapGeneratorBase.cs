using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.AI;
using BlazorRogue.Components;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World.Generation;

/// <summary>
/// Base class for map-generators providing an implementation of
/// <c>IMapGenerator.GenerateMap()</c> that creates an empty map and
/// calls a set of overridable generator-functions in turn.
/// </summary>
/// <param name="width">Width of map to generate</param>
/// <param name="height">Height of map to generate</param>
/// <param name="levelNumber">Which level is the map set at?</param>
/// <param name="game">Game instance</param>
/// <param name="wallSet">Which main wall-tileset to use</param>
/// <param name="settings">Parsed configuration</param>
abstract class MapGeneratorBase(
    int width,
    int height,
    int levelNumber,
    Game game,
    TileSet wallSet,
    SettingsMap settings
) : IMapGenerator
{
    protected readonly Map map = new(width, height, wallSet, game);
    protected readonly Configuration configuration = game.Configuration;
    protected readonly int levelNumber = levelNumber;
    protected readonly Random mapGenerationRandomSource = new();

    // Decorations - shared by every DungeonGeneratorBase subclass, so levels.json groups these
    // under "common" rather than mixing them in with a specific generator's own layout parameters.
    // CommonSettings() is static because field initializers can't reference another instance
    // field/method of the same type being constructed - only static members and the primary
    // constructor's own parameters (e.g. `settings`) are allowed at this point.
    protected readonly double percentageChanceOfBones = CommonSettings(settings)
        .GetDouble("percentage_chance_of_bones", 0.05);
    protected readonly double percentageChanceOfTables = CommonSettings(settings)
        .GetDouble("percentage_chance_of_tables", 0.06);
    protected readonly double percentageChanceOfAltars = CommonSettings(settings)
        .GetDouble("percentage_chance_of_altars", 0.04);
    protected readonly double percentageChanceOfStatues = CommonSettings(settings)
        .GetDouble("percentage_chance_of_statues", 0.02);
    protected readonly double percentageChanceOfFountains = CommonSettings(settings)
        .GetDouble("percentage_chance_of_fountains", 0.015);
    protected readonly double percentageChanceOfSpiderWebInCorner = CommonSettings(settings)
        .GetDouble("percentage_chance_of_spider_web_in_corner", 0.25);
    protected readonly double percentageChanceOfTorch = CommonSettings(settings)
        .GetDouble("percentage_chance_of_torch", 0.25);
    protected readonly double percentageChanceOfChests = CommonSettings(settings)
        .GetDouble("percentage_chance_of_chests", 0.02);
    protected readonly double percentageChanceOfItems = CommonSettings(settings)
        .GetDouble("percentage_chance_of_items", 0.0);
    protected readonly double percentageChanceOfBarrels = CommonSettings(settings)
        .GetDouble("percentage_chance_of_barrels", 0.03);
    protected readonly double percentageChanceOfGraveyardClutter = CommonSettings(settings)
        .GetDouble("percentage_chance_of_graveyard_clutter", 0.02);
    protected readonly double percentageChanceOfRunes = CommonSettings(settings)
        .GetDouble("percentage_chance_of_runes", 0.015);
    protected readonly double percentageChanceOfLeaves = CommonSettings(settings)
        .GetDouble("percentage_chance_of_leaves", 0.03);
    protected readonly double percentageChanceOfDust = CommonSettings(settings)
        .GetDouble("percentage_chance_of_dust", 0.05);
    protected readonly double percentageChanceOfLilypad = CommonSettings(settings)
        .GetDouble("percentage_chance_of_lilypad", 0.15);
    protected readonly double percentageChanceOfPuddleLarge = CommonSettings(settings)
        .GetDouble("percentage_chance_of_puddle_large", 0.15);

    // Parallel to itemTypePoolWeights below. Both reference game.Configuration rather than the
    // `configuration` field further down - field initializers can't reference another instance
    // field of the same type being constructed (see the CommonSettings note above), only the
    // primary constructor's own parameters.
    protected readonly ItemType[] itemTypePool =
    [
        .. CommonSettings(settings)
            .GetWeightedIds("item_types", [])
            .Select(w => game.Configuration.ItemTypeById(w.Id)),
    ];
    protected readonly double[] itemTypePoolWeights =
    [
        .. CommonSettings(settings).GetWeightedIds("item_types", []).Select(w => w.Weight),
    ];

    // Independent chance each entry in candidateDoors actually becomes a door in AddDoors. Default
    // 1.0 keeps the historical "a door at every candidate" behaviour; a generator that records a
    // lot of candidates (e.g. BSPMapGenerator, one per room a corridor touches) can dial it down.
    protected readonly double percentageChanceOfDoor = CommonSettings(settings)
        .GetDouble("percentage_chance_of_door", 1.0);

    // protected so a subclass can read its own extra "common" content knobs (e.g. BSPMapGenerator's
    // monster density) in a field initializer; still static for the CS0236 reason above.
    protected static SettingsMap CommonSettings(SettingsMap settings) =>
        settings.GetMap("common", SettingsMap.Empty);

    // Sourced from Data/doorsets.json rather than hardcoded, so a new door-set (e.g. a lockable
    // type down the line) becomes available to generators automatically. Uses game.Configuration
    // rather than the `configuration` field for the same CS0236-adjacent reason as itemTypePool
    // above - see that field's comment.
    protected readonly string[] doorTypes = [.. game.Configuration.DoorSets.Keys];

    protected readonly List<Tuple<int, int>> candidateDoors = [];

    // Picks a random element ahead of the base constructor running, e.g. for choosing a subclass's
    // wall set from a constructor initializer - at that point the instance `random` field (and any
    // other instance state) hasn't been initialized yet, so it can't be used. Uses Random.Shared
    // rather than a seedable source for the same reason.
#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
    protected static T SelectRandom<T>(IEnumerable<T> elements) =>
        elements.ElementAt(Random.Shared.Next(elements.Count()));
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

    protected static T SelectRandomWeighted<T>(T[] elements, double[] weights) =>
        WeightedPick(elements, weights, Random.Shared);

    /// <summary>
    /// Resolves the wall <see cref="TileSet"/> for a level: weighted-picks among the ids listed in
    /// the level's <c>common.wall_tile_set</c> setting, or - when that's unspecified, e.g. for
    /// levels with no "common" parameters at all - falls back to a uniform pick over
    /// <paramref name="defaultPool"/> (the generator's whole level-type pool).
    /// </summary>
    protected static TileSet SelectWallSet(
        Configuration configuration,
        SettingsMap settings,
        IEnumerable<TileSet> defaultPool
    )
    {
        var weighted = CommonSettings(settings).GetWeightedIds("wall_tile_set", []);
        if (weighted.Count == 0)
        {
            return SelectRandom(defaultPool);
        }

        TileSet[] wallSets = [.. weighted.Select(w => configuration.WallSetById(w.Id))];
        double[] weights = [.. weighted.Select(w => w.Weight)];
        return SelectRandomWeighted(wallSets, weights);
    }

    /// <summary>
    /// Resolves a named floor-set pool (e.g. "common" or "special") for a level: weighted ids
    /// listed under the level's <c>common.floor_tile_set.&lt;pool&gt;</c> setting, or - when that
    /// pool is unspecified/empty - a uniform pool over <paramref name="defaultPool"/> (typically
    /// every known floor-set). Returns the resolved pool rather than a single pick, since callers
    /// may need to pick from it repeatedly (e.g. once per room).
    /// </summary>
    protected static (TileSet[] TileSets, double[] Weights) ResolveFloorPool(
        Configuration configuration,
        SettingsMap settings,
        string pool,
        IEnumerable<TileSet> defaultPool
    )
    {
        var weighted = CommonSettings(settings)
            .GetMap("floor_tile_set", SettingsMap.Empty)
            .GetWeightedIds(pool, []);
        if (weighted.Count == 0)
        {
            TileSet[] defaultTileSets = [.. defaultPool];
            return (defaultTileSets, [.. defaultTileSets.Select(_ => 1.0)]);
        }

        TileSet[] floorSets = [.. weighted.Select(w => configuration.FloorSetById(w.Id))];
        double[] weights = [.. weighted.Select(w => w.Weight)];
        return (floorSets, weights);
    }

    /// <summary>
    /// Implementation of IMapGenerator.GenerateMap that calls a set of overridable generator-functions in turn:
    ///     CreateLayout(), AddDoors(), AddRandomPostMapGenerationDecorations(),
    ///     AddStairs(), AddPlayer(), AddMonsters()
    ///
    ///  and ensures that map.PostGenInitialize() is called.
    /// </summary>
    /// <param name="existingPlayer">An existing player object, if relevant.</param>
    /// <returns>The generated map.</returns>
    public virtual Map GenerateMap(Moveable? existingPlayer = null)
    {
        var playerPos = CreateLayout();

        // Before doors/decorations/stairs/monsters, so they all steer clear of pool tiles (their
        // placement is opt-in on TileType.Floor, and a Liquid tile no longer reports as one).
        AddLiquidPools(playerPos);

        AddDoors();
        AddRandomPostMapGenerationDecorations();

        // After decorations, so its own eligibility check naturally steers clear of tiles they
        // already claimed - before stairs/monsters, see AddFenceEnclosures' own doc comment for
        // the accepted (pre-existing, not new) risk that leaves open.
        AddFenceEnclosures(playerPos);

        AddStairs();

        AddPlayer(playerPos, existingPlayer);

        AddMonsters();

        // initialize various maps and so on in Map (it there a better place to do this?)
        map.PostGenInitalize();

        return map;
    }

    /// <summary>
    /// Basic method for adding a player object at the spot given by <paramref name="playerPos"/>.
    /// If <paramref name="existingPlayer"/> is not set, then a new player will be created.
    /// </summary>
    /// <param name="playerPos">Position to add the player at.</param>
    /// <param name="existingPlayer">And existing player object, if relevant.</param>
    protected virtual void AddPlayer(Tuple<int, int> playerPos, Moveable? existingPlayer)
    {
        if (existingPlayer is null)
        {
            var heroType = GetRandomElement(configuration.HeroTypes).Value;
            existingPlayer = new Moveable(playerPos, null, heroType, new InventoryComponent());
        }
        else
        {
            existingPlayer.PlaceAt(playerPos.Item1, playerPos.Item2);
        }
        map.AddPlayer(existingPlayer);
    }

    /// <summary>
    /// Guarantees a down-stair (to levelNumber + 1) and/or an up-stair (to levelNumber - 1) exist,
    /// whenever those levels are defined in levels.json - unlike the percentage-chance decorations
    /// above, missing stairs would make part of the dungeon unreachable.
    /// </summary>
    protected virtual void AddStairs()
    {
        Tuple<int, int>? downPos = null;
        if (configuration.Levels.ContainsKey(levelNumber + 1))
        {
            downPos = GetRandomUnblockedMapTile();
            map.AddGameObject(new Stair(downPos.Item1, downPos.Item2, StairDirection.Down));
        }

        if (configuration.Levels.ContainsKey(levelNumber - 1))
        {
            Tuple<int, int> upPos;
            do
            {
                upPos = GetRandomUnblockedMapTile();
            } while (
                downPos is not null && upPos.Item1 == downPos.Item1 && upPos.Item2 == downPos.Item2
            );

            map.AddGameObject(new Stair(upPos.Item1, upPos.Item2, StairDirection.Up));
        }
    }

    /// <summary>
    /// Basic simple method for placing some random monsters in a generated map. Subclasses with a
    /// room structure typically override this to distribute monsters room-by-room (see
    /// <c>BSPMapGenerator</c>); <see cref="AddMonsterAt(int, int)"/> is the shared spawn helper.
    /// </summary>
    protected virtual void AddMonsters()
    {
        const int noOfRandomMonsters = 10;

        for (int i = 0; i < noOfRandomMonsters; i++)
        {
            var pos = GetRandomUnblockedMapTile();
            _ = AddMonsterAt(pos.Item1, pos.Item2);
        }
    }

    /// <summary>
    /// Creates a monster of a uniformly-random configured type at
    /// (<paramref name="x"/>, <paramref name="y"/>), wires up its AI component, and registers it
    /// with the map. Does not check whether the tile is free - the caller owns that.
    /// </summary>
    protected Moveable AddMonsterAt(int x, int y) =>
        AddMonsterAt(x, y, GetRandomElement(configuration.MonsterTypes).Value);

    /// <summary>
    /// Same as the random overload, but with a specific <paramref name="monsterType"/>
    /// instead of a uniformly-random one - e.g. for a deterministic dev-only level that wants a
    /// known, repeatable monster (see FenceGalleryMapGenerator).
    /// </summary>
    protected Moveable AddMonsterAt(int x, int y, MoveableType monsterType)
    {
        var monster = new Moveable(
            Tuple.Create(x, y),
            AIComponentFactory.Create(
                monsterType.AIComponentId,
                map,
                monsterType.AIComponentSettings
            ),
            monsterType
        );
        map.AddMonster(monster);
        return monster;
    }

    /// <summary>
    /// Should create the basic layout of the map - placing walls and floors.
    /// </summary>
    /// <returns>A tuple representing the player position.</returns>
    protected abstract Tuple<int, int> CreateLayout();

    /// <summary>
    /// Adds doors in suitable places - assumes that candidate door spots have been added to the candidateDoors list.
    /// </summary>
    protected virtual void AddDoors()
    {
        foreach (var candidateDoor in candidateDoors)
        {
            if (mapGenerationRandomSource.NextDouble() >= percentageChanceOfDoor)
            {
                continue;
            }

            int x = candidateDoor.Item1;
            int y = candidateDoor.Item2;
            if (map.Tiles[x, y].TileType == TileType.Floor)
            {
                // Check if horizontal makes sense
                if (
                    x > 1
                    && x < map.Width - 1
                    && map.Tiles[x - 1, y].TileType == TileType.Wall
                    && map.Tiles[x + 1, y].TileType == TileType.Wall
                )
                {
                    if (!MapTileContainsDoor(x, y))
                    {
                        map.AddGameObject(
                            new Door(
                                x,
                                y,
                                GetRandomElement(doorTypes),
                                Orientation.Horizontal,
                                GetRandomBool()
                            )
                        );
                    }
                }
                // Check if vertical makes sense
                else if (
                    y > 1
                    && y < map.Height - 1
                    && map.Tiles[x, y - 1].TileType == TileType.Wall
                    && map.Tiles[x, y + 1].TileType == TileType.Wall
                )
                {
                    if (!MapTileContainsDoor(x, y))
                    {
                        map.AddGameObject(
                            new Door(
                                x,
                                y,
                                GetRandomElement(doorTypes),
                                Orientation.Vertical,
                                GetRandomBool()
                            )
                        );
                    }
                }
            }
        }
    }

    /// <summary>
    /// Simple method that uses brute-force to find a random unblocked tile.
    /// </summary>
    /// <returns>An unblocked tile.</returns>
    /// <exception cref="InvalidOperationException">Throws, if no unblocked tile was found.</exception>
    protected Tuple<int, int> GetRandomUnblockedMapTile()
    {
        int maxSearch = 200;
        for (int i = 0; i < maxSearch; i++)
        {
            int x = mapGenerationRandomSource.Next(0, map.Width);
            int y = mapGenerationRandomSource.Next(0, map.Height);

            if (!map.IsBlocked(x, y) && map.Tiles[x, y].Liquid is null)
                return Tuple.Create(x, y);
        }
        throw new InvalidOperationException(
            $"Couldn't find an unblocked tile on map in {maxSearch} tries!"
        );
    }

    /// <summary>
    /// Places a few liquid pools (see <c>Data/liquidsets.json</c>) if the level's
    /// <c>common.liquid_pools</c> settings ask for them - absent settings mean no pools. Each pool
    /// is a rough disc carved over existing floor tiles only, never over walls, existing game
    /// objects, or the player's start. <c>always</c> ids get one guaranteed pool each; <c>types</c>
    /// ids are the weighted pool for the remaining <c>count_min</c>..<c>count_max</c> random pools.
    /// </summary>
    protected virtual void AddLiquidPools(Tuple<int, int> playerPos)
    {
        var poolSettings = CommonSettings(settings).GetMap("liquid_pools", SettingsMap.Empty);
        var weightedTypes = poolSettings.GetWeightedIds("types", []);
        var alwaysTypes = poolSettings.GetWeightedIds("always", []);
        if (weightedTypes.Count == 0 && alwaysTypes.Count == 0)
        {
            return;
        }

        int radiusMin = poolSettings.GetInt("radius_min", 1);
        int radiusMax = poolSettings.GetInt("radius_max", 3);

        int RandomRadius() => mapGenerationRandomSource.Next(radiusMin, radiusMax + 1);

        void PlacePool(LiquidType liquid)
        {
            if (TryFindPoolCentre(playerPos, out int centreX, out int centreY))
            {
                CarveLiquidPool(centreX, centreY, RandomRadius(), liquid, playerPos);
            }
        }

        // Guaranteed pools first, so a level that wants (say) an acid pool for sure always gets one.
        foreach (var (id, _) in alwaysTypes)
        {
            PlacePool(configuration.LiquidTypeById(id));
        }

        if (weightedTypes.Count == 0)
        {
            return;
        }

        LiquidType[] liquidTypes =
        [
            .. weightedTypes.Select(t => configuration.LiquidTypeById(t.Id)),
        ];
        double[] weights = [.. weightedTypes.Select(t => t.Weight)];

        int countMin = poolSettings.GetInt("count_min", 2);
        int countMax = poolSettings.GetInt("count_max", 4);

        int poolCount = mapGenerationRandomSource.Next(countMin, countMax + 1);
        for (int i = 0; i < poolCount; i++)
        {
            PlacePool(GetRandomElementWeighted(liquidTypes, weights));
        }
    }

    bool TryFindPoolCentre(Tuple<int, int> playerPos, out int x, out int y)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            x = mapGenerationRandomSource.Next(0, map.Width);
            y = mapGenerationRandomSource.Next(0, map.Height);
            if (IsPoolEligible(x, y, playerPos))
            {
                return true;
            }
        }

        x = 0;
        y = 0;
        return false;
    }

    void CarveLiquidPool(
        int centreX,
        int centreY,
        int radius,
        LiquidType liquid,
        Tuple<int, int> playerPos
    )
    {
        for (int x = centreX - radius; x <= centreX + radius; x++)
        {
            for (int y = centreY - radius; y <= centreY + radius; y++)
            {
                int dx = x - centreX;
                int dy = y - centreY;
                if ((dx * dx) + (dy * dy) > radius * radius)
                {
                    continue;
                }

                if (IsPoolEligible(x, y, playerPos))
                {
                    map.SetLiquidTile(x, y, liquid);
                }
            }
        }
    }

    bool IsPoolEligible(int x, int y, Tuple<int, int> playerPos)
    {
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return false;
        }

        var tile = map.Tiles[x, y];
        if (tile.TileType != TileType.Floor || tile.Blocking)
        {
            return false;
        }

        if (map.GameObjectByCoord[x, y].Any())
        {
            return false;
        }

        // keep the player's start tile and the ring around it clear of hazards
        return Math.Abs(x - playerPos.Item1) > 1 || Math.Abs(y - playerPos.Item2) > 1;
    }

    /// <summary>
    /// Places a few rectangular fence enclosures (see Data/decorations.json's fence_* types and
    /// PlaceFenceEnclosure below, dontrolle/BlazorRogue-internal#86) if the level's
    /// <c>common.fence_enclosures</c> settings ask for them - absent settings (or a zero
    /// <c>count_max</c>) mean none, same "opt in explicitly" default as <see cref="AddLiquidPools"/>.
    /// Each enclosure is a rough rectangle sampled over existing floor tiles only, never over
    /// walls, existing game objects, or the player's start - same sampling strategy as
    /// AddLiquidPools/TryFindPoolCentre, just over a rectangle instead of a disc. The enclosure's
    /// actual footprint needs one extra free column of floor on each side beyond its own
    /// width x height (see IsFenceEnclosureEligible) for the non-blocking companion decorations
    /// that complete the west/east wall art (see PlaceFenceEnclosure).
    /// </summary>
    protected virtual void AddFenceEnclosures(Tuple<int, int> playerPos)
    {
        var fenceSettings = CommonSettings(settings).GetMap("fence_enclosures", SettingsMap.Empty);
        int countMax = fenceSettings.GetInt("count_max", 0);
        if (countMax <= 0)
        {
            return;
        }

        int countMin = fenceSettings.GetInt("count_min", 0);
        int widthMin = fenceSettings.GetInt("width_min", 3);
        int widthMax = fenceSettings.GetInt("width_max", 3);
        int heightMin = fenceSettings.GetInt("height_min", 3);
        int heightMax = fenceSettings.GetInt("height_max", 3);

        int count = mapGenerationRandomSource.Next(countMin, countMax + 1);
        for (int i = 0; i < count; i++)
        {
            int width = mapGenerationRandomSource.Next(widthMin, widthMax + 1);
            int height = mapGenerationRandomSource.Next(heightMin, heightMax + 1);

            if (TryFindFenceEnclosureOrigin(width, height, playerPos, out int x0, out int y0))
            {
                int gateColumn = mapGenerationRandomSource.Next(1, width - 1);
                PlaceFenceEnclosure(x0, y0, width, height, gateColumn);
            }
        }
    }

    bool TryFindFenceEnclosureOrigin(
        int width,
        int height,
        Tuple<int, int> playerPos,
        out int x0,
        out int y0
    )
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            int cx = mapGenerationRandomSource.Next(0, map.Width);
            int cy = mapGenerationRandomSource.Next(0, map.Height);
            if (IsFenceEnclosureEligible(cx, cy, width, height, playerPos))
            {
                x0 = cx;
                y0 = cy;
                return true;
            }
        }

        x0 = 0;
        y0 = 0;
        return false;
    }

    bool IsFenceEnclosureEligible(int x0, int y0, int width, int height, Tuple<int, int> playerPos)
    {
        for (int x = x0; x < x0 + width; x++)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                if (!IsPoolEligible(x, y, playerPos))
                {
                    // Reuses IsPoolEligible's exact bounds/floor/blocking/occupied/player-buffer
                    // checks - a fence tile has the same eligibility requirements a liquid pool
                    // tile does, despite the unrelated name.
                    return false;
                }
            }
        }

        // The north and south wall rows use the tall-picket "Infront" shapes (fence_end_west/
        // fence_opening/fence_end_east/fence_straight/fence_corner_sw/fence_corner_se, see
        // PlaceFenceEnclosure) - that art isn't designed to coexist with a real dungeon wall's own
        // decoration bleeding onto the same tile (Tile.RenderHalfWall bleeds a wall's cap onto the
        // tile north of it; RenderShadow bleeds a shadow onto the tile south of it - both a
        // VerticalOffset trick, unrelated to fences, universal to every wall in the game). Keep
        // both wall rows clear of an adjacent real wall. The west/east side walls (thin vertical
        // posts, not "Infront") don't have this problem, so aren't checked here.
        for (int x = x0; x < x0 + width; x++)
        {
            if (IsWall(x, y0 - 1) || IsWall(x, y0 + height))
            {
                return false;
            }
        }

        // Every shape along the west/east sides - the two side walls, the north row's end caps, and
        // the south row's corners - reads as a single spindly line/post on its own; the source art
        // is designed to be composited with a mirrored companion tile immediately outside the
        // enclosure on that row (see PlaceFenceEnclosure), so both full outer columns must be free.
        for (int dy = 0; dy < height; dy++)
        {
            if (
                !IsPoolEligible(x0 - 1, y0 + dy, playerPos)
                || !IsPoolEligible(x0 + width, y0 + dy, playerPos)
            )
            {
                return false;
            }
        }

        return true;
    }

    bool IsWall(int x, int y) =>
        x >= 0
        && y >= 0
        && x < map.Width
        && y < map.Height
        && map.Tiles[x, y].TileType == TileType.Wall;

    /// <summary>
    /// Draws a complete rectangular fence enclosure with its top-left corner at
    /// (<paramref name="x0"/>, <paramref name="y0"/>): fence_end_west/fence_straight-or-
    /// fence_opening-repeated/fence_end_east along the top, fence_wall_west/fence_wall_east
    /// repeated down both sides, fence_corner_sw/fence_straight-repeated/fence_corner_se along the
    /// bottom - the same shape validated by FenceGalleryMapGenerator's fixed 3x3 example and the
    /// composite mockups in dontrolle/BlazorRogue-internal#86. Also places a non-blocking companion
    /// tile one column outside the footprint for every shape along the west/east sides - the side
    /// walls (fence_wall_east_companion/fence_wall_west_companion), the top row's end caps
    /// (fence_post_east/fence_post_west), and the bottom row's corners (fence_corner_sw_companion/
    /// fence_corner_se_companion, using the "continues" dimpled art since the corner's own rail
    /// continues into it) - completing each shape's own single-line/dangling art into a proper
    /// double-rail. Only the west/east wall columns actually block movement; every companion is
    /// purely cosmetic. <paramref name="gateColumn"/> is 1-indexed from the west wall (so 0 and
    /// width-1, always the corners, aren't valid gate positions). Pure drawing - no eligibility
    /// checking; callers must ensure the whole <paramref name="width"/> x <paramref name="height"/>
    /// footprint, plus one extra column on each side for the companions, is free first (see
    /// AddFenceEnclosures/IsFenceEnclosureEligible). Requires width &gt;= 3 and height &gt;= 3 - the
    /// only sizes with a gate and a corner-to-cap vertical run actually validated.
    /// </summary>
    protected void PlaceFenceEnclosure(int x0, int y0, int width, int height, int gateColumn)
    {
        if (width < 3 || height < 3)
        {
            throw new ArgumentException(
                $"A fence enclosure needs to be at least 3x3 (got {width}x{height})."
            );
        }
        if (gateColumn < 1 || gateColumn > width - 2)
        {
            throw new ArgumentException(
                $"gateColumn must be between 1 and width - 2 (got {gateColumn} for width {width})."
            );
        }

        for (int dx = 0; dx < width; dx++)
        {
            string typeId =
                dx == 0 ? "fence_end_west"
                : dx == width - 1 ? "fence_end_east"
                : dx == gateColumn ? "fence_opening"
                : "fence_straight";
            PlaceFence(x0 + dx, y0, typeId);
        }

        // fence_end_west/fence_end_east's own art dangles with no cap on their outer side - the
        // non-blocking fence_post_east/fence_post_west companions complete it, one column outside
        // the footprint (same treatment as the side walls' companions below).
        PlaceFence(x0 - 1, y0, "fence_post_east");
        PlaceFence(x0 + width, y0, "fence_post_west");

        for (int dy = 1; dy < height - 1; dy++)
        {
            PlaceFence(x0, y0 + dy, "fence_wall_west");
            PlaceFence(x0 + width - 1, y0 + dy, "fence_wall_east");

            // fence_wall_west's rail sits at its own west edge and fence_wall_east's at its own
            // east edge - each reads as a spindly single line alone, but the two interlock into a
            // proper double-rail once adjacent (confirmed by compositing the source art, see
            // dontrolle/BlazorRogue-internal#86). These companions are the same images, placed one
            // column outside the enclosure's own footprint, but non-blocking - the enclosure's real
            // blocking edge is still just the fence_wall_west/east column placed above.
            PlaceFence(x0 - 1, y0 + dy, "fence_wall_east_companion");
            PlaceFence(x0 + width, y0 + dy, "fence_wall_west_companion");
        }

        for (int dx = 0; dx < width; dx++)
        {
            string typeId =
                dx == 0 ? "fence_corner_sw"
                : dx == width - 1 ? "fence_corner_se"
                : "fence_straight";
            PlaceFence(x0 + dx, y0 + height - 1, typeId);
        }

        // Same non-blocking-companion treatment as the top row's end caps, one column outside the
        // footprint - the corners' own vertical rail continues into fence_corner_sw/fence_corner_se
        // from outside, so their companions use the dimpled "continues" variants (fence_11/fence_15)
        // rather than the plain fence_6/fence_7 the side walls use.
        PlaceFence(x0 - 1, y0 + height - 1, "fence_corner_sw_companion");
        PlaceFence(x0 + width, y0 + height - 1, "fence_corner_se_companion");
    }

    /// <summary>
    /// Places one fence_* decoration (see Data/decorations.json, dontrolle/BlazorRogue-internal#86)
    /// by id at (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    protected void PlaceFence(int x, int y, string typeId) =>
        map.AddGameObject(
            new StaticDecorativeObject(x, y, configuration.StaticDecorativeObjectTypes[typeId])
        );

    /// <summary>
    /// Adds decorations randomly on walls and floors.
    /// </summary>
    protected virtual void AddRandomPostMapGenerationDecorations()
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                PlaceTorchIfEligible(x, y);
                PlaceDustIfEligible(x, y);
                PlaceLilypadIfEligible(x, y);

                AddRandomPostGenFloorDecorationsAt(x, y);
            }
        }
    }

    /// <summary>
    /// Adds random decorations to the floor at (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    protected void AddRandomPostGenFloorDecorationsAt(int x, int y)
    {
        if (map.Tiles[x, y].TileType == TileType.Floor)
        {
            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfBones
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                map.AddGameObject(
                    new StaticDecorativeObject(
                        x,
                        y,
                        configuration.StaticDecorativeObjectTypes["bones"]
                    )
                );
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfTables
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
                && !TileOccupied(x, y)
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    // mostly plain tables, occasionally an alchemy or paperwork variant
                    string tableId = "table";
                    int tableRoll = mapGenerationRandomSource.Next(0, 4);
                    if (tableRoll == 1)
                    {
                        tableId = "table_lab";
                    }
                    else if (tableRoll == 2)
                    {
                        tableId = "table_papers";
                    }

                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes[tableId]
                        )
                    );
                }
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfAltars
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
                && !TileOccupied(x, y)
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    string altarId =
                        mapGenerationRandomSource.Next(0, 4) == 0 ? "altar_skull" : "altar_blood";

                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes[altarId]
                        )
                    );
                }
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfBarrels
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
                && !TileOccupied(x, y)
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes["barrel"]
                        )
                    );
                }
            }

            // Statue's "top" half bleeds visually onto the tile above (see Statue.Render), so that
            // tile needs to be open, empty floor too - otherwise the top would render over a wall
            // or another decoration (e.g. a second statue stacked right below this one, its "top"
            // landing on this one's "bottom"), and a player couldn't actually walk behind it as
            // intended.
            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfStatues
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
                && !TileOccupied(x, y)
                && map.Tiles[x, y - 1].TileType == TileType.Floor
                && !MapTileContainsDoor(x, y - 1)
                && !map.IsBlocked(x, y - 1)
                && !map.GameObjectByCoord[x, y - 1].Any()
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    map.AddGameObject(new Statue(x, y));
                }
            }

            // Fountain's own tile isn't Blocking either (see Fountain), so this check mirrors
            // Statue's above in the opposite direction: instead of requiring open floor above (for
            // Statue's own bleed), it requires a plain wall above (for Fountain's own bleed) - not
            // a door frame, and not already carrying a torch or dust wall-piece placed by
            // PlaceTorchIfEligible/PlaceDustIfEligible earlier in this same x-column (see
            // AddRandomPostMapGenerationDecorations's row-by-row loop order - row y-1 is always
            // fully processed before row y for a fixed x). No explicit !map.IsBlocked(x, y - 1)
            // check is needed - a Wall tile is unconditionally blocked already.
            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfFountains
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
                && !TileOccupied(x, y)
                && map.Tiles[x, y - 1].TileType == TileType.Wall
                && !MapTileContainsDoor(x, y - 1)
                && !map.GameObjectByCoord[x, y - 1].Any()
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    map.AddGameObject(new Fountain(x, y));
                }
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfGraveyardClutter
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
                && !TileOccupied(x, y)
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    string clutterId = mapGenerationRandomSource.Next(0, 4) switch
                    {
                        0 => "grave",
                        1 => "grave_broken",
                        2 => "coffin",
                        _ => "coffin_open",
                    };

                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes[clutterId]
                        )
                    );
                }
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfRunes
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                map.AddGameObject(
                    new StaticDecorativeObject(
                        x,
                        y,
                        configuration.StaticDecorativeObjectTypes["rune"]
                    )
                );
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfLeaves
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                string leavesId =
                    mapGenerationRandomSource.Next(0, 2) == 0 ? "leaves_green" : "leaves_brown";

                map.AddGameObject(
                    new StaticDecorativeObject(
                        x,
                        y,
                        configuration.StaticDecorativeObjectTypes[leavesId]
                    )
                );
            }

            // Splashes only near a liquid edge, tinted to match whichever liquid it's bordering
            // (puddle_large has one image variant per LiquidType.Id - see decorations.json).
            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfPuddleLarge
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                string? adjacentLiquidId =
                    map.Tiles[x, y - 1].Liquid?.Id
                    ?? map.Tiles[x, y + 1].Liquid?.Id
                    ?? map.Tiles[x - 1, y].Liquid?.Id
                    ?? map.Tiles[x + 1, y].Liquid?.Id;

                if (adjacentLiquidId is not null)
                {
                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes["puddle_large"],
                            adjacentLiquidId
                        )
                    );
                }
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfChests
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                string chestId = "chest_silver";
                int gold = mapGenerationRandomSource.Next(0, 4);
                if (mapGenerationRandomSource.Next(0, 4) == 0)
                {
                    chestId = "chest_gold";
                    gold += 4;
                }

                map.AddGameObject(
                    new Chest(x, y, chestId, new InventoryComponent() { Gold = gold })
                );
            }

            if (
                itemTypePool.Length > 0
                && mapGenerationRandomSource.NextDouble() < percentageChanceOfItems
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                var itemType = GetRandomElementWeighted(itemTypePool, itemTypePoolWeights);
                map.AddGameObject(new Item(x, y, itemType));
            }

            // in the following we rely on floors never being placed on the perimeter tiles, else we could do
            //if(x > 0 && x < map.Width -1 && y > 0 && y < map.Height - 1){ ... }
            if (mapGenerationRandomSource.NextDouble() < percentageChanceOfSpiderWebInCorner)
            {
                bool wallAbove = map.Tiles[x, y - 1].TileType == TileType.Wall;
                bool wallBelow = map.Tiles[x, y + 1].TileType == TileType.Wall;
                bool wallLeft = map.Tiles[x - 1, y].TileType == TileType.Wall;
                bool wallRight = map.Tiles[x + 1, y].TileType == TileType.Wall;

                string corner = "";
                int verticalOffset = 0;
                if (wallAbove && wallLeft)
                {
                    corner = "NW";
                    verticalOffset = -1;
                }
                else if (wallBelow && wallLeft)
                {
                    corner = "SW";
                }
                else if (wallBelow && wallRight)
                {
                    corner = "SE";
                }
                else if (wallAbove && wallRight)
                {
                    corner = "NE";
                    verticalOffset = -1;
                }

                if (!string.IsNullOrEmpty(corner))
                {
                    // i.e., we found a suitable spot for a spiderweb
                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes["corner_spiderweb"],
                            corner,
                            verticalOffset
                        )
                    );
                }
            }
        }
    }

    /// <summary>
    /// Returns the number of blocking tiles around the map-tile at (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    protected int NumberOfSurroundingBlockingSpots(int x, int y)
    {
        int numberOfSurroundingBlockingSpots = 0;

        for (int dx = -1; dx < 2; dx++)
        {
            for (int dy = -1; dy < 2; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                if (map.IsBlocked(x + dx, y + dy))
                {
                    numberOfSurroundingBlockingSpots++;
                }
            }
        }

        return numberOfSurroundingBlockingSpots;
    }

    /// <summary>
    /// Has a chance (<c>percentageChanceOfTorch</c>) of adding a torch at (<paramref name="x"/>, <paramref name="y"/>).
    /// </summary>
    protected void PlaceTorchIfEligible(int x, int y)
    {
        // Wall tiles with a floor tile directly below (and no door there) may get a torch. Purely a
        // content-placement roll, unlike Tile.Render's half-wall/wall-face/edge art - a torch isn't
        // implied by tile geometry the way that art is, so it stays a generator decision.

        if (map.Tiles[x, y].TileType != TileType.Wall)
        {
            return;
        }

        if (y >= map.Height - 1 || map.Tiles[x, y + 1].TileType != TileType.Floor)
        {
            return;
        }

        if (MapTileContainsDoor(x, y + 1))
        {
            return;
        }

        if (mapGenerationRandomSource.NextDouble() < percentageChanceOfTorch)
        {
            map.AddGameObject(new Torch(x, y));
        }
    }

    /// <summary>
    /// Has a chance (<c>percentageChanceOfDust</c>) of adding a paired dust decoration at
    /// (<paramref name="x"/>, <paramref name="y"/>) - one piece drawn on the wall tile itself, one
    /// on the floor tile directly below it. Same wall-tile-with-floor-below geometry as
    /// <see cref="PlaceTorchIfEligible"/>, but places two GameObjects instead of one. Which image
    /// pair is used depends on whether the floor tile below is itself hugging a room corner - i.e.
    /// a side wall sits immediately west or east of *that floor tile* (one row down, not beside the
    /// wall tile itself - a rectangular room's top wall is a contiguous run, so the tile beside a
    /// top-wall tile is essentially always another wall; the corner only becomes visible one row
    /// down, next to the perpendicular side wall) - or a middle-of-the-run straight segment.
    /// </summary>
    protected void PlaceDustIfEligible(int x, int y)
    {
        if (map.Tiles[x, y].TileType != TileType.Wall)
        {
            return;
        }

        if (y >= map.Height - 1 || map.Tiles[x, y + 1].TileType != TileType.Floor)
        {
            return;
        }

        if (MapTileContainsDoor(x, y + 1) || map.IsBlocked(x, y + 1))
        {
            return;
        }

        if (mapGenerationRandomSource.NextDouble() < percentageChanceOfDust)
        {
            // A floor tile with a side wall on both sides (a 1-wide nook) is treated as a NW
            // corner - an arbitrary but harmless tie-break, since that shape is rare.
            bool sideWallToWest = x > 0 && map.Tiles[x - 1, y + 1].TileType == TileType.Wall;
            bool sideWallToEast =
                x < map.Width - 1 && map.Tiles[x + 1, y + 1].TileType == TileType.Wall;

            var (wallTag, floorTag) =
                sideWallToWest ? ("wall_nw", "floor_nw")
                : sideWallToEast ? ("wall_ne", "floor_ne")
                : ("wall_straight", "floor_straight");

            var dustType = configuration.StaticDecorativeObjectTypes["dust"];
            map.AddGameObject(new StaticDecorativeObject(x, y, dustType, wallTag));
            map.AddGameObject(new StaticDecorativeObject(x, y + 1, dustType, floorTag));
        }
    }

    /// <summary>
    /// Has a chance (<c>percentageChanceOfLilypad</c>) of adding a lilypad at
    /// (<paramref name="x"/>, <paramref name="y"/>) - only on a liquid tile whose
    /// <c>LiquidType.Name</c> is "water" (blue/green/teal), never mud/acid/lava. No explicit
    /// image tag is passed, so <see cref="StaticDecorativeObject"/> randomly picks between the two
    /// lilypad species ("a"/"b"), each animated via its own CSS class - see
    /// <see cref="Rendering.HandAuthoredSpriteAnimations"/>.
    /// </summary>
    protected void PlaceLilypadIfEligible(int x, int y)
    {
        if (map.Tiles[x, y].Liquid is not { Name: "water" })
        {
            return;
        }

        if (MapTileContainsDoor(x, y) || map.IsBlocked(x, y))
        {
            return;
        }

        if (mapGenerationRandomSource.NextDouble() < percentageChanceOfLilypad)
        {
            map.AddGameObject(
                new StaticDecorativeObject(
                    x,
                    y,
                    configuration.StaticDecorativeObjectTypes["lilypad"]
                )
            );
        }
    }

    /// <summary>
    /// Does the map contain a door at (x,y)?
    /// </summary>
    protected bool MapTileContainsDoor(int x, int y) =>
        map.GameObjectByCoord[x, y].Any(go => go is Door);

    /// <summary>
    /// Does (x,y) already hold a GameObject whose art visually fills the tile (see
    /// GameObject.OccupiesTile) - e.g. a statue or another solid prop - so a second one shouldn't
    /// be placed on top of it? Deliberately narrower than <see cref="Map.IsBlocked"/>: scatter
    /// decorations (bones, runes, leaves, ...) never set OccupiesTile, so they can still coexist
    /// on the same tile as each other, same as before this check existed.
    /// </summary>
    protected bool TileOccupied(int x, int y) =>
        map.GameObjectByCoord[x, y].Any(g => g.OccupiesTile);

    protected T GetRandomElement<T>(T[] elements) =>
        elements[mapGenerationRandomSource.Next(0, elements.Length)];

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
    protected T GetRandomElement<T>(IEnumerable<T> elements) =>
        elements.ElementAt(mapGenerationRandomSource.Next(0, elements.Count()));
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

    protected T GetRandomElementWeighted<T>(T[] elements, double[] weights) =>
        WeightedPick(elements, weights, mapGenerationRandomSource);

    // Shared core for GetRandomElementWeighted (instance, seedable via `random`) and
    // SelectRandomWeighted (static, for use ahead of the base constructor running - see
    // SelectRandom above) so the weighting logic isn't duplicated between them.
    static T WeightedPick<T>(T[] elements, double[] weights, Random rng)
    {
        if (elements.Length != weights.Length)
            throw new ArgumentException("elements and weigths should be of same length.");

        int i;
        double r = rng.NextDouble() * weights.Sum();
        for (i = 0; i < weights.Length; i++)
        {
            if (r < weights[i])
            {
                break;
            }
            r -= weights[i];
        }

        return elements[i];
    }

    protected bool GetRandomBool() => mapGenerationRandomSource.Next(0, 2) == 0;

    /// <summary>
    /// In-place Fisher-Yates shuffle of <paramref name="list"/> using the seedable
    /// map-generation random source.
    /// </summary>
    protected void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = mapGenerationRandomSource.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Update the map to have a wall tile at (<paramref name="x"/>,<paramref name="y"/>).
    /// </summary>
    protected void PlaceWall(int x, int y)
    {
        map.Tiles[x, y].TileSet = map.DungeonWallSet;
        map.Tiles[x, y].TileIndex = GetRandomElementWeighted(
            map.DungeonWallSet.ImageBaseIndexes,
            map.DungeonWallSet.ImageBaseWeights
        );
        map.Tiles[x, y].Blocking = true;
    }

    /// <summary>
    /// Update the map to have a floor tile at (<paramref name="x"/>,<paramref name="y"/>).
    /// </summary>
    protected void PlaceFloor(int x, int y, TileSet floorSet)
    {
        map.Tiles[x, y].TileSet = floorSet;
        map.Tiles[x, y].TileIndex = GetRandomElement(floorSet.ImageBaseIndexes);
        map.Tiles[x, y].Blocking = false;
    }

    /// <summary>
    /// Places a freestanding decorative fence pillar spanning (<paramref name="x"/>,
    /// <paramref name="y"/>) and the tile immediately east of it - the "fence_pillar_west"/
    /// "fence_pillar_east" pair (see Data/decorations.json, dontrolle/BlazorRogue-internal#86) only
    /// reads correctly as two adjacent tiles, and doesn't block movement or light at all (it's too
    /// slight visually to justify either) - purely a decorative flourish, unrelated to any actual
    /// fence line. Callers must ensure both tiles are free floor themselves.
    /// </summary>
    protected void PlaceFencePillar(int x, int y)
    {
        map.AddGameObject(
            new StaticDecorativeObject(
                x,
                y,
                configuration.StaticDecorativeObjectTypes["fence_pillar_west"]
            )
        );
        map.AddGameObject(
            new StaticDecorativeObject(
                x + 1,
                y,
                configuration.StaticDecorativeObjectTypes["fence_pillar_east"]
            )
        );
    }
}
