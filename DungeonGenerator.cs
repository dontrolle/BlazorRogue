using System;

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

    public DungeonGenerator(int width, int height)
    {
        // choose random wall-set for this entire dungeon
        map = new Map(width, height, RandomElement(WallSets));
    }

    public void Generate(){
        CreateRoom(1, 1, 6, 7);
        CreateRoom(10, 3, 6, 7);
        HorizontalTunnel(6, 4, 5); // test from left to right
        HorizontalTunnel(10, 6, -5); // test from right to left
        //VerticalTunnel(13, 10, 5);
    }

    private T RandomElement<T>(T[] elements){
        return elements[random.Next(0, elements.Length)];
    }

    private void PlaceWall(int x, int y, int[] WallIndexes)
    {
        Map.Tiles[x, y].TileSet = map.DungeonWallSet;
        Map.Tiles[x, y].TileIndex = RandomElement(WallIndexes);
        Map.Tiles[x, y].TileType = TileType.Wall;
    }

    private void PlaceFloor(int x, int y, string FloorSet, int[] FloorIndexes)
    {
        Map.Tiles[x, y].TileSet = FloorSet;
        Map.Tiles[x, y].TileIndex = RandomElement(FloorIndexes);
        Map.Tiles[x, y].TileType = TileType.Floor;
    }

    // create a horizontal tunnel with a door in each end
    // positive length means go left
    // negative length means go right
    // checks the floor tile set left (or right) of 'breaching' door, and sets the floor tile set of the door to the same tileset
    // TODO PROBLEM - connecting room may not have been created yet
    private void HorizontalTunnel(int start_x, int y, int length)
    {
        // handle possible 'negative' length tunnel
        int door_0_x = start_x;
        int door_1_x = start_x + length + (Math.Sign(length) * -1);
        int left_door_x = Math.Min(door_0_x, door_1_x);
        int right_door_x = Math.Max(door_0_x, door_1_x);
        var left_door_tileset = Map.Tiles[left_door_x - 1, y].TileSet;
        var right_door_tileset = Map.Tiles[right_door_x + 1, y].TileSet;

        // Set floor tile on door tiles
        PlaceFloor(left_door_x, y, left_door_tileset, BaseFloorIndexes);
        PlaceFloor(right_door_x, y, right_door_tileset, BaseFloorIndexes);

        // put doors in tiles
        //Map.Tiles[left_door_x, y].Items.Add("door");
        // TODO

        // Change tile above door to WallWithFront type
        PlaceWall(left_door_x, y - 1, WallsWithFront);
        PlaceWall(right_door_x, y - 1, WallsWithFront);

        // Randomly choose either floor set for the tunnel
        var tunnelFloorSet = random.Next(0,2) == 0 ? left_door_tileset : right_door_tileset;

        // set floors in tunnel
        // create walls around tunnel        
        for (int x = left_door_x + 1; x < right_door_x; x++)
        {
            PlaceWall(x, y - 1, WallsWithFront);
            PlaceFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            PlaceWall(x, y + 1, WallsWithFront);
        }
    }

    private void CreateRoom(int left_x, int top_y, int width, int height)
    {
        // choose random floor-set for this room
        string floorset = RandomElement(BaseFloorSets);

        // corners
        PlaceWall(left_x, top_y, WallsWithoutFront);
        PlaceWall(left_x + width - 1, top_y, WallsWithoutFront);
        PlaceWall(left_x, top_y + height - 1, WallsWithFront);
        PlaceWall(left_x + width - 1, top_y + height - 1, WallsWithFront);

        // rest of top row and bottom row
        for (int x = 1; x < width - 1; x++)
        {
            PlaceWall(left_x + x, top_y, WallsWithFront);
            PlaceWall(left_x + x, top_y + height - 1, WallsWithFront);
        }

        int right_x = left_x + width;
        int bottom_y = top_y + height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (x == 0 || x == width - 1)
                {
                    PlaceWall(x + left_x, y + top_y, WallsWithoutFront);
                }
                else 
                {
                    PlaceFloor(x + left_x, y + top_y, floorset, BaseFloorIndexes);
                }
            }
        }
    }
}

