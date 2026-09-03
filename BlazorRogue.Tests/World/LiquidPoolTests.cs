using System.Collections.Generic;
using System.Linq;
using BlazorRogue.AI;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;
using BlazorRogue.World;
using BlazorRogue.World.Generation;

namespace BlazorRogue.Tests.World;

/// <summary>
/// Covers liquid pool tiles: <see cref="Map.SetLiquidTile"/> and the hazard behaviour hanging off
/// it (instakill on entry, per-turn acid damage, slow-on-exit), the AI treating lava as
/// impassable, and generator placement via <c>MapGeneratorBase.AddLiquidPools</c>.
/// </summary>
public class LiquidPoolTests
{
    static LiquidType Liquid(LiquidEffectKind kind, int magnitude) =>
        new(
            id: "test_liquid",
            name: "sludge",
            spriteName: "water_blue",
            frameCount: 4,
            animationDurationSeconds: 1.0,
            lipIndex: 1,
            asciiColor: "#000000",
            effectKind: kind,
            effectMagnitude: magnitude
        );

    static Moveable NewCreature(int x, int y, AIComponent? ai = null)
    {
        var type = new MoveableType(
            id: "dummy",
            name: "Dummy",
            animationClass: "animated_dummy",
            asciiCharacter: "d",
            asciiColour: "white",
            weaponSkill: 30,
            weaponDamage: 5,
            toughness: 0,
            armour: 0,
            wounds: 20,
            aiComponentId: AIComponentFactory.DefaultId,
            aiComponentSettings: SettingsMap.Empty,
            singular: true
        );
        return new Moveable(x, y, ai, type);
    }

