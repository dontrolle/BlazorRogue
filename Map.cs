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
    private List<Decoration>[,] decorations;

    public List<Decoration>[,] Decorations 
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
        decorations = new List<Decoration>[width, height];
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
                decorations[i,j] = new List<Decoration>();
            }
        }

        gameObjects = new List<GameObject>();
    }

    private void ClearDecorations(){
        for (int i = 0; i < decorations.GetLength(0); i++)
        {
            for (int j = 0; j < decorations.GetLength(1); j++)
            {
                decorations[i,j].Clear();
            }
        }
    }

    // Renders GameObjects to Decorations, updating the latter
    // TODO: Optimizations abound - clearing all decorations is overdoing it; 
    //       we know the identity and locality of the decoration clicked; and this has link to gameobject
    public void RenderGameObjects(){
        ClearDecorations();
        foreach (var gameObject in GameObjects)
        {
            gameObject.Render(this);
        }
    }
}