using System;

public class DungeonGenerator {
    private string DungeonWallSet;
    private MapPosition[,] map;
    public MapPosition[,] Map
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
        DungeonWallSet = RandomElement(WallSets);

        map = new MapPosition[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map[i,j] = new MapPosition (
                    i,
                    j,
                    TileType.Floor,
                    "extra",
                    11
                );
            }
        }
    }

    public void Generate(){
        CreateRoom(1, 1, 6, 7);
        CreateRoom(10, 3, 6, 7);
        HorizontalTunnel(6, 4, 5); // from left to right
        HorizontalTunnel(10, 6, -5); // from right to left
        //VerticalTunnel(13, 10, 5);
    }

    private T RandomElement<T>(T[] elements){
        return elements[random.Next(0, elements.Length)];
    }

    private void SetWall(int x, int y, int[] WallIndexes)
    {
        Map[x, y].TileSet = DungeonWallSet;
        Map[x, y].TileIndex = RandomElement(WallIndexes);
        Map[x, y].TileType = TileType.Wall;
    }

    private void SetFloor(int x, int y, string FloorSet, int[] FloorIndexes)
    {
        Map[x, y].TileSet = FloorSet;
        Map[x, y].TileIndex = RandomElement(FloorIndexes);
        Map[x, y].TileType = TileType.Floor;
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
        var left_door_tileset = Map[left_door_x - 1, y].TileSet;
        var right_door_tileset = Map[right_door_x + 1, y].TileSet;

        // Set floor tile on door tiles
        SetFloor(left_door_x, y, left_door_tileset, BaseFloorIndexes);
        SetFloor(right_door_x, y, right_door_tileset, BaseFloorIndexes);

        // Randomly choose either floor set for the tunnel
        var tunnelFloorSet = random.Next(0,2) == 0 ? left_door_tileset : right_door_tileset;

        // put doors in tiles
        // TODO

        // Change tile above door to WallWithFront type
        SetWall(left_door_x, y - 1, WallsWithFront);
        SetWall(right_door_x, y - 1, WallsWithFront);

        // set floors in tunnel
        // create walls around tunnel        
        for (int x = left_door_x + 1; x < right_door_x; x++)
        {
            SetWall(x, y - 1, WallsWithFront);
            SetFloor(x, y, tunnelFloorSet, BaseFloorIndexes);
            SetWall(x, y + 1, WallsWithFront);
        }
    }

    private void CreateRoom(int left_x, int top_y, int width, int height)
    {
        // choose random floor-set for this room
        string floorset = RandomElement(BaseFloorSets);

        // corners
        SetWall(left_x, top_y, WallsWithoutFront);
        SetWall(left_x + width - 1, top_y, WallsWithoutFront);
        SetWall(left_x, top_y + height - 1, WallsWithFront);
        SetWall(left_x + width - 1, top_y + height - 1, WallsWithFront);

        // rest of top row and bottom row
        for (int x = 1; x < width - 1; x++)
        {
            SetWall(left_x + x, top_y, WallsWithFront);
            SetWall(left_x + x, top_y + height - 1, WallsWithFront);
        }

        int right_x = left_x + width;
        int bottom_y = top_y + height;
        for (int x = 0; x < width; x++)
        {
            for (int y = 1; y < height - 1; y++)
            {
                if (x == 0 || x == width - 1)
                {
                    SetWall(x + left_x, y + top_y, WallsWithoutFront);
                }
                else 
                {
                    SetFloor(x + left_x, y + top_y, floorset, BaseFloorIndexes);
                }
            }
        }
    }
}

