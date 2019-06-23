using System;
using System.Linq;
using System.Collections.Generic;

public class DungeonGenerator
{
    private Map map;
    public Map Map
    {
        get
        {
            return map;
        }
    }

    Random random = new Random();

    // Walls with borders- "cave", "ruins", "stone"
    // Wall-sets have tiles 1-6 halved; can be used to round off the top for iso effect
    // and - in junction with this - to paste over bottom part of lowermost floors for same...
    // Wall tiles 7-12 have no front-face
    // Wall tiles 13 is special?
    // Wall tiles 14-19 have a front-face
    private readonly String[] WallSets = new[] { "crypt", "dungeon", "ruins" };
    private readonly int[] WallsWithoutFront = new[] { 7, 8, 9, 10, 11, 12 };
    private readonly int[] WallsWithFront = new[] { 14, 15, 16, 17, 18, 19 };
    private readonly String[] BaseFloorSets = new[] { "set_blue", "set_dark", "set_grey" };
    private readonly int[] BaseFloorIndexes = new[] { 1, 2, 3, 4, 5 };
    private readonly Tuple<string,int[]>[] SpecialFloorSets = new [] { 
            Tuple.Create("diagonal_blue", new [] {1,2,3,4}),
            Tuple.Create("diagonal_red", new [] {1,2,3,4}),
            Tuple.Create("crusted_grey", new [] {1,2,3,4}),
            Tuple.Create("extra", new int[] {19}),
            Tuple.Create("extra", new int[] {17}),
            Tuple.Create("extra", new int[] {7}),
            Tuple.Create("extra", new int[] {4}),
            Tuple.Create("extra", new int[] {3}),
            Tuple.Create("extra", new int[] {2}),
            Tuple.Create("extra", new int[] {1}),
        };
    private readonly String[] DoorTypes = new[] { "metal", "stone", "wood", "ruin" };

    // width and height are including walls
    private const int MaxRooms = 10;
    private const int MinRoomHeight = 4;
    private const int MaxRoomHeight = 8;
    private const int MinRoomWidth = 4;
    private const int MaxRoomWidth = 10;
    const int SpecialRoomHeight = 7;
    const int SpecialRoomWidth = 8;    
    const double PercentageChanceOfSpecialRoom = 1.0;    

    private List<Room> Rooms = new List<Room>();
    private List<Tuple<int,int>> CandidateDoors = new List<Tuple<int, int>>();
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

