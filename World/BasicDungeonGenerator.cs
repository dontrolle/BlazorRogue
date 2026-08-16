using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.World;

/// <summary>
/// Basic dungeon generator, following the basic principles laid out in the excellent tutorials on RogueBasin, https://www.roguebasin.com/index.php/Tutorials.
/// </summary>
/// <param name="width">Dungeon width</param>
/// <param name="height">Dungeon height</param>
/// <param name="game">Game instance</param>
/// <param name="settings">Map settings</param>
class BasicDungeonGenerator(int width, int height, Game game, SettingsMap settings)
    : MapGeneratorBase(
        width,
        height,
        game,
        SelectWallSet(game.Configuration, settings, game.Configuration.DungeonWallSets),
        settings
    )
{
    public const string Id = "basic_dungeon_generator";

    // width and height are including walls. LayoutSettings() is static because field initializers
    // can't reference another instance field/method of the same type being constructed - only
    // static members and the primary constructor's own parameters (e.g. `settings`) are allowed.
    readonly int maxRooms = LayoutSettings(settings).GetInt("max_rooms", 10);
    readonly int minRoomHeight = LayoutSettings(settings).GetInt("min_room_height", 4);
    readonly int maxRoomHeight = LayoutSettings(settings).GetInt("max_room_height", 8);
    readonly int minRoomWidth = LayoutSettings(settings).GetInt("min_room_width", 4);
    readonly int maxRoomWidth = LayoutSettings(settings).GetInt("max_room_width", 10);
    readonly int specialRoomHeight = LayoutSettings(settings).GetInt("special_room_height", 7);
    readonly int specialRoomWidth = LayoutSettings(settings).GetInt("special_room_width", 8);
    readonly double percentageChanceOfSpecialRoom = LayoutSettings(settings)
        .GetDouble("percentage_chance_of_special_room", 1.0);

    static SettingsMap LayoutSettings(SettingsMap settings) =>
        settings.GetMap("layout", SettingsMap.Empty);

    readonly List<Room> rooms = [];

    class Room(int x, int y, int width, int height)
    {
        public int X { get; } = x;
        public int Y { get; } = y;
        public int Width { get; } = width;
        public int Height { get; } = height;
        public int Left => X;
        public int Right => X + Width - 1;
        public int Upper => Y;
        public int Lower => Y + Height - 1;
        public int CenterX => X + ((Width - 1) / 2);
        public int CenterY => Y + ((Height - 1) / 2);

        public bool Intersect(Room other)
        {
            bool xInter = Left <= other.Right && Right >= other.Left;
            bool yInter = Lower >= other.Upper && Upper <= other.Lower;
            return xInter && yInter;
        }
    }

    protected override Tuple<int, int> CreateLayout()
    {
        var playerPos = CreateFloorPlans();
        AddWalls();
        return playerPos;
    }

    void AddWalls()
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

    Tuple<int, int> CreateFloorPlans()
    {
        var playerCoord = Tuple.Create(-1, -1);
        Room? lastRoom = null;
        for (int i = 0; i < maxRooms; i++)
        {
            int w = random.Next(minRoomWidth, maxRoomWidth + 1);
            int h = random.Next(minRoomHeight, maxRoomHeight + 1);
            int x = random.Next(1, map.Width - w - 1);
            int y = random.Next(1, map.Height - h - 1);
            var newRoom = new Room(x, y, w, h);
            bool intersect = false;
            foreach (var r in rooms)
            {
                if (newRoom.Intersect(r))
                {
                    intersect = true;
                    break;
                }
            }

            if (!intersect)
            {
                rooms.Add(newRoom);
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

                        candidateDoors.Add(Tuple.Create(doorX, doorY));

                        CreateVerticalTunnelFloor(lastRoom, newRoom, newRoom.CenterX);
                        doorX = newRoom.CenterX;
                        doorY = newRoom.Upper;
                        if (newRoom.CenterY < lastRoom.CenterY)
                            doorY = newRoom.Lower;

                        candidateDoors.Add(Tuple.Create(doorX, doorY));
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

                        candidateDoors.Add(Tuple.Create(doorX, doorY));

                        CreateHorizontalTunnelFloor(lastRoom, newRoom, newRoom.CenterY);
                        doorY = newRoom.CenterY;
                        doorX = newRoom.Left;
                        if (newRoom.CenterX < lastRoom.CenterX)
                            doorX = newRoom.Right;

                        candidateDoors.Add(Tuple.Create(doorX, doorY));
                    }
                }
                lastRoom = newRoom;
            }
        }

        return playerCoord;
    }

    void CreateHorizontalTunnelFloor(Room fromRoom, Room toRoom, int y)
    {
        var leftRoom = toRoom;
        var rightRoom = fromRoom;
        if (rightRoom.CenterX < leftRoom.CenterX)
        {
            leftRoom = fromRoom;
            rightRoom = toRoom;
        }

        int minX = leftRoom.CenterX;
        int maxX = rightRoom.CenterX;

        // get the floor tile set of each room
        var from_floor_tileset = map.Tiles[fromRoom.CenterX, fromRoom.CenterY].TileSet;
        var to_floor_tileset = map.Tiles[toRoom.CenterX, toRoom.CenterY].TileSet;

        var possibleTileSets = (new[] { from_floor_tileset, to_floor_tileset })
            .Intersect(configuration.StandardFloorSets)
            .ToArray();

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

    void CreateVerticalTunnelFloor(Room fromRoom, Room toRoom, int x)
    {
        var upperRoom = fromRoom;
        var lowerRoom = toRoom;
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

        var possibleTileSets = (new[] { from_floor_tileset, to_floor_tileset })
            .Intersect(configuration.StandardFloorSets)
            .ToArray();

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

    void CreateRoomFloor(Room room) => CreateRoom(room, true);

    void CreateRoom(Room room, bool elideOuterWalls = false) =>
        CreateRoom(room.X, room.Y, room.Width, room.Height, elideOuterWalls);

    void CreateRoom(int left_x, int top_y, int width, int height, bool elideOuterWalls = false)
    {
        bool placeWalls = !elideOuterWalls;

        // choose random floor-set for this room
        var floorset = GetRandomElement(configuration.StandardFloorSets);
        //bool specialRoom = false;
        if (
            width >= specialRoomWidth
            && height >= specialRoomHeight
            && random.NextDouble() < percentageChanceOfSpecialRoom
        )
        {
            //specialRoom = true;
            floorset = GetRandomElement(configuration.SpecialFloorSets);
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

        _ = left_x + width;
        _ = top_y + height;
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
