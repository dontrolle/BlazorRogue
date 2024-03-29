﻿using System;
using System.Linq;
using System.Collections.Generic;
using BlazorRogue.GameObjects;
using BlazorRogue.Entities;
using BlazorRogue.Vision;

namespace BlazorRogue
{
  public class Map
  {
    public int Width { get; private set; }
    public int Height { get; private set; }

    public TileSet DungeonWallSet { get; private set; }
    public Game Game { get; }
    public Moveable Player { get; private set; }
    public const int PlayerSightRadius = 6;
    public const int PlayerSightRadiusSquared = PlayerSightRadius * PlayerSightRadius;

    public List<string> DebugInfo = new List<string>();

    public Tile[,] Tiles { get; }

    private readonly List<GameObject> gameObjects;
    public IEnumerable<GameObject> GameObjects => gameObjects;

    /* moveables contain both monsters and player */
    private readonly List<Moveable> moveables;
    public IEnumerable<Moveable> Moveables => moveables;

    /* Actually, currently these are GameObjects with AI */
    private readonly List<Moveable> monsters;
    public IEnumerable<Moveable> Monsters => monsters;

    private readonly List<GameObject>[,] gameObjectByCoord;
    public IEnumerable<GameObject>[,] GameObjectByCoord => gameObjectByCoord;

    public List<Decoration>[,] Decorations { get; }

    public List<Decoration>[,] MoveableDecorations { get; }

    /* Fragile... These aren't protected in any sense, currently... */
    public bool[,] IsMappedMap;
    public bool[,] IsVisibleMap;
    public bool[,] BlocksLightMap;
    public bool[,] BlocksMovementMap;

    private readonly Visibility VisibilityAlgorithm;
    private bool PostGenInitialized = false;

    public IEnumerable<Decoration> AllDecorations(int x, int y)
    {
      return Decorations[x, y].Concat(MoveableDecorations[x, y]);
    }

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

      // TODO: UF
      // A field, if necessary?
      var blackTileSet = new TileSet("black", TileType.Black, "extra", new[] { 11 }, null, character: "");

      for (int i = 0; i < width; i++)
      {
        for (int j = 0; j < height; j++)
        {
          // initalize map with dark floor tiles
          Tiles[i, j] = new Tile(
              i,
              j,
              blackTileSet,
              11
          )
          { Blocking = true };

          Decorations[i, j] = new List<Decoration>();
          MoveableDecorations[i, j] = new List<Decoration>();
          gameObjectByCoord[i, j] = new List<GameObject>();
        }
      }

      gameObjects = new List<GameObject>();
      moveables = new List<Moveable>();
      VisibilityAlgorithm = new AdamMilVisibility(BlocksLight, SetVisible, GetDistanceSquared);

      monsters = new List<Moveable>();
    }

    /// <summary>
    /// Should be called after generation to initialize lookup-maps and the like.
    /// </summary>
    public void PostGenInitalize()
    {
      PostGenInitialized = true;

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
      BlocksLightMap[x, y] = Tiles[x, y].Blocking || gameObjectByCoord[x, y].Any(g => g.BlocksLight);

      if (recomputeVisibility)
        RecomputeVisibility();
    }