        public bool Intersect(Room other){
            bool xInter = this.Left <= other.Right && this.Right >= other.Left;
            bool yInter = this.Lower >= other.Upper && this.Upper <= other.Lower;
            return xInter && yInter;
        }
    }

    public DungeonGenerator(int width, int height)
    {
        // choose random wall-set for this entire dungeon
        map = new Map(width, height, GetRandomElement(WallSets));
    }

    public void Generate()
    {
        var playerCoord = CreateFloorPlans();
        
        AddWalls();
        AddDoors();
        AddPostGenerationDecorations();

        // Add Player in the corner of the first room - offset +1,+1 from left-top corner
        AddPlayer(playerCoord.Item1, playerCoord.Item2);
    }

    private void AddDoors(){
        foreach (var candidateDoor in CandidateDoors)
        {
            var x = candidateDoor.Item1;
            var y = candidateDoor.Item2;
            if (map.Tiles[x,y].TileType == TileType.Floor)
            {
                // Check if horizontal makes sense
                if(x > 1 && x < Map.Width - 1 && map.Tiles[x-1,y].TileType == TileType.Wall && map.Tiles[x+1,y].TileType == TileType.Wall){
                    if(!MapTileContainsDoor(x, y))
                        map.AddGameObject(new Door(x, y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Horizontal, GetRandomBool()));
                }
                // Check if vertical makes sense
                else if(y > 1 && y < Map.Height - 1 && map.Tiles[x,y-1].TileType == TileType.Wall && map.Tiles[x,y+1].TileType == TileType.Wall){
                    if(!MapTileContainsDoor(x, y))
                        map.AddGameObject(new Door(x, y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Vertical, GetRandomBool()));
                }
            }
        }
    }

    private void AddWalls()
    {
        // Depends on no rooms having been misplaced directly up Map-border
        for (int x = 1; x < Map.Width - 1; x++)
        {
            for (int y = 1; y < Map.Height - 1; y++)
            {
                if(Map.Tiles[x,y].TileType == TileType.Floor){
                    for (int dx = -1; dx < 2; dx++)
                    {
                        for (int dy = -1; dy < 2; dy++)
                        {
                            if(Map.Tiles[x+dx, y+dy].TileType == TileType.Black){
                                PlaceWall(x+dx,y+dy);
                            }
                        }
                    }
                }
            }
        }        
    }

    private Tuple<int,int> CreateFloorPlans()
    {
        var playerCoord = Tuple.Create(-1, -1);        
        Room lastRoom = null;
        for (int i = 0; i < MaxRooms; i++)
        {
            int w = random.Next(MinRoomWidth, MaxRoomWidth + 1);
            int h = random.Next(MinRoomHeight, MaxRoomHeight + 1);
            int x = random.Next(1, Map.Width - w - 1);
            int y = random.Next(1, Map.Height - h - 1);
            var newRoom = new Room(x, y, w, h);
            bool intersect = false;
            foreach (var r in Rooms)
            {
                if(newRoom.Intersect(r)){
                    intersect = true;
                    break;
                }
            }

            if(!intersect){
                Rooms.Add(newRoom);
                Map.DebugInfo.Add($"Room(l:{newRoom.Left}, r:{newRoom.Right}; u:{newRoom.Upper}, l:{newRoom.Lower}; w:{newRoom.Width}, h:{newRoom.Height})");
                CreateRoomFloor(newRoom);

                if(lastRoom == null){
                    // place player in first room
                    playerCoord = Tuple.Create(newRoom.X + 1, newRoom.Y + 1);
                }
                else {
                    // connect to last room with corridor
                    if(GetRandomBool()){
                        // go horizontally, then vertically
                        CreateHorizontalTunnelFloor(lastRoom, newRoom, lastRoom.CenterY);
                        // place candidate door
                        int doorY = lastRoom.CenterY;
                        // x = lastRoom.Left if newRoom is Left of lastRoom, else lastRoom.Right
                        int doorX = lastRoom.Left;
                        if(newRoom.CenterX > lastRoom.CenterX)
                            doorX = lastRoom.Right;

                        CandidateDoors.Add(Tuple.Create(doorX, doorY));

                        CreateVerticalTunnelFloor(lastRoom, newRoom, newRoom.CenterX);
                        doorX = newRoom.CenterX;
                        doorY = newRoom.Upper;
                        if(newRoom.CenterY < lastRoom.CenterY)
                            doorY = newRoom.Lower;

                        CandidateDoors.Add(Tuple.Create(doorX, doorY));
                    }
                    else {
                        CreateVerticalTunnelFloor(lastRoom, newRoom, lastRoom.CenterX);
                        // place candidate door                        
                        int doorX = lastRoom.CenterX;
                        // y = lastRoom.Upper if newRoom is above lastRoom, else lastRoom.Lower
                        int doorY = lastRoom.Upper;
                        if(newRoom.CenterY > lastRoom.CenterY)
                            doorY = lastRoom.Lower;

                        CandidateDoors.Add(Tuple.Create(doorX, doorY));

                        CreateHorizontalTunnelFloor(lastRoom, newRoom, newRoom.CenterY);
                        doorY = newRoom.CenterY;
                        doorX = newRoom.Left;
                        if(newRoom.CenterX < lastRoom.CenterX)
                            doorX = newRoom.Right;

                        CandidateDoors.Add(Tuple.Create(doorX, doorY));
                    }
                }
                lastRoom = newRoom;                
            }
        }

        return playerCoord;
    }

    // TODO: Factor CreateXXXTunnelFloor into one method
    private void CreateHorizontalTunnelFloor(Room fromRoom, Room toRoom, int y){
        Room leftRoom = toRoom;
        Room rightRoom = fromRoom;
        if(rightRoom.CenterX < leftRoom.CenterX) {
            leftRoom = fromRoom;
            rightRoom = toRoom;
        }

        int minX = leftRoom.CenterX;
        int maxX = rightRoom.CenterX;

        // get the floor tile set of each room
        var from_floor_tileset = Map.Tiles[fromRoom.CenterX, fromRoom.CenterY].TileSet;
        var to_floor_tileset = Map.Tiles[toRoom.CenterX, toRoom.CenterY].TileSet;

        var possibleTileSets = (new string[] {from_floor_tileset, to_floor_tileset}).Intersect(BaseFloorSets);

        // Randomly choose either floor set for the tunnel - restricted to BaseFloorSets
        var tunnelFloorSet = GetRandomElement(possibleTileSets.ToArray());

        for (int x = minX; x < maxX + 1; x++)
        {
            if(Map.Tiles[x,y].TileType != TileType.Floor){
                PlaceFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            }
        }
    }

    private void CreateVerticalTunnelFloor(Room fromRoom, Room toRoom, int x){
        Room upperRoom = fromRoom;
        Room lowerRoom = toRoom;
        if(lowerRoom.CenterY < upperRoom.CenterY) {
            upperRoom = toRoom;
            lowerRoom = fromRoom;
        }

        int minY = upperRoom.CenterY;
        int maxY = lowerRoom.CenterY;

        // get the floor tile set of each room
        var from_floor_tileset = Map.Tiles[fromRoom.CenterX, fromRoom.CenterY].TileSet;
        var to_floor_tileset = Map.Tiles[toRoom.CenterX, toRoom.CenterY].TileSet;

        var possibleTileSets = (new string[] {from_floor_tileset, to_floor_tileset}).Intersect(BaseFloorSets);

        // Randomly choose either floor set for the tunnel - restricted to BaseFloorSets
        var tunnelFloorSet = GetRandomElement(possibleTileSets.ToArray());

        for (int y = minY; y < maxY + 1; y++)
        {
            if(Map.Tiles[x,y].TileType != TileType.Floor){
                PlaceFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            }
        }
    }

    private void AddPlayer(int x, int y)
    {
        Map.AddPlayer(new Player(x, y));
    }

    private void AddPostGenerationDecorations()
    {
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                if (y > 0)
                {
                    // Add halfwall decorations on all wall tiles (offset -1) with a floor-tile or a black tile directly above 
                    // if tile above has door, select from 1-3, else from tiles 1-6
                    if (Map.Tiles[x, y].TileType == TileType.Wall && (Map.Tiles[x, y - 1].TileType == TileType.Floor || Map.Tiles[x, y - 1].TileType == TileType.Black))
                    {
                        int topHalfWallIndex = 6;
                        if (Map.GameObjectByCoord[x, y - 1].Any(go => go is Door))
                        {
                            topHalfWallIndex = 3;
                        }
                        var halfWallIndex = random.Next(1, topHalfWallIndex + 1);
                        Map.AddGameObject(new HalfWall(x, y, halfWallIndex));
                    }
                }

                if (y < Map.Height - 1)
                {
                    // Wall should have front, if there is a floor tile or a black tile below; if tile below has a door, choose 14
                    if (Map.Tiles[x, y].TileType == TileType.Wall && (Map.Tiles[x, y + 1].TileType == TileType.Floor || Map.Tiles[x, y + 1].TileType == TileType.Black))
                    {
                        var index = GetRandomElement(WallsWithFront);
                        if (MapTileContainsDoor(x, y + 1))
                        {
                            index = 14;
                        }
                        Map.Tiles[x, y].TileIndex = index;
                    }
                }
            }
        }
    }

    private bool MapTileContainsDoor(int x, int y){
        return Map.GameObjectByCoord[x, y].Any(go => go is Door);
    }
    private T GetRandomElement<T>(T[] elements)
    {
        return elements[random.Next(0, elements.Length)];
    }

    private bool GetRandomBool()
    {
        return random.Next(0, 2) == 0;
    }

    private void PlaceWall(int x, int y)
    {
        PlaceWall(x, y, WallsWithoutFront);
    }

    private void PlaceWall(int x, int y, int[] WallIndexes)
    {
        // TODO: Fix - right now important to clear all properties, else some may remain from earlier floor, e.g.
        Map.Tiles[x, y].TileSet = map.DungeonWallSet;
        Map.Tiles[x, y].TileIndex = GetRandomElement(WallIndexes);
        Map.Tiles[x, y].TileType = TileType.Wall;
        Map.Tiles[x, y].Blocking = true;
    }

    private void PlaceFloor(int x, int y, string FloorSet, int[] FloorIndexes)
    {
        // TODO: Fix - right now important to clear all properties, else some may remain from earlier wall, e.g.
        Map.Tiles[x, y].TileSet = FloorSet;
        Map.Tiles[x, y].TileIndex = GetRandomElement(FloorIndexes);
        Map.Tiles[x, y].TileType = TileType.Floor;
        Map.Tiles[x, y].Blocking = false;
    }

    // create a horizontal tunnel with a door in each end
    // positive length means go left
    // negative length means go right
    // checks the floor tile set left (or right) of 'breaching' door, and 
    // sets the floor tile set of the door to the same tileset
    private void HorizontalTunnel(int start_x, int y, int length)
    {
        // handle possible 'negative' length tunnel
        int door_0_x = start_x;
        int door_1_x = start_x + length + (Math.Sign(length) * -1);
        int left_door_x = Math.Min(door_0_x, door_1_x);
        int right_door_x = Math.Max(door_0_x, door_1_x);

        // checks the floor tile set left (or right) of 'breaching' door, and 
        // sets the floor tile set of the door to the same tileset
        // TODO: Handle possible visual problem - connecting room may not have been created yet
        var left_door_floor_tileset = Map.Tiles[left_door_x - 1, y].TileSet;
        var right_door_floor_tileset = Map.Tiles[right_door_x + 1, y].TileSet;

        // Set floor tileset on door tiles
        PlaceFloor(left_door_x, y, left_door_floor_tileset, BaseFloorIndexes);
        PlaceFloor(right_door_x, y, right_door_floor_tileset, BaseFloorIndexes);

        // put doors in tiles
        map.AddGameObject(new Door(left_door_x, y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Vertical, GetRandomBool()));
        map.AddGameObject(new Door(right_door_x, y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Vertical, GetRandomBool()));

        // Change tile above door to WallWithFront type
        // TODO: Nice to have: Restrict tile-types to simpler ones without decoration (will be obscured by door)
        // TODO: Superflouos, if wall-front'age is determined as a decorative post-gen. pass
        PlaceWall(left_door_x, y - 1);
        PlaceWall(right_door_x, y - 1);

        // Randomly choose either floor set for the tunnel
        var tunnelFloorSet = random.Next(0, 2) == 0 ? left_door_floor_tileset : right_door_floor_tileset;

        // set floors in tunnel
        // create walls around tunnel
        for (int x = left_door_x + 1; x < right_door_x; x++)
        {
            PlaceWall(x, y - 1);
            PlaceFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            PlaceWall(x, y + 1);
        }
    }

    // create a vertical tunnel with a door in each end
    // positive length means go down
    // negative length means go up
    // checks the floor tile set above (or below) of 'breaching' door, and 
    // sets the floor tile set of the door to the same tileset
    private void VerticalTunnel(int x, int start_y, int length)
    {
        // handle possible 'negative' length tunnel
        int door_0_y = start_y;
        int door_1_y = start_y + length + (Math.Sign(length) * -1);
        int upper_door_y = Math.Min(door_0_y, door_1_y);
        int bottom_door_y = Math.Max(door_0_y, door_1_y);

        // checks the floor tile set above (or below) of 'breaching' door, and 
        // sets the floor tileset of the door to the same tileset
        // TODO: Handle possible visual problem - connecting room may not have been created yet
        var upper_door_floor_tileset = Map.Tiles[x, upper_door_y - 1].TileSet;
        var bottom_door_floor_tileset = Map.Tiles[x, bottom_door_y + 1].TileSet;

        // Set floor tileset on door tiles
        PlaceFloor(x, upper_door_y, upper_door_floor_tileset, BaseFloorIndexes);
        PlaceFloor(x, bottom_door_y, bottom_door_floor_tileset, BaseFloorIndexes);

        // put doors in tiles
        map.AddGameObject(new Door(x, upper_door_y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Horizontal, GetRandomBool()));
        map.AddGameObject(new Door(x, bottom_door_y, GetRandomElement(DoorTypes), random.Next(1, 4), Orientation.Horizontal, GetRandomBool()));

        // Change tile left and right of doors to WallWithoutFront type
        // TODO: Nice to have: Restrict tile-types to simpler ones without decoration (will be obscured by door)
        // TODO: Superflouos, if wall-front'age is determined as a decorative post-gen. pass
        PlaceWall(x - 1, upper_door_y);
        PlaceWall(x + 1, upper_door_y);
        PlaceWall(x - 1, bottom_door_y);
        PlaceWall(x + 1, bottom_door_y);

        // Randomly choose either floor set for the tunnel
        var tunnelFloorSet = random.Next(0, 2) == 0 ? upper_door_floor_tileset : bottom_door_floor_tileset;

        // set floors in tunnel
        // create walls around tunnel
        for (int y = upper_door_y + 1; y < bottom_door_y; y++)
        {
            PlaceWall(x - 1, y);
            PlaceFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            PlaceWall(x + 1, y);
        }
    }

    private void CreateRoomFloor(Room room){
        CreateRoom(room, true);
    }

    private void CreateRoom(Room room, bool elideOuterWalls = false){
        CreateRoom(room.X, room.Y, room.Width, room.Height, elideOuterWalls);
    }

    private void CreateRoom(int left_x, int top_y, int width, int height, bool elideOuterWalls = false)
    {
        bool placeWalls = !elideOuterWalls;

        // choose random floor-set for this room
        string floorset = GetRandomElement(BaseFloorSets);
        int[] floorIndexes = BaseFloorIndexes;
        bool specialRoom = false;
        if(width >= SpecialRoomWidth && height >= SpecialRoomHeight && random.NextDouble() < PercentageChanceOfSpecialRoom){
            specialRoom = true;
            (floorset, floorIndexes) = GetRandomElement(SpecialFloorSets);
        }

        if(placeWalls)
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
                    if(placeWalls)
                        PlaceWall(x + left_x, y + top_y);
                }
                else
                {
                    PlaceFloor(x + left_x, y + top_y, floorset, floorIndexes);
                }
            }
        }
    }
}