    // A small all-floor map wired to a real Game (so Map.Game.AddMessage works) and pointed at by
    // References.Map (so Moveable.Move's enter-hook targets it).
    static Map BareFloorMap(int size = 10)
    {
        var game = new Game();
        var wallSet = new TileSet("w", TileType.Wall, "w", [0]);
        var floorSet = new TileSet("f", TileType.Floor, "f", [0]);
        var map = new Map(size, size, wallSet, game);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                map.Tiles[x, y].TileSet = floorSet;
                map.Tiles[x, y].Blocking = false;
            }
        }
        References.Map = map;
        return map;
    }

    [Fact]
    public void SetLiquidTileMakesAWalkableLiquidTile()
    {
        var map = BareFloorMap();

        map.SetLiquidTile(3, 3, Liquid(LiquidEffectKind.Slow, 25));

        var tile = map.Tiles[3, 3];
        Assert.Equal(TileType.Liquid, tile.TileType);
        Assert.False(tile.Blocking);
        Assert.Equal("~", tile.Character);
        Assert.False(map.IsBlocked(3, 3));
    }

    [Fact]
    public void IsLethalLiquidIsTrueOnlyForInstakillTiles()
    {
        var map = BareFloorMap();
        map.SetLiquidTile(1, 1, Liquid(LiquidEffectKind.Instakill, 0));
        map.SetLiquidTile(2, 2, Liquid(LiquidEffectKind.Acid, 5));

        Assert.True(map.IsLethalLiquid(1, 1));
        Assert.False(map.IsLethalLiquid(2, 2));
        Assert.False(map.IsLethalLiquid(5, 5)); // plain floor
        Assert.False(map.IsLethalLiquid(-1, 0)); // off map
    }

    [Fact]
    public void EnteringAnInstakillTileKillsTheMoveable()
    {
        var game = new Game();
        var player = game.Map.Player;
        game.Map.SetLiquidTile(player.X, player.Y, Liquid(LiquidEffectKind.Instakill, 0));

        game.Map.OnMoveableEnteredTile(player);

        Assert.True(game.Map.IsGameOver);
    }

    [Fact]
    public void MoveOntoLavaTriggersTheEnterHookAndEndsTheGame()
    {
        var game = new Game();
        var player = game.Map.Player;

        // Find an orthogonal floor neighbour, turn it into lava, then walk into it.
        (int dx, int dy) = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) }.First(d =>
            game.Map.Tiles[player.X + d.Item1, player.Y + d.Item2].TileType == TileType.Floor
        );
        game.Map.SetLiquidTile(player.X + dx, player.Y + dy, Liquid(LiquidEffectKind.Instakill, 0));

        player.Move(dx, dy);

        Assert.True(game.Map.IsGameOver);
    }

    [Fact]
    public void StandingInAcidAtEndOfTurnCostsWounds()
    {
        var game = new Game();
        var player = game.Map.Player;
        int before = player.CombatComponent!.Wounds;

        // Magnitude well above any hero's damage soak so the tick always bites.
        game.Map.SetLiquidTile(player.X, player.Y, Liquid(LiquidEffectKind.Acid, 50));
        game.Map.PlayerTookTurn();

        Assert.True(player.CombatComponent.Wounds < before);
    }

    [Fact]
    public void AcidDamageAddsAMessageNamingTheAmountAndLiquid()
    {
        var game = new Game();
        var player = game.Map.Player;

        game.Map.SetLiquidTile(player.X, player.Y, Liquid(LiquidEffectKind.Acid, 50));
        int dealt = player.CombatComponent!.Wounds;
        game.Map.PlayerTookTurn();
        dealt -= player.CombatComponent.Wounds;

        Assert.Contains(game.Messages, m => m == $"You take {dealt} damage from the sludge!");
    }

    [Fact]
    public void LiquidTileTooltipIsCapitalisedAndOnlyOnUnrotatedDecorations()
    {
        var map = BareFloorMap();
        // A two-tile pool, so the northern tile gets rotated convex-corner edging sprites.
        map.SetLiquidTile(4, 4, Liquid(LiquidEffectKind.Slow, 25));
        map.SetLiquidTile(4, 5, Liquid(LiquidEffectKind.Slow, 25));

        map.Tiles[4, 4].Render(map);

        var decs = map.Decorations[4, 4];
        Assert.Contains(decs, d => d.GameObject.InfoText == "Sludge");
        Assert.Contains(decs, d => d.RotationDegrees != 0); // there really is a rotated sprite here
        // A tooltip that rode a rotated sprite would render sideways.
        Assert.All(
            decs.Where(d => d.GameObject.InfoText.Length > 0),
            d => Assert.Equal(0, d.RotationDegrees)
        );
    }

    [Fact]
    public void NoBloodPuddleSpawnsWhenAMoveableDiesInALiquidPool()
    {
        var game = new Game();
        var monster = game.Map.Monsters.First();
        int mx = monster.X;
        int my = monster.Y;
        game.Map.SetLiquidTile(mx, my, Liquid(LiquidEffectKind.Acid, 5));

        monster.CombatComponent!.ApplyDamage(999);

        Assert.DoesNotContain(
            game.Map.GameObjects,
            go => go.X == mx && go.Y == my && go.Name.Contains("puddle")
        );
    }

    [Fact]
    public void BloodPuddleStillSpawnsWhenAMoveableDiesOnDryGround()
    {
        var game = new Game();
        var monster = game.Map.Monsters.First();
        int mx = monster.X;
        int my = monster.Y;

        monster.CombatComponent!.ApplyDamage(999);

        Assert.Contains(
            game.Map.GameObjects,
            go => go.X == mx && go.Y == my && go.Name.Contains("puddle")
        );
    }

    [Fact]
    public void PlainWaterDealsNoAcidDamage()
    {
        var game = new Game();
        var player = game.Map.Player;
        int before = player.CombatComponent!.Wounds;

        game.Map.SetLiquidTile(player.X, player.Y, Liquid(LiquidEffectKind.Slow, 25));
        game.Map.PlayerTookTurn();

        Assert.Equal(before, player.CombatComponent.Wounds);
    }

    [Fact]
    public void LiquidStumbleAlwaysFailsTheMoveAtMagnitude100()
    {
        var map = BareFloorMap();
        var player = NewCreature(4, 4);
        map.AddPlayer(player);
        map.SetLiquidTile(4, 4, Liquid(LiquidEffectKind.Slow, 100));

        Assert.True(map.LiquidStumble(player));
    }

    [Fact]
    public void LiquidStumbleNeverFiresAtMagnitudeZeroOrOffASlowTile()
    {
        var map = BareFloorMap();
        var player = NewCreature(4, 4);
        map.AddPlayer(player);

        Assert.False(map.LiquidStumble(player)); // plain floor

        map.SetLiquidTile(4, 4, Liquid(LiquidEffectKind.Slow, 0));
        Assert.False(map.LiquidStumble(player)); // 0% chance

        map.SetLiquidTile(4, 4, Liquid(LiquidEffectKind.Acid, 5));
        Assert.False(map.LiquidStumble(player)); // acid isn't a slow effect
    }

    [Fact]
    public void SimpleAiWillNotStepIntoLava()
    {
        var map = BareFloorMap();
        map.AddPlayer(NewCreature(4, 2));

        var ai = (SimpleAIComponent)
            AIComponentFactory.Create(SimpleAIComponent.ComponentId, map, SettingsMap.Empty);
        var monster = NewCreature(2, 2, ai);
        ai.Wake();

        map.SetLiquidTile(3, 2, Liquid(LiquidEffectKind.Instakill, 0));

        ai.TakeTurn();

        Assert.Equal((2, 2), (monster.X, monster.Y));
        Assert.True(monster.CombatComponent!.Wounds > 0);
    }

    [Fact]
    public void SimpleAiDoesStepOntoAPlainFloorTileTowardsThePlayer()
    {
        // Control for the test above: without the lava the monster closes the distance.
        var map = BareFloorMap();
        map.AddPlayer(NewCreature(4, 2));

        var ai = (SimpleAIComponent)
            AIComponentFactory.Create(SimpleAIComponent.ComponentId, map, SettingsMap.Empty);
        var monster = NewCreature(2, 2, ai);
        ai.Wake();

        ai.TakeTurn();

        Assert.Equal((3, 2), (monster.X, monster.Y));
    }

    static LevelConfiguration LevelWithSettings(SettingsMap settingsMap) =>
        new(
            number: 0,
            id: "liquid-pool-test-level",
            name: "Liquid Pool Test Level",
            height: 40,
            width: 40,
            generatorId: TestMapGenerator.Id,
            backgroundSoundtrack: "test.mp3",
            settingsMap: settingsMap
        );

    [Fact]
    public void AddLiquidPoolsCarvesPoolsOverFloorOnlyAndSparesGameObjectsAndPlayerStart()
    {
        var game = new Game();
        var settings = new SettingsMap(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["liquid_pools"] = new SettingsMap(
                            new Dictionary<string, object>
                            {
                                ["count_min"] = 6,
                                ["count_max"] = 6,
                                ["radius_min"] = 2,
                                ["radius_max"] = 3,
                                ["types"] = new List<(string, double)> { ("water_blue", 1.0) },
                            }
                        ),
                    }
                ),
            }
        );

        var map = MapGeneratorFactory.Create(LevelWithSettings(settings), game).GenerateMap();

        var liquidTiles = new List<(int X, int Y)>();
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Tiles[x, y].Liquid is not null)
                {
                    liquidTiles.Add((x, y));
                }
            }
        }

        Assert.NotEmpty(liquidTiles);
        Assert.All(
            liquidTiles,
            t =>
            {
                Assert.Equal(TileType.Liquid, map.Tiles[t.X, t.Y].TileType);
                Assert.False(map.Tiles[t.X, t.Y].Blocking);
                // never on the always-wall perimeter
                Assert.InRange(t.X, 1, map.Width - 2);
                Assert.InRange(t.Y, 1, map.Height - 2);
            }
        );

        // Stairs, the player and monsters were all placed after the pools and must have avoided them.
        Assert.Null(map.Tiles[map.Player.X, map.Player.Y].Liquid);
        Assert.All(map.GameObjects, go => Assert.Null(map.Tiles[go.X, go.Y].Liquid));
        Assert.All(map.Monsters, m => Assert.Null(map.Tiles[m.X, m.Y].Liquid));
    }

    [Fact]
    public void AddLiquidPoolsAlwaysPlacesOnePoolPerGuaranteedType()
    {
        var game = new Game();
        var settings = new SettingsMap(
            new Dictionary<string, object>
            {
                ["common"] = new SettingsMap(
                    new Dictionary<string, object>
                    {
                        ["liquid_pools"] = new SettingsMap(
                            new Dictionary<string, object>
                            {
                                ["count_min"] = 0,
                                ["count_max"] = 0,
                                ["radius_min"] = 2,
                                ["radius_max"] = 2,
                                ["always"] = new List<(string, double)> { ("water_bubbling", 1.0) },
                            }
                        ),
                    }
                ),
            }
        );

        var map = MapGeneratorFactory.Create(LevelWithSettings(settings), game).GenerateMap();

        var acidTiles = 0;
        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                if (map.Tiles[x, y].Liquid?.Id == "water_bubbling")
                {
                    acidTiles++;
                }
            }
        }

        Assert.True(acidTiles > 0, "expected a guaranteed acid pool from liquid_pools.always");
    }

    [Fact]
    public void AddLiquidPoolsIsANoOpWhenTheLevelHasNoLiquidPoolSettings()
    {
        var game = new Game();
        var map = MapGeneratorFactory
            .Create(LevelWithSettings(SettingsMap.Empty), game)
            .GenerateMap();

        for (int x = 0; x < map.Width; x++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                Assert.Null(map.Tiles[x, y].Liquid);
            }
        }
    }
}
