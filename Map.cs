using System;
using System.Linq;
using System.Collections.Generic;

public class Map
{
    public const int TileWidth = 48;
    public const int TileHeight = 48;
    public int Width { get; private set; }
    public int Height { get; private set; }

    public string DungeonWallSet { get; private set; }
    public Player Player { get; private set; }
    public const int SightRadius = 6;
    public const int PlayerSightRadiusSquared = SightRadius * SightRadius;

    public List<string> DebugInfo = new List<string>();

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

    private List<GameObject> moveables;
    public IEnumerable<GameObject> Moveables
    {
        get
        {
            return moveables;
        }
    }

    private List<Decoration>[,] moveableDecorations;

    public List<Decoration>[,] MoveableDecorations
    {
        get
        {
            return moveableDecorations;
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

    public IEnumerable<Decoration> AllDecorations(int x, int y)
    {
        return decorations[x,y].Concat(moveableDecorations[x, y]);
    }

    public Map(int width, int height, string dungeonWallSet)
    {
        DungeonWallSet = dungeonWallSet;
        Width = width;
        Height = height;
        // initalize map with dark floor tiles
        tiles = new MapPosition[width, height];
        decorations = new List<Decoration>[width, height];
        moveableDecorations = new List<Decoration>[width, height];        
        gameObjectByCoord = new List<GameObject>[width, height];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                tiles[i, j] = new MapPosition(
                    i,
                    j,
                    TileType.Black,
                    "extra",
                    11
                ) { Blocking = true };
                decorations[i, j] = new List<Decoration>();
                moveableDecorations[i, j] = new List<Decoration>();
                gameObjectByCoord[i, j] = new List<GameObject>();
            }
        }

        gameObjects = new List<GameObject>();
        moveables = new List<GameObject>();
    }

    private void ClearTwoDimListArray<T>(List<T>[,] clearArray)
    {
        for (int i = 0; i < clearArray.GetLength(0); i++)
        {
            for (int j = 0; j < clearArray.GetLength(1); j++)
            {
                clearArray[i, j].Clear();
            }
        }
    }

    public void ClearMoveables()
    {
        ClearTwoDimListArray(moveableDecorations);
    }
    private void ClearDecorations()
    {
        ClearTwoDimListArray(decorations);
    }

    private void ClearDecorations(int x, int y)
    {
        decorations[x, y].Clear();
    }

    // Renders GameObjects to Decorations, updating the latter
    public void RenderGameObjects()
    {
        ClearDecorations();
        foreach (var gameObject in GameObjects)
        {
            gameObject.Render(this);
        }
    }

    public void RenderGameObjects(int x, int y)
    {
        ClearDecorations(x, y);
        var reRenderGameObjects = GameObjectByCoord[x, y];
        foreach (var gameObject in reRenderGameObjects)
        {
            gameObject.Render(this);
        }
    }

    public void AddGameObject(GameObject gameObject)
    {
        gameObjects.Add(gameObject);
        gameObjectByCoord[gameObject.x, gameObject.y].Add(gameObject);
    }

    public void AddPlayer(Player player)
    {
        AddMoveable(player);
        this.Player = player;
        //TODO: Do I want to stuff beside this?
    }

    public void AddMoveable(GameObject gameObject)
    {
        moveables.Add(gameObject);
    }

    public void RenderMoveables()
    {
        ClearMoveables();
        foreach (var moveable in Moveables)
        {
            moveable.Render(this);
        }
    }

    public void OnKeyPress(string keyPressed)
    {
        char numKey;
        // only handle numpad events for now
        if(keyPressed.ToLower().StartsWith("numpad")){
            numKey = keyPressed[6];
        }
        else
            return;

        // Handle basic player movement
        int xDelta = 0;
        int yDelta = 0;
        if ("147".IndexOf(numKey) > -1)
            xDelta = -1;
        else if ("369".IndexOf(numKey) > -1)
        {
            xDelta = 1;
        }

        if ("789".IndexOf(numKey) > -1)
            yDelta = -1;
        else if ("123".IndexOf(numKey) > -1)
        {
            yDelta = 1;
        }

        // Check for blocking Walls or GameObject's
        int destX = this.Player.x + xDelta;
        int destY = this.Player.y + yDelta;
        if (!IsBlocked(destX, destY))
            Player.Move(xDelta, yDelta);
    }

    // TODO: 
    // * precompute and save in bool[,] array 
    // * update on any GameObject move, or state change (like Door)
    public bool IsBlocked(int x, int y){
        if(Tiles[x,y].Blocking)
            return true;
        
        if(GameObjectByCoord[x,y].Any(g=>g.Blocking))
            return true;

        return false;
    }

    // TODO: 
    // * precompute and save in bool[,] array 
    // * update on any GameObject move, or state change (like Door)
    public bool IsVisible(int x, int y){
        return ((Player.x-x)*(Player.x-x)+(Player.y-y)*(Player.y-y)) < PlayerSightRadiusSquared;
    }
}