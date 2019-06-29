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

    public bool[,] IsMappedMap;
    public bool[,] IsVisibleMap;

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
        IsMappedMap = new bool[width, height];
        IsVisibleMap = new bool[width, height];
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
                IsMappedMap[i,j] = false;
                IsVisibleMap[i,j] = false;
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

    public void ForEachTile(Action<int,int> apply){
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                apply(x,y);
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

        // Init IsVisible and IsMapped maps)
        ForEachTile(
            (x,y) => IsVisibleMap[x,y] = IsMappedMap[x,y] = IsVisible(x,y)
        );
    }

    public void AddMoveable(GameObject gameObject)
    {
        moveables.Add(gameObject);
        // TODO: Should I be adding to gameObjectByCoord here, also?
        // TODO: Currently, something fishy in that Moveables and MoveableByCorrd aren't linked. Player itself adds the dec.
        // TODO: I'll probably hit this, when I add the first other moveable
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
        if (!IsBlocked(destX, destY)){
            Player.Move(xDelta, yDelta);
            UpdateFoVMapsAfterPlayerMove(xDelta, yDelta);
        }
    }

    private void UpdateFoVMapsAfterPlayerMove(int xDelta, int yDelta){
        ForEachTile(
            (x,y) => {
                bool xyVisible = IsVisible(x,y);
                IsVisibleMap[x,y] = xyVisible;
                IsMappedMap[x,y] |= xyVisible;
            }
        );        
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