using System;
using System.Linq;
using System.Collections.Generic;
using BlazorRogue.GameObjects;
using BlazorRogue.Entities;
using BlazorRogue.AI;

namespace BlazorRogue
{
    public class DungeonGenerator
    {
        private readonly Map map;
        private readonly Configuration configuration;
        readonly Random random = new Random();

        // Decorations
        private const double PercentageChanceOfBones = 0.05;
        private readonly double PercentageChanceOfSpiderWeb = 0.25;
        private readonly double PercentageChanceOfTorch = 0.25;

        // TODO: UF
        // Walls with borders- "cave", "ruins", "stone"
        // Wall-sets have tiles 1-6 halved; can be used to round off the top for iso effect
        // and - in junction with this - to paste over bottom part of lowermost floors for same...
        // Wall tiles 7-12 have no front-face
        // Wall tiles 13 is special?
        // Wall tiles 14-19 have a front-face
        private readonly int[] WallsWithFront = new[] { 14, 15, 16, 17, 18, 19 };
        private readonly double[] WallsWithFrontWeights = new[] { 1.0, 0.1, 0.1, 0.1, 0.1, 0.1 };

        // TODO: 
        // Pickup - parse and configure decorations via config; carefully choose how fancy I want to get.
        // Probable good first steps, either 
        // 1. Basic visual decorations isolated to one tile, or,
        // 2. Handle wall decorations, which should be rendered with +/- from tile (right?)
        //    (Handle WallsWithFront and halfwalls as decoration; logically they should be able to be configured in relation to WallsWithoutFront, ..., hmmm)

        // TODO: UF
        private readonly string[] DoorTypes = new[] { "metal", "stone", "wood", "ruin" };

        // width and height are including walls
        private const int MaxRooms = 10;
        private const int MinRoomHeight = 4;
        private const int MaxRoomHeight = 8;
        private const int MinRoomWidth = 4;
        private const int MaxRoomWidth = 10;
        const int SpecialRoomHeight = 7;
        const int SpecialRoomWidth = 8;
        const double PercentageChanceOfSpecialRoom = 1.0;

        private readonly List<Room> Rooms = new List<Room>();
        private readonly List<Tuple<int, int>> CandidateDoors = new List<Tuple<int, int>>();
        class Room
        {
            public Room(int X, int Y, int width, int height)
            {
                this.X = X;
                this.Y = Y;
                Width = width;
                Height = height;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
            public int Left => X;
            public int Right => X + Width - 1;
            public int Upper => Y;
            public int Lower => Y + Height - 1;
            public int CenterX => X + (Width - 1) / 2;
            public int CenterY => Y + (Height - 1) / 2;

            public bool Intersect(Room other)
            {
                bool xInter = this.Left <= other.Right && this.Right >= other.Left;
                bool yInter = this.Lower >= other.Upper && this.Upper <= other.Lower;
                return xInter && yInter;
            }
        }

        enum Level
        {
            Dungeon,
            Cave
        }

        private readonly Level LevelType;

        public DungeonGenerator(int width, int height, Game game)
        {
            configuration = game.Configuration;

            // Choose random level-type
            LevelType = Level.Dungeon; // hardwire for now (TODO: maybe have DungeonGen and Game-level config in the long run.)

            var wallSet = LevelType switch
            {
                Level.Cave => GetRandomElement(configuration.CaveWallSets),
                Level.Dungeon => GetRandomElement(configuration.DungeonWallSets),
                _ => throw new InvalidOperationException($"Unknown level-type: {LevelType}"),
            };

            map = new Map(width, height, wallSet, game);
        }

