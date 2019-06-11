using System;
using System.Linq;
using System.Collections.Generic;

public class Map
{
    public const int TileWidth = 48;
    public const int TileHeight = 48;
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
    public IEnumerable<GameObject> GameObjects
    {
        get
        {
            return gameObjects;
        }
    }

    private List<GameObject>[,] gameObjectByCoord;
    public IEnumerable<GameObject>[,] GameObjectByCoord
    {
        get
        {
            return gameObjectByCoord;
        }
    }

    // Decorations are rendered gameobjects, effects, and other graphics
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
        gameObjectByCoord = new List<GameObject>[width, height];
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
                gameObjectByCoord[i,j] = new List<GameObject>();
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

    private IEnumerable<GameObject> ClearDecorations(int x, int y){
        // we can't do deferred evaluation here, since we're going to clear this exact list in the next line...
        var go = decorations[x,y].Select(d => d.GameObject).Distinct().ToList();
        decorations[x,y].Clear();
        return go;
    }

    // Renders GameObjects to Decorations, updating the latter
    public void RenderGameObjects(){
        ClearDecorations();
        foreach (var gameObject in GameObjects)
        {
            gameObject.Render(this);
        }
    }

    public void RenderGameObjects(int x, int y){
        // TODO: Use GameObjectsByCoord instead and simplify ClearDecorations()
        var reRenderGameObjects = ClearDecorations(x, y);
        foreach (var gameObject in reRenderGameObjects)
        {
            gameObject.Render(this);
        }
    }

    public void AddGameObject(int x, int y, GameObject gameObject){
        gameObjects.Add(gameObject);
        gameObjectByCoord[x,y].Add(gameObject);
    }
}