using System;
using System.Linq;

public class DungeonGenerator {
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
    // Wall-sets have tiles 1-6 halved; can be used to round off the top (TODO) for iso effect
    // and - in junction with this - to paste over bottom part of lowermost floors for same... (TODO)
    // Wall tiles 7-12 have no front-face
    // Wall tiles 13 is special?
    // Wall tiles 14-19 have a front-face
    private readonly String[] WallSets = new [] { "crypt", "dungeon", };
    private readonly int[] WallsWithoutFront = new [] { 7,8,9,10,11,12 };
    private readonly int[] WallsWithFront = new [] { 14,15,16,17,18,19 };
    private readonly String[] BaseFloorSets = new [] { "set_blue", "set_dark", "set_grey" };
    private readonly int[] BaseFloorIndexes = new [] {1,2,3,4,5};
    private readonly String[] DoorTypes = new [] { "metal" , "stone", "wood", "ruin" };

    public DungeonGenerator(int width, int height)
    {
        // choose random wall-set for this entire dungeon
        map = new Map(width, height, GetRandomElement(WallSets));
    }

    public void Generate(){
        CreateRoom(1, 1, 6, 7);
        CreateRoom(10, 3, 6, 7);
        HorizontalTunnel(6, 4, 5); // test from left to right
        HorizontalTunnel(10, 6, -5); // test from right to left
        VerticalTunnel(13, 9, 5);
        AddPostGenerationDecorations();
        AddPlayer(3,3);
    }

    private void AddPlayer(int x, int y){
        Map.AddPlayer(new Player(x, y));
    }

    private void AddPostGenerationDecorations()
    {
        for (int x = 0; x < Map.Width; x++)
        {
            for (int y = 0; y < Map.Height; y++)
            {
                if (y > 0){
                    // Add halfwall decorations on all wall tiles (offset -1) with a floor-tile directly above 
                    // if tile above has door, select from 1-3, else from tiles 1-6
                    if( Map.Tiles[x,y].TileType == TileType.Wall && Map.Tiles[x,y-1].TileType == TileType.Floor ){
                        int topHalfWallIndex = 6;
                        if(Map.GameObjectByCoord[x,y-1].Any(go => go is Door)){
                            topHalfWallIndex = 3;
                        }
                        var halfWallIndex = random.Next(1, topHalfWallIndex + 1);
                        Map.AddGameObject(new HalfWall(x,y, halfWallIndex));
                    }
                }

                if (y < Map.Height - 1){
                    // Wall should have front, if there is a floor tile below.
                    if( Map.Tiles[x,y].TileType == TileType.Wall && Map.Tiles[x,y+1].TileType == TileType.Floor ){
                        Map.Tiles[x,y].TileIndex = GetRandomElement(WallsWithFront);
                    }
                }
            }
        }        
    }

    private T GetRandomElement<T>(T[] elements){
        return elements[random.Next(0, elements.Length)];
    }

    private bool GetRandomBool(){
        return random.Next(0,2) == 0;
    }

    private void PlaceWall(int x, int y){
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
        var tunnelFloorSet = random.Next(0,2) == 0 ? left_door_floor_tileset : right_door_floor_tileset;

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
        var tunnelFloorSet = random.Next(0,2) == 0 ? upper_door_floor_tileset : bottom_door_floor_tileset;

        // set floors in tunnel
        // create walls around tunnel
        for (int y = upper_door_y + 1; y < bottom_door_y; y++)
        {
            PlaceWall(x - 1, y);
            PlaceFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            PlaceWall(x + 1, y);
        }
    }

    private void CreateRoom(int left_x, int top_y, int width, int height)
    {
        // choose random floor-set for this room
        string floorset = GetRandomElement(BaseFloorSets);

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

        int right_x = left_x + width;
        int bottom_y = top_y + height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (x == 0 || x == width - 1)
                {
                    PlaceWall(x + left_x, y + top_y);
                }
                else 
                {
                    PlaceFloor(x + left_x, y + top_y, floorset, BaseFloorIndexes);
                }
            }
        }
    }
}