        public Map GenerateMap()
        {
            // wait for config parse to finish
            //configParsed.Wait(); // NO ASYNC 

            Tuple<int, int> playerPos;

            switch (LevelType)
            {
                case Level.Dungeon:
                    playerPos = CreateFloorPlans();
                    AddWalls();
                    break;

                case Level.Cave:
                    playerPos = CreateCave();

                    break;
                default:
                    throw new InvalidOperationException("Unknown level-type:" + LevelType);
            }

            AddDoors();
            AddPostGenerationDecorations();


            // Add Player
            var heroType = GetRandomElement(configuration.HeroTypes).Value;
            var player = new Moveable(playerPos, null, heroType);

            map.AddPlayer(player);

            // Add monsters
            int noOfRandomMonsters = 6;

            for (int i = 0; i < noOfRandomMonsters; i++)
            {
                var pos = GetRandomUnblockedMapTile();
                var monsterType = GetRandomElement(configuration.MonsterTypes).Value;
                var monster = new Moveable(pos, new SimpleAIComponent(map), monsterType);
                map.AddMonster(monster);
            }

            var extraPos = GetRandomUnblockedMapTile();
            var extraGoblin = new Moveable(extraPos, new SimpleAIComponent(map), configuration.MonsterTypes["goblin"]);
            map.AddMonster(extraGoblin);

            // initialize various maps and so on in Map (it there a better place to do this?)
            map.PostGenInitalize();

            return map;
        }

        private void AddDoors()
        {
            foreach (var candidateDoor in CandidateDoors)
            {
                var x = candidateDoor.Item1;
                var y = candidateDoor.Item2;
                if (map.Tiles[x, y].TileType == TileType.Floor)
                {
                    // Check if horizontal makes sense
                    if (x > 1 && x < map.Width - 1 && map.Tiles[x - 1, y].TileType == TileType.Wall && map.Tiles[x + 1, y].TileType == TileType.Wall)
                    {
                        if (!MapTileContainsDoor(x, y))
                            map.AddGameObject(new Door(x, y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Horizontal, GetRandomBool()));
                    }
                    // Check if vertical makes sense
                    else if (y > 1 && y < map.Height - 1 && map.Tiles[x, y - 1].TileType == TileType.Wall && map.Tiles[x, y + 1].TileType == TileType.Wall)
                    {
                        if (!MapTileContainsDoor(x, y))
                            map.AddGameObject(new Door(x, y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Vertical, GetRandomBool()));
                    }
                }
            }
        }

