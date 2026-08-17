using System;
using System.Collections.Generic;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.Vision;

namespace BlazorRogue.World;

class Map
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public TileSet DungeonWallSet { get; private set; }
    public Game Game { get; }

    // Set via AddPlayer() shortly after Map construction (DungeonGenerator always calls it before
    // the map is used); null! avoids forcing nullable-checks on every consumer of this property.
    public Moveable Player { get; private set; } = null!;
    public const int PlayerSightRadius = 6;
    public const int PlayerSightRadiusSquared = PlayerSightRadius * PlayerSightRadius;

    public List<string> DebugInfo = [];

    public Tile[,] Tiles { get; }

    readonly List<GameObject> gameObjects;
    public IEnumerable<GameObject> GameObjects => gameObjects;

    /* moveables contain both monsters and player */
    readonly List<Moveable> moveables;
    public IEnumerable<Moveable> Moveables => moveables;

    /* Actually, currently these are GameObjects with AI */
    readonly List<Moveable> monsters;
    public IEnumerable<Moveable> Monsters => monsters;

    readonly List<GameObject>[,] gameObjectByCoord;
    public IEnumerable<GameObject>[,] GameObjectByCoord => gameObjectByCoord;

    public List<Decoration>[,] Decorations { get; }

    public List<Decoration>[,] MoveableDecorations { get; }

    /* Fragile... These aren't protected in any sense, currently... */
    public bool[,] IsMappedMap;
    public bool[,] IsVisibleMap;
    public bool[,] BlocksLightMap;
    public bool[,] BlocksMovementMap;

    readonly Visibility visibilityAlgorithm;
    bool postGenInitialized;

    public IEnumerable<Decoration> AllDecorations(int x, int y) =>
        Decorations[x, y].Concat(MoveableDecorations[x, y]);

    public Map(int width, int height, TileSet dungeonWallSet, Game game)
    {
        DungeonWallSet = dungeonWallSet;
        Game = game;
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        Decorations = new List<Decoration>[width, height];
        MoveableDecorations = new List<Decoration>[width, height];
        gameObjectByCoord = new List<GameObject>[width, height];
        IsMappedMap = new bool[width, height];
        IsVisibleMap = new bool[width, height];
        BlocksLightMap = new bool[width, height];
        BlocksMovementMap = new bool[width, height];

        // A field, if necessary?
        var blackTileSet = new TileSet(
            "black",
            TileType.Black,
            "floor_extra",
            [11],
            null,
            character: ""
        );

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // initalize map with dark floor tiles
                Tiles[i, j] = new Tile(i, j, blackTileSet, 11) { Blocking = true };

                Decorations[i, j] = [];
                MoveableDecorations[i, j] = [];
                gameObjectByCoord[i, j] = [];
            }
        }

        gameObjects = [];
        moveables = [];
        visibilityAlgorithm = new AdamMilVisibility(BlocksLight, SetVisible, GetDistanceSquared);
        monsters = [];
    }

    /// <summary>
    /// Should be called after generation to initialize lookup-maps and the like.
    /// </summary>
    public void PostGenInitalize()
    {
        postGenInitialized = true;

        // Init lookup maps
        ForEachTile(
            (x, y) =>
            {
                UpdateBlocksLight(x, y);
                UpdateBlockMovement(x, y);
            }
        );

        RecomputeVisibility();
        RenderGameObjects();
        RenderMoveables();
    }

    public void PlayerTookTurn()
    {
        foreach (var monster in Monsters)
        {
            monster.AIComponent?.TakeTurn();
        }
    }

    public void UpdateBlocksLight(int x, int y, bool recomputeVisibility = false)
    {
        BlocksLightMap[x, y] =
            Tiles[x, y].Blocking || gameObjectByCoord[x, y].Any(g => g.BlocksLight);

        if (recomputeVisibility)
            RecomputeVisibility();
    }

    public void UpdateBlockMovement(int x, int y)
    {
        BlocksMovementMap[x, y] =
            Tiles[x, y].Blocking || gameObjectByCoord[x, y].Any(g => g.Blocking);

        BlocksMovementMap[x, y] |= moveables.Any(m => m.Blocking && m.X == x && m.Y == y);
    }

    static void ClearTwoDimListArray<T>(List<T>[,] clearArray)
    {
        for (int i = 0; i < clearArray.GetLength(0); i++)
        {
            for (int j = 0; j < clearArray.GetLength(1); j++)
            {
                clearArray[i, j].Clear();
            }
        }
    }

    public void ForEachTile(Action<int, int> apply) => ForEachTile(apply, 0, Width, 0, Height);

    public void ForEachTile(Action<int, int> apply, int xMin, int xMax, int yMin, int yMax)
    {
        xMin = Math.Max(0, xMin);
        xMax = Math.Min(Width, xMax);
        yMin = Math.Max(0, yMin);
        yMax = Math.Min(Height, yMax);

        for (int x = xMin; x < xMax; x++)
        {
            for (int y = yMin; y < yMax; y++)
            {
                apply(x, y);
            }
        }
    }

    public void ClearMoveables() => ClearTwoDimListArray(MoveableDecorations);

    void ClearDecorations() => ClearTwoDimListArray(Decorations);

    void ClearDecorations(int x, int y) => Decorations[x, y].Clear();

    // Renders GameObjects to Decorations, updating the latter
    public void RenderGameObjects()
    {
        if (!postGenInitialized)
        {
            throw new InvalidOperationException(
                "Remember to call PostGenInitialization after generation of Map."
            );
        }

        ClearDecorations();
        foreach (var gameObject in GameObjects)
        {
            gameObject.Render(this);
        }
    }

    public void RenderGameObjects(int x, int y)
    {
        if (!postGenInitialized)
        {
            throw new InvalidOperationException(
                "Remember to call PostGenInitialization after generation of Map."
            );
        }

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
        gameObjectByCoord[gameObject.X, gameObject.Y].Add(gameObject);
    }

    public void AddPlayer(Moveable player)
    {
        AddMoveable(player);
        player.GameObjectKilled += PlayerKilled;
        Player = player;
    }

    /// <summary>
    /// Unsubscribes this map's PlayerKilled handler from the player before it's placed onto a new
    /// map (e.g. via stairs) - otherwise the player's GameObjectKilled event keeps a reference to
    /// this (now discarded) map, and a later death would double-fire PlayerKilled.
    /// </summary>
    public void DetachPlayer() => Player.GameObjectKilled -= PlayerKilled;

    /// <summary>
    /// True once the player has died. The game is then finished: no further turns are accepted and
    /// the only way on is to start a new game.
    /// </summary>
    public bool IsGameOver { get; private set; }

    void PlayerKilled(object? sender, EventArgs e)
    {
        if (sender is not Moveable killedPlayer)
        {
            throw new InvalidOperationException(
                "PlayerKilled should only be invoked with Moveable's."
            );
        }

        // Several monsters attack within a single PlayerTookTurn(), and CombatComponent raises
        // GameObjectKilled on every hit that leaves the player at or below zero wounds - so this
        // can fire more than once for one death. Only the first time counts.
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        References.SoundManager.PlayGameLoose();

        // The player's corpse is deliberately left on the map rather than removed, so the final
        // position stays visible behind the game over message. Mark the spot the same way a dead
        // monster is marked (see MonsterKilled).
        PlaceBloodPuddle(killedPlayer);
    }

    void PlaceBloodPuddle(Moveable killed)
    {
        var puddleType = Game.Configuration.StaticDecorativeObjectTypes["puddle"];
        var puddleObject = new StaticDecorativeObject(
            killed.X,
            killed.Y,
            puddleType,
            nameOverride: killed.Name + "_puddle",
            infoTextOverride: $"Blood puddle of {killed.Name}",
            decorationLayer: Decoration.Layer.Behind
        );

        AddGameObject(puddleObject);
        // somewhat icky calling RenderXxx here...
        RenderGameObjects(killed.X, killed.Y);
    }

    public void AddMonster(Moveable monster)
    {
        AddMoveable(monster);
        monster.GameObjectKilled += MonsterKilled;
        monsters.Add(monster);
    }

    void MonsterKilled(object? sender, EventArgs e)
    {
        if (sender is not Moveable killedMonster)
        {
            throw new InvalidOperationException(
                "MonsterKilled should only be invoked with Moveable's."
            );
        }

        if (!monsters.Remove(killedMonster))
        {
            throw new InvalidOperationException(
                "MonsterKilled should only be invoked with a monster in the monsters list."
            );
        }

        if (!moveables.Remove(killedMonster))
        {
            throw new InvalidOperationException(
                "MonsterKilled should only be invoked with a moveable in the moveables list."
            );
        }

        PlaceBloodPuddle(killedMonster);
    }

    public void AddMoveable(Moveable moveable) => moveables.Add(moveable);

    public void RenderMoveables()
    {
        if (!postGenInitialized)
        {
            throw new InvalidOperationException(
                "Remember to call PostGenInitialization after generation of Map."
            );
        }

        ClearMoveables();
        foreach (var moveable in Moveables)
        {
            moveable.Render(this);
        }
    }

    public bool HandlePlayerAction(bool shiftKey, char numKey)
    {
        // The UI stops sending input once the game is over; this is the engine-side backstop, so a
        // dead player can never take another turn.
        if (IsGameOver)
        {
            return false;
        }

        References.EffectsSystem.Reset();

        bool stateChanged;
        bool playerMoved = false;
        bool playerAttacked = false;
        if (shiftKey)
        {
            stateChanged = HandlePlayerUse(numKey);
        }
        else
        {
            stateChanged = HandlePlayerMove(numKey, out playerAttacked);
            playerMoved = stateChanged;
        }

        if (stateChanged)
        {
            // we need to recompute visibility maps
            RecomputeVisibility();

            // wake visible monsters (visibility is reflexive)
            WakeVisibleMonsters(Player.X, Player.Y, PlayerSightRadius);
        }

        if (playerMoved && !playerAttacked && !Player.CombatComponent!.IsStarving)
        {
            Player.CombatComponent.HealByMove();
        }

        return true;
    }

    bool HandlePlayerUse(char numKey)
    {
        CalculateDeltaAndDest(numKey, out _, out _, out int destX, out int destY);

        bool stateChanged = false;

        // handle use'able GameObject's
        foreach (var go in gameObjectByCoord[destX, destY])
        {
            if (go.UseableComponent != null)
            {
                go.UseableComponent.Use();
                stateChanged = true;
            }
        }

        if (stateChanged)
        {
            UpdateBlocksLight(destX, destY);
            UpdateBlockMovement(destX, destY);
            RenderGameObjects(destX, destY);
        }

        return stateChanged;
    }

    bool HandlePlayerMove(char numKey, out bool playerAttacked)
    {
        // Handle basic player movement
        CalculateDeltaAndDest(numKey, out int xDelta, out int yDelta, out int destX, out int destY);

        bool stateChanged = false;
        playerAttacked = false;

        // Check for blocking Walls or GameObject's
        if (!IsBlocked(destX, destY))
        {
            // where we came from is definetely not blocking anymore, since we just vacated the tile
            BlocksMovementMap[Player.X, Player.Y] = false;
            // do the move
            Player.Move(xDelta, yDelta);
            // and we need to update blocked status for the destination tile (for the benefit of other moveables)
            BlocksMovementMap[destX, destY] = true;
            stateChanged = true;
        }
        else
        {
            // player skipped a turn; also prevents player from attacking oneself... ;)
            if (xDelta == 0 && yDelta == 0)
            {
                // NOTE: Changed this to true to enable healing while idling, doesn't seem to have any adverse effects ... (flw)
                return true;
            }

            // handle moveables - I take a copy as moveables may be modified, because of death
            // TODO: FIX, THIS IS CLUNKY AS HELL...
            foreach (var mo in moveables.Where(m => m.X == destX && m.Y == destY).ToList())
            {
                // what to do if it doesn't have a CombatComponent?
                if (mo.CombatComponent != null)
                {
                    bool hit = Game.FightingSystem.CloseCombatAttack(
                        Player.CombatComponent!,
                        mo.CombatComponent
                    );
                    References.SoundManager.PlayCombatSound(hit);
                    References.EffectsSystem.Shake = hit;
                    UpdateBlockMovement(destX, destY);
                    stateChanged = true;
                    playerAttacked = true;
                }
            }
        }

        return stateChanged;
    }

    void CalculateDeltaAndDest(
        char numKey,
        out int xDelta,
        out int yDelta,
        out int destX,
        out int destY
    )
    {
        xDelta = 0;
        yDelta = 0;
        if ("147".Contains(numKey, StringComparison.Ordinal))
        {
            xDelta = -1;
        }
        else if ("369".Contains(numKey, StringComparison.Ordinal))
        {
            xDelta = 1;
        }

        if ("789".Contains(numKey, StringComparison.Ordinal))
        {
            yDelta = -1;
        }
        else if ("123".Contains(numKey, StringComparison.Ordinal))
        {
            yDelta = 1;
        }

        destX = Player.X + xDelta;
        destY = Player.Y + yDelta;
    }

    void WakeVisibleMonsters(int x, int y, int playerSightRadius) =>
        ForEachTile(
            (xx, yy) =>
            {
                if (IsVisibleMap[xx, yy])
                {
                    foreach (var mo in moveables.Where(m => m.X == xx && m.Y == yy))
                    {
                        mo.AIComponent?.Wake();
                    }
                    ;
                }
            },
            x - playerSightRadius,
            x + playerSightRadius + 1,
            y - playerSightRadius,
            y + playerSightRadius + 1
        );

    void RecomputeVisibility()
    {
        ForEachTile((x, y) => IsVisibleMap[x, y] = false);

        visibilityAlgorithm.Compute(
            new LevelPoint((uint)Player.X, (uint)Player.Y),
            PlayerSightRadius
        );
    }

    public bool IsBlocked(int x, int y) =>
        postGenInitialized
            ? BlocksMovementMap[x, y]
            : Tiles[x, y].Blocking
                || gameObjectByCoord[x, y].Any(g => g.Blocking)
                || moveables.Where(m => m.Blocking).Any(m => m.X == x && m.Y == y);

    public bool BlocksLight(int x, int y) =>
        x < 0 || x >= Width || y < 0 || y >= Height || BlocksLightMap[x, y];

    public void SetVisible(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        IsVisibleMap[x, y] = true;
        IsMappedMap[x, y] = true;
    }

    public static int GetDistance(int x, int y) =>
        // we are ok with truncation here
        (int)Math.Sqrt((x * x) + (y * y));

    public static int GetDistanceSquared(int x, int y) => (x * x) + (y * y);
}