    public void UpdateBlockMovement(int x, int y)
    {
      BlocksMovementMap[x, y] = Tiles[x, y].Blocking || gameObjectByCoord[x, y].Any(g => g.Blocking);

      BlocksMovementMap[x, y] |= moveables.Any(m => m.Blocking && m.x == x && m.y == y);
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

    public void ForEachTile(Action<int, int> apply)
    {
      ForEachTile(apply, 0, Width, 0, Height);
    }

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

    public void ClearMoveables()
    {
      ClearTwoDimListArray(MoveableDecorations);
    }
    private void ClearDecorations()
    {
      ClearTwoDimListArray(Decorations);
    }

    private void ClearDecorations(int x, int y)
    {
      Decorations[x, y].Clear();
    }

    // Renders GameObjects to Decorations, updating the latter
    public void RenderGameObjects()
    {
      if (!PostGenInitialized)
        throw new InvalidOperationException("Remember to call PostGenInitialization after generation of Map.");

      ClearDecorations();
      foreach (var gameObject in GameObjects)
      {
        gameObject.Render(this);
      }
    }

    public void RenderGameObjects(int x, int y)
    {
      if (!PostGenInitialized)
        throw new InvalidOperationException("Remember to call PostGenInitialization after generation of Map.");

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

    public void AddPlayer(Moveable player)
    {
      AddMoveable(player);
      this.Player = player;
    }

    public void AddMonster(Moveable monster)
    {
      AddMoveable(monster);
      monster.GameObjectKilled += MonsterKilled;
      this.monsters.Add(monster);
    }

    private void MonsterKilled(object sender, EventArgs e)
    {
      var killedMonster = sender as Moveable;
      if (killedMonster == null)
      {
        throw new InvalidOperationException("MonsterKilled should only be invoked with GameObject's of type Monster.");
      }

      System.Diagnostics.Debug.WriteLine(killedMonster.GetHashCode().ToString() + " dead");

      monsters.Remove(killedMonster);
      moveables.Remove(killedMonster);

      // Place a blood puddle
      var puddleType = Game.Configuration.StaticDecorativeObjectTypes["puddle"];
      var puddleObject = new StaticDecorativeObject(
          killedMonster.x,
          killedMonster.y,
          puddleType,
          nameOverride: killedMonster.Name + "_puddle",
          infoTextOverride: $"Blood puddle of {killedMonster.Name}");

      AddGameObject(puddleObject);
      // somewhat icky calling RenderXxx here...
      RenderGameObjects(killedMonster.x, killedMonster.y);
    }

    public void AddMoveable(Moveable moveable)
    {
      moveables.Add(moveable);
    }

    public void RenderMoveables()
    {
      if (!PostGenInitialized)
        throw new InvalidOperationException("Remember to call PostGenInitialization after generation of Map.");

      ClearMoveables();
      foreach (var moveable in Moveables)
      {
        moveable.Render(this);
      }
    }

    public bool HandlePlayerAction(bool shiftKey, char numKey)
    {
      References.EffectsSystem.Reset();

      bool stateChanged;
      if (shiftKey)
      {
        stateChanged = HandlePlayerUse(numKey);
      }
      else
      {
        stateChanged = HandlePlayerMove(numKey);
      }

      if (stateChanged)
      {
        // we need to recompute visibility maps
        RecomputeVisibility();

        // wake visible monsters (visibility is reflexive)
        WakeVisibleMonsters(Player.x, Player.y, PlayerSightRadius);
      }

      return true;
    }

    private bool HandlePlayerUse(char numKey)
    {
      CalculateDeltaAndDest(numKey, out var xDelta, out var yDelta, out var destX, out var destY);

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

    private bool HandlePlayerMove(char numKey)
    {
      // Handle basic player movement
      CalculateDeltaAndDest(numKey, out var xDelta, out var yDelta, out var destX, out var destY);

      bool stateChanged = false;

      // Check for blocking Walls or GameObject's
      if (!IsBlocked(destX, destY))
      {
        // where we came from is definetely not blocking anymore, since we just vacated the tile
        BlocksMovementMap[this.Player.x, this.Player.y] = false;
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
          return false;
        }

        // handle moveables - I take a copy as moveables may be modified, because of death 
        // TODO: FIX, THIS IS CLUNKY AS HELL...
        foreach (var mo in moveables.Where(m => m.x == destX && m.y == destY).ToList())
        {
          // what to do if it doesn't have a CombatComponent?
          if (mo.CombatComponent != null)
          {
            var hit = Game.FightingSystem.CloseCombatAttack(Player.CombatComponent!, mo.CombatComponent);
            References.SoundManager.PlayCombatSound(hit);
            References.EffectsSystem.Shake = hit;
            UpdateBlockMovement(destX, destY);
            stateChanged = true;
          }
        }
      }

      return stateChanged;
    }

    private void CalculateDeltaAndDest(char numKey, out int xDelta, out int yDelta, out int destX, out int destY)
    {
      xDelta = 0;
      yDelta = 0;
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

      destX = this.Player.x + xDelta;
      destY = this.Player.y + yDelta;
    }

    private void WakeVisibleMonsters(int x, int y, int playerSightRadius)
    {
      ForEachTile((xx, yy) =>
      {
        if (IsVisibleMap[xx, yy])
        {
          foreach (var mo in moveables.Where(m => m.x == xx && m.y == yy))
          {
            mo.AIComponent?.Wake();
          };
        }
      },
      x - playerSightRadius, x + playerSightRadius + 1, y - playerSightRadius, y + playerSightRadius + 1);
    }

    private void RecomputeVisibility()
    {
      // TODO: Can I optimize this clearing? Or, fold it into the Compute()?
      ForEachTile(
          (x, y) =>
          {
            IsVisibleMap[x, y] = false;
          }
      );

      VisibilityAlgorithm.Compute(new LevelPoint((uint)Player.x, (uint)Player.y), PlayerSightRadius);
    }

    public bool IsBlocked(int x, int y)
    {
      if (PostGenInitialized)
        return BlocksMovementMap[x, y];
      else
      {
        if (Tiles[x, y].Blocking)
          return true;

        if (gameObjectByCoord[x, y].Any(g => g.Blocking))
          return true;

        return moveables.Where(m => m.Blocking).Any(m => m.x == x && m.y == y);
      }
    }

    public bool BlocksLight(int x, int y)
    {
      if (x < 0 || x >= Width || y < 0 || y >= Height)
        return true;

      return BlocksLightMap[x, y];
    }

    public void SetVisible(int x, int y)
    {
      if (x < 0 || x >= Width || y < 0 || y >= Height)
        return;

      IsVisibleMap[x, y] = true;
      IsMappedMap[x, y] = true;
    }

    public static int GetDistance(int x, int y)
    {
      // we are ok with truncation here
      return (int)Math.Sqrt(x * x + y * y);
    }

    public static int GetDistanceSquared(int x, int y)
    {
      return x * x + y * y;
    }
  }
}