        private void AddWalls()
        {
            // Depends on no rooms having been misplaced directly up map-border
            for (int x = 1; x < map.Width - 1; x++)
            {
                for (int y = 1; y < map.Height - 1; y++)
                {
                    if (map.Tiles[x, y].TileType == TileType.Floor)
                    {
                        for (int dx = -1; dx < 2; dx++)
                        {
                            for (int dy = -1; dy < 2; dy++)
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

        private Tuple<int, int> CreateFloorPlans()
        {
            var playerCoord = Tuple.Create(-1, -1);
            Room? lastRoom = null;
            for (int i = 0; i < MaxRooms; i++)
            {
                int w = random.Next(MinRoomWidth, MaxRoomWidth + 1);
                int h = random.Next(MinRoomHeight, MaxRoomHeight + 1);
                int x = random.Next(1, map.Width - w - 1);
                int y = random.Next(1, map.Height - h - 1);
                var newRoom = new Room(x, y, w, h);
                bool intersect = false;
                foreach (var r in Rooms)
                {
                    if (newRoom.Intersect(r))
                    {
                        intersect = true;
                        break;
                    }
                }

                if (!intersect)
                {
                    Rooms.Add(newRoom);
                    //map.DebugInfo.Add($"Room(l:{newRoom.Left}, r:{newRoom.Right}; u:{newRoom.Upper}, l:{newRoom.Lower}; w:{newRoom.Width}, h:{newRoom.Height})");
                    CreateRoomFloor(newRoom);

                    if (lastRoom == null)
                    {
                        // place player in first room
                        playerCoord = Tuple.Create(newRoom.X + 1, newRoom.Y + 1);
                    }
                    else
                    {
                        // connect to last room with corridor
                        if (GetRandomBool())
                        {
                            // go horizontally, then vertically
                            CreateHorizontalTunnelFloor(lastRoom, newRoom, lastRoom.CenterY);
                            // place candidate door
                            int doorY = lastRoom.CenterY;
                            // x = lastRoom.Left if newRoom is Left of lastRoom, else lastRoom.Right
                            int doorX = lastRoom.Left;
                            if (newRoom.CenterX > lastRoom.CenterX)
                                doorX = lastRoom.Right;

                            CandidateDoors.Add(Tuple.Create(doorX, doorY));

                            CreateVerticalTunnelFloor(lastRoom, newRoom, newRoom.CenterX);
                            doorX = newRoom.CenterX;
                            doorY = newRoom.Upper;
                            if (newRoom.CenterY < lastRoom.CenterY)
                                doorY = newRoom.Lower;

                            CandidateDoors.Add(Tuple.Create(doorX, doorY));
                        }
                        else
                        {
                            CreateVerticalTunnelFloor(lastRoom, newRoom, lastRoom.CenterX);
                            // place candidate door                        
                            int doorX = lastRoom.CenterX;
                            // y = lastRoom.Upper if newRoom is above lastRoom, else lastRoom.Lower
                            int doorY = lastRoom.Upper;
                            if (newRoom.CenterY > lastRoom.CenterY)
                                doorY = lastRoom.Lower;

                            CandidateDoors.Add(Tuple.Create(doorX, doorY));

                            CreateHorizontalTunnelFloor(lastRoom, newRoom, newRoom.CenterY);
                            doorY = newRoom.CenterY;
                            doorX = newRoom.Left;
                            if (newRoom.CenterX < lastRoom.CenterX)
                                doorX = newRoom.Right;

                            CandidateDoors.Add(Tuple.Create(doorX, doorY));
                        }
                    }
                    lastRoom = newRoom;
                }
            }

            return playerCoord;
        }

        private Tuple<int, int> CreateCave()
        {
            int initWallPercentageChance = 40;

            var genmap = new bool[map.Width, map.Height];

            Action<int, int> initFill = (x, y) =>
                 {
                     if (random.Next(1, 101) < initWallPercentageChance)
                         genmap[x, y] = true;
                     else
                         genmap[x, y] = false;
                 };

            map.ForEachTile(initFill);

            bool[,] newmap = null;
            Action<int, int> generation1Fill = (x, y) =>
                 {
                     if (SurroundingWallNumberWithinN(genmap, x, y, 1) >= 5 || SurroundingWallNumberWithinN(genmap, x, y, 2) <= 1)
                         newmap[x, y] = true;
                     else
                         newmap[x, y] = false;
                 };

            for (int i = 0; i < 4; i++)
            {
                newmap = new bool[map.Width, map.Height];
                map.ForEachTile(generation1Fill);
                genmap = newmap;
            }

            //return FinalizeCaveGen(genmap);        

            Action<int, int> generation2Fill = (x, y) =>
                 {
                     if (SurroundingWallNumberWithinN(genmap, x, y, 1) >= 5)
                         newmap[x, y] = true;
                     else
                         newmap[x, y] = false;
                 };

            for (int i = 0; i < 3; i++)
            {
                newmap = new bool[map.Width, map.Height];
                map.ForEachTile(generation2Fill);
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

        private Tuple<int, int> FinalizeCaveGen(bool[,] genmap)
        {
            // TODO: UF
            var caveGenTileSet = "crusted_grey";
            TileSet? floorset = configuration.SpecialFloorSets.FirstOrDefault(t => t.Id == caveGenTileSet);
            if(floorset==null)
            {
                throw new Exception("Missing tileset " + caveGenTileSet + " in read configuration.");
            }

            int[] floorIndexes = new[] { 1, 2, 3, 4 };
            FillMap(genmap, floorset);

            return GetRandomUnblockedMapTile();
        }

        private void FillMap(bool[,] genmap, TileSet floorset)
        {
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

        private int SurroundingWallNumberWithinN(bool[,] genmap, int x, int y, int distance)
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

        private Tuple<int, int> GetRandomUnblockedMapTile()
        {
            int MaxSearch = 200;
            for (int i = 0; i < MaxSearch; i++)
            {
                var x = random.Next(0, map.Width);
                var y = random.Next(0, map.Height);

                if (!map.IsBlocked(x, y))
                    return Tuple.Create(x, y);
            }
            throw new Exception($"Couldn't find an unblocked tile on map in {MaxSearch} tries!");
        }


        private void CreateHorizontalTunnelFloor(Room fromRoom, Room toRoom, int y)
        {
            Room leftRoom = toRoom;
            Room rightRoom = fromRoom;
            if (rightRoom.CenterX < leftRoom.CenterX)
            {
                leftRoom = fromRoom;
                rightRoom = toRoom;
            }

            int minX = leftRoom.CenterX;
            int maxX = rightRoom.CenterX;

            // get the floor tile set of each room
            TileSet from_floor_tileset = map.Tiles[fromRoom.CenterX, fromRoom.CenterY].TileSet;
            TileSet to_floor_tileset = map.Tiles[toRoom.CenterX, toRoom.CenterY].TileSet;

            var possibleTileSets = (new[] { from_floor_tileset, to_floor_tileset }).Intersect(configuration.StandardFloorSets).ToArray();

            // Randomly choose either floor set for the tunnel - restricted to BaseFloorSets
            var tunnelFloorSet = GetRandomElement(configuration.StandardFloorSets);
            if (possibleTileSets.Length > 0)
            {
                tunnelFloorSet = GetRandomElement(possibleTileSets);
            }

            for (int x = minX; x < maxX + 1; x++)
            {
                if (map.Tiles[x, y].TileType != TileType.Floor)
                {
                    PlaceFloor(x, y, tunnelFloorSet);
                }
            }
        }

        private void CreateVerticalTunnelFloor(Room fromRoom, Room toRoom, int x)
        {
            Room upperRoom = fromRoom;
            Room lowerRoom = toRoom;
            if (lowerRoom.CenterY < upperRoom.CenterY)
            {
                upperRoom = toRoom;
                lowerRoom = fromRoom;
            }

            int minY = upperRoom.CenterY;
            int maxY = lowerRoom.CenterY;

            // get the floor tile set of each room
            var from_floor_tileset = map.Tiles[fromRoom.CenterX, fromRoom.CenterY].TileSet;
            var to_floor_tileset = map.Tiles[toRoom.CenterX, toRoom.CenterY].TileSet;

            var possibleTileSets = (new[] { from_floor_tileset, to_floor_tileset }).Intersect(configuration.StandardFloorSets).ToArray();

            // Randomly choose either floor set for the tunnel - restricted to BaseFloorSets
            var tunnelFloorSet = GetRandomElement(configuration.StandardFloorSets);
            if (possibleTileSets.Length > 0)
            {
                tunnelFloorSet = GetRandomElement(possibleTileSets);
            }

            for (int y = minY; y < maxY + 1; y++)
            {
                if (map.Tiles[x, y].TileType != TileType.Floor)
                {
                    PlaceFloor(x, y, tunnelFloorSet);
                }
            }
        }

        // TODO: UF
        private void AddPostGenerationDecorations()
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    if (y > 0)
                    {
                        // Add halfwall decorations on all wall tiles (offset -1) with a floor-tile or a black tile directly above 
                        // if tile above has door, select from 1-3, else from tiles 1-6
                        if (map.Tiles[x, y].TileType == TileType.Wall && (map.Tiles[x, y - 1].TileType == TileType.Floor || map.Tiles[x, y - 1].TileType == TileType.Black))
                        {
                            int topHalfWallIndex = 6;

                            bool restrictToSimplerHalfWall = MapTileContainsDoor(x, y - 1) || random.Next(0, 4) < 3;
                            if (restrictToSimplerHalfWall)
                            {
                                topHalfWallIndex = 3;
                            }
                            var halfWallIndex = random.Next(1, topHalfWallIndex + 1);
                            map.AddGameObject(new HalfWall(x, y, halfWallIndex));

                            // add extra decs for specific tilesets
                            if (LevelType == Level.Cave)
                            {
                                // add cave_edge_1 and 2 to halfwall-tiles offset to the left and right respectively
                                if (x > 0 && (map.Tiles[x - 1, y].TileType == TileType.Floor || map.Tiles[x - 1, y].TileType == TileType.Black))
                                {
                                    map.AddGameObject(new CaveEdge(x, y, 1, -1, -1));
                                    //map.DebugInfo.Add($"Halfwall left cave edge at ({x},{y})");
                                }


                                if (x < map.Width - 1 && (map.Tiles[x + 1, y].TileType == TileType.Floor || map.Tiles[x + 1, y].TileType == TileType.Black))
                                {
                                    map.AddGameObject(new CaveEdge(x, y, 2, -1, 1));
                                    //map.DebugInfo.Add($"Halfwall right cave edge at ({x},{y})");
                                }
                            }
                        }
                    }

                    if (y < map.Height - 1)
                    {
                        // Wall should have front, if there is a floor tile or a black tile below; if tile below has a door, choose 14
                        if (map.Tiles[x, y].TileType == TileType.Wall && (map.Tiles[x, y + 1].TileType == TileType.Floor || map.Tiles[x, y + 1].TileType == TileType.Black))
                        {
                            var index = GetRandomElementWeighted(WallsWithFront, WallsWithFrontWeights);
                            bool mapTileBelowHasDoor = MapTileContainsDoor(x, y + 1);
                            if (mapTileBelowHasDoor)
                            {
                                index = 14;
                            }
                            map.Tiles[x, y].TileIndex = index;

                            // check for adding torch
                            if (!mapTileBelowHasDoor && map.Tiles[x, y + 1].TileType == TileType.Floor && random.NextDouble() < PercentageChanceOfTorch)
                            {
                                map.AddGameObject(new Torch(x, y));
                                //map.DebugInfo.Add($"Added torch at ({x},{y}).");
                            }

                            // add extra decs for specific tilesets
                            if (LevelType == Level.Cave)
                            {
                                // - add cave_edge_5 and 6 to wall tiles with front offset to the left and right respectively
                                if (x > 0 && (map.Tiles[x - 1, y].TileType == TileType.Floor || map.Tiles[x - 1, y].TileType == TileType.Black))
                                {
                                    map.AddGameObject(new CaveEdge(x, y, 5, 0, -1));
                                }

                                if (x < map.Width - 1 && (map.Tiles[x + 1, y].TileType == TileType.Floor || map.Tiles[x + 1, y].TileType == TileType.Black))
                                {
                                    map.AddGameObject(new CaveEdge(x, y, 6, 0, 1));
                                }
                            }
                        }
                    }

                    if (map.Tiles[x, y].TileType == TileType.Floor)
                    {
                        if (random.NextDouble() < PercentageChanceOfBones)
                        {
                            map.AddGameObject(new StaticDecorativeObject(x, y, configuration.StaticDecorativeObjectTypes["bones"]));
                        }

                        // in the following we rely on floors never being placed on the perimeter tiles, else we could do
                        //if(x > 0 && x < map.Width -1 && y > 0 && y < map.Height - 1){ ... }
                        if (random.NextDouble() < PercentageChanceOfSpiderWeb)
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
                                map.AddGameObject(new StaticDecorativeObject(x, y, configuration.StaticDecorativeObjectTypes["corner_spiderweb"], corner, verticalOffset));
                            }
                        }
                    }

                    // add extra decs for specific tilesets
                    if (LevelType == Level.Cave)
                    {
                        // add cave_edge_3 and 4 to normal wall tiles offset to the left and right respectively
                        if (map.Tiles[x, y].TileType == TileType.Wall)
                        {
                            if (x > 0 && (map.Tiles[x - 1, y].TileType == TileType.Floor || map.Tiles[x - 1, y].TileType == TileType.Black))
                            {
                                map.AddGameObject(new CaveEdge(x, y, 3, 0, -1));
                            }

                            if (x < map.Width - 1 && (map.Tiles[x + 1, y].TileType == TileType.Floor || map.Tiles[x + 1, y].TileType == TileType.Black))
                            {
                                map.AddGameObject(new CaveEdge(x, y, 4, 0, 1));
                            }
                        }
                    }
                }
            }
        }

        private bool MapTileContainsDoor(int x, int y)
        {
            return map.GameObjectByCoord[x, y].Any(go => go is Door);
        }

        private T GetRandomElement<T>(T[] elements)
        {
            return elements[random.Next(0, elements.Length)];
        }

        private T GetRandomElement<T>(IEnumerable<T> elements)
        {
            return elements.ElementAt(random.Next(0, elements.Count()));
        }

        private T GetRandomElementWeighted<T>(T[] elements, double[] weights)
        {
            if (elements.Length != weights.Length)
                throw new ArgumentException("elements and weigths should be of same length.");

            int i;
            double r = random.NextDouble() * weights.Sum();
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

        private bool GetRandomBool()
        {
            return random.Next(0, 2) == 0;
        }

        private void PlaceWall(int x, int y)
        {
            // TODO: Fix - right now important to clear all properties, else some may remain from earlier floor, e.g.
            map.Tiles[x, y].TileSet = map.DungeonWallSet;
            map.Tiles[x, y].TileIndex = GetRandomElementWeighted(map.DungeonWallSet.ImgIndexes, map.DungeonWallSet.ImgWeights);
            map.Tiles[x, y].Blocking = true;
        }

        private void PlaceFloor(int x, int y, TileSet FloorSet)
        {
            // TODO: Fix - right now important to clear all properties, else some may remain from earlier wall, e.g.
            map.Tiles[x, y].TileSet = FloorSet;
            map.Tiles[x, y].TileIndex = GetRandomElement(FloorSet.ImgIndexes);
            map.Tiles[x, y].Blocking = false;
        }

        private void CreateRoomFloor(Room room)
        {
            CreateRoom(room, true);
        }

        private void CreateRoom(Room room, bool elideOuterWalls = false)
        {
            CreateRoom(room.X, room.Y, room.Width, room.Height, elideOuterWalls);
        }

        private void CreateRoom(int left_x, int top_y, int width, int height, bool elideOuterWalls = false)
        {
            bool placeWalls = !elideOuterWalls;

            // choose random floor-set for this room
            var floorset = GetRandomElement(configuration.StandardFloorSets);
            //bool specialRoom = false;
            if (width >= SpecialRoomWidth && height >= SpecialRoomHeight && random.NextDouble() < PercentageChanceOfSpecialRoom)
            {
                //specialRoom = true;
                (floorset) = GetRandomElement(configuration.SpecialFloorSets);
            }

            if (placeWalls)
            {
                // corners
                PlaceWall(left_x, top_y);
                PlaceWall(left_x + width - 1, top_y);
                PlaceWall(left_x, top_y + height - 1);
                PlaceWall(left_x + width - 1, top_y + height - 1);

                // rest of top row and bottom row
                for (int x = 1; x < width - 1; x++)
                {
                    PlaceWall(left_x + x, top_y);
                    PlaceWall(left_x + x, top_y + height - 1);
                }
            }

            int right_x = left_x + width;
            int bottom_y = top_y + height;
            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (x == 0 || x == width - 1)
                    {
                        if (placeWalls)
                            PlaceWall(x + left_x, y + top_y);
                    }
                    else
                    {
                        PlaceFloor(x + left_x, y + top_y, floorset);
                    }
                }
            }
        }
    }
}
