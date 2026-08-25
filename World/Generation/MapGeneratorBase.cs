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
    protected readonly double percentageChanceOfSpiderWebInCorner = CommonSettings(settings)
        .GetDouble("percentage_chance_of_spider_web_in_corner", 0.25);
    protected readonly double percentageChanceOfTorch = CommonSettings(settings)
        .GetDouble("percentage_chance_of_torch", 0.25);
    protected readonly double percentageChanceOfChests = CommonSettings(settings)
        .GetDouble("percentage_chance_of_chests", 0.02);

    static SettingsMap CommonSettings(SettingsMap settings) =>
        settings.GetMap("common", SettingsMap.Empty);

    protected readonly string[] doorTypes = ["metal", "stone", "wood", "ruin"];

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

        AddDoors();
        AddRandomPostMapGenerationDecorations();
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
    /// Basic simple method for placing some random monsters in a generated map.
    /// </summary>
    protected virtual void AddMonsters()
    {
        int noOfRandomMonsters = 10;

        for (int i = 0; i < noOfRandomMonsters; i++)
        {
            var pos = GetRandomUnblockedMapTile();
            var monsterType = GetRandomElement(configuration.MonsterTypes).Value;
            var monster = new Moveable(
                pos,
                AIComponentFactory.Create(
                    monsterType.AIComponentId,
                    map,
                    monsterType.AIComponentSettings
                ),
                monsterType
            );
            map.AddMonster(monster);
        }
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
                                mapGenerationRandomSource.Next(1, 4),
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
                                mapGenerationRandomSource.Next(1, 4),
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

            if (!map.IsBlocked(x, y))
                return Tuple.Create(x, y);
        }
        throw new InvalidOperationException(
            $"Couldn't find an unblocked tile on map in {maxSearch} tries!"
        );
    }

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
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes["table"]
                        )
                    );
                }
            }

            if (
                mapGenerationRandomSource.NextDouble() < percentageChanceOfAltars
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                if (NumberOfSurroundingBlockingSpots(x, y) < 4)
                {
                    map.AddGameObject(
                        new StaticDecorativeObject(
                            x,
                            y,
                            configuration.StaticDecorativeObjectTypes["altar_blood"]
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
            //map.DebugInfo.Add($"Added torch at ({x},{y}).");
        }
    }

    /// <summary>
    /// Does the map contain a door at (x,y)?
    /// </summary>
    protected bool MapTileContainsDoor(int x, int y) =>
        map.GameObjectByCoord[x, y].Any(go => go is Door);

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
}
