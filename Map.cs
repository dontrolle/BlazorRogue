using System;
using System.Linq;
using System.Collections.Generic;

public class Map
{
    public int Width { get; private set; }
    public int Height {get; private set; }

    public string DungeonWallSet { get; private set;}

    private MapPosition[,] tiles;
    public MapPosition[,] Tiles
    {
        get
        {
            return tiles;
        }
    }

    private List<GameObject> gameObjects;

    public List<GameObject> GameObjects
    {
        get
        {
            return gameObjects;
        }
    }

    // Decorations are rendered effects, and other graphics
    private List<Decoration> decorations;

    public List<Decoration> Decorations 
    {
        get
        {
            return decorations;
        }
    }

    public Map(int width, int height, string dungeonWallSet)
    {
        DungeonWallSet = dungeonWallSet;
        Width = width;
        Height = height;
        // initalize map with dark floor tiles
        tiles = new MapPosition[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tiles[i,j] = new MapPosition (
                    i,
                    j,
                    TileType.Floor,
                    "extra",
                    11
                );
            }
        }

        gameObjects = new List<GameObject>();
    }
}