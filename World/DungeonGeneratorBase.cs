using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.AI;
using BlazorRogue.Components;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

abstract class DungeonGeneratorBase(
    int width,
    int height,
    Game game,
    TileSet wallSet,
    SettingsMap settings
) : IMapGenerator
{
    protected readonly Map map = new(width, height, wallSet, game);
    protected readonly Configuration configuration = game.Configuration;
    protected readonly Random random = new();

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
    /// <paramref name="defaultPool"/> (the generator's whole level-type pool), preserving the
    /// previous behaviour.
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

    public Map GenerateMap()
    {
        var playerPos = CreateLayout();

        AddDoors();
        AddPostGenerationDecorations();

        // Add Player
        var heroType = GetRandomElement(configuration.HeroTypes).Value;
        var player = new Moveable(playerPos, null, heroType, new InventoryComponent());
        map.AddPlayer(player);

        AddMonsters();

        // initialize various maps and so on in Map (it there a better place to do this?)
        map.PostGenInitalize();

        return map;
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

    protected abstract Tuple<int, int> CreateLayout();

    protected void AddDoors()
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
                                random.Next(1, 4),
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
                                random.Next(1, 4),
                                Orientation.Vertical,
                                GetRandomBool()
                            )
                        );
                    }
                }
            }
        }
    }

    protected Tuple<int, int> GetRandomUnblockedMapTile()
    {
        int maxSearch = 200;
        for (int i = 0; i < maxSearch; i++)
        {
            int x = random.Next(0, map.Width);
            int y = random.Next(0, map.Height);

            if (!map.IsBlocked(x, y))
                return Tuple.Create(x, y);
        }
        throw new InvalidOperationException(
            $"Couldn't find an unblocked tile on map in {maxSearch} tries!"
        );
    }

    protected void AddPostGenerationDecorations()
    {
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                AddPostGenWallDecorations(x, y);

                AddPostGenFloorDecorations(x, y);
            }
        }
    }

    protected void AddPostGenFloorDecorations(int x, int y)
    {
        if (map.Tiles[x, y].TileType == TileType.Floor)
        {
            if (
                random.NextDouble() < percentageChanceOfBones
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
                random.NextDouble() < percentageChanceOfTables
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
                random.NextDouble() < percentageChanceOfAltars
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
                random.NextDouble() < percentageChanceOfChests
                && !MapTileContainsDoor(x, y)
                && !map.IsBlocked(x, y)
            )
            {
                string chestId = "chest_silver";
                int gold = random.Next(0, 4);
                if (random.Next(0, 4) == 0)
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
            if (random.NextDouble() < percentageChanceOfSpiderWebInCorner)
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

    protected void AddPostGenWallDecorations(int x, int y)
    {
        // Add halfwall decorations on all wall tiles (offset -1) with a floor-tile or a black tile directly above
        // if tile above has door, select from 1-3, else from tiles 1-6
        if (y > 0)
        {
            if (
                map.Tiles[x, y].TileType == TileType.Wall
                && (
                    map.Tiles[x, y - 1].TileType == TileType.Floor
                    || map.Tiles[x, y - 1].TileType == TileType.Black
                )
            )
            {
                int[] halfwallIndexes = map.DungeonWallSet.ImageEdgeNorthIndexes;

                bool restrictToSimplerHalfWall =
                    MapTileContainsDoor(x, y - 1) || random.Next(0, 4) < 3;
                if (restrictToSimplerHalfWall)
                {
                    halfwallIndexes = map.DungeonWallSet.ImageSimpleEdgeNorthIndexes;
                }

                map.AddGameObject(new HalfWall(x, y, GetRandomElement(halfwallIndexes)));

                OnNorthHalfWallPlaced(x, y);
            }
        }

        // Wall should have front, if there is a floor tile or a black tile below; if tile below has a door, choose 14
        if (y < map.Height - 1)
        {
            if (
                map.Tiles[x, y].TileType == TileType.Wall
                && (
                    map.Tiles[x, y + 1].TileType == TileType.Floor
                    || map.Tiles[x, y + 1].TileType == TileType.Black
                )
            )
            {
                int index = GetRandomElementWeighted(
                    map.DungeonWallSet.ImageSouthEdgeIndexes,
                    map.DungeonWallSet.ImageSouthEdgeWeights
                );
                bool mapTileBelowHasDoor = MapTileContainsDoor(x, y + 1);
                if (mapTileBelowHasDoor)
                {
                    // TODO: UF
                    index = 14;
                }
                map.Tiles[x, y].TileIndex = index;

                // check for adding torch
                if (
                    !mapTileBelowHasDoor
                    && map.Tiles[x, y + 1].TileType == TileType.Floor
                    && random.NextDouble() < percentageChanceOfTorch
                )
                {
                    map.AddGameObject(new Torch(x, y));
                    //map.DebugInfo.Add($"Added torch at ({x},{y}).");
                }

                OnSouthWallFrontPlaced(x, y);
            }
        }

        if (map.Tiles[x, y].TileType == TileType.Wall)
        {
            OnWallTileVisited(x, y);
        }
    }

    // Hooks for subclass-specific extra wall decorations (e.g. cave edges), called from
    // AddPostGenWallDecorations above. No-op by default.
    protected virtual void OnNorthHalfWallPlaced(int x, int y) { }

    protected virtual void OnSouthWallFrontPlaced(int x, int y) { }

    protected virtual void OnWallTileVisited(int x, int y) { }

    protected bool MapTileContainsDoor(int x, int y) =>
        map.GameObjectByCoord[x, y].Any(go => go is Door);

    protected T GetRandomElement<T>(T[] elements) => elements[random.Next(0, elements.Length)];

#pragma warning disable CA1851 // Possible multiple enumerations of 'IEnumerable' collection
    protected T GetRandomElement<T>(IEnumerable<T> elements) =>
        elements.ElementAt(random.Next(0, elements.Count()));
#pragma warning restore CA1851 // Possible multiple enumerations of 'IEnumerable' collection

    protected T GetRandomElementWeighted<T>(T[] elements, double[] weights) =>
        WeightedPick(elements, weights, random);

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

    protected bool GetRandomBool() => random.Next(0, 2) == 0;

    protected void PlaceWall(int x, int y)
    {
        // TODO: Fix - right now important to clear all properties, else some may remain from earlier floor, e.g.
        map.Tiles[x, y].TileSet = map.DungeonWallSet;
        map.Tiles[x, y].TileIndex = GetRandomElementWeighted(
            map.DungeonWallSet.ImageBaseIndexes,
            map.DungeonWallSet.ImageBaseWeights
        );
        map.Tiles[x, y].Blocking = true;
    }

    protected void PlaceFloor(int x, int y, TileSet floorSet)
    {
        // TODO: Fix - right now important to clear all properties, else some may remain from earlier wall, e.g.
        map.Tiles[x, y].TileSet = floorSet;
        map.Tiles[x, y].TileIndex = GetRandomElement(floorSet.ImageBaseIndexes);
        map.Tiles[x, y].Blocking = false;
    }
}
