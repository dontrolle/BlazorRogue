using System.Linq;
using BlazorRogue.GameObjects;
using BlazorRogue.World;

namespace BlazorRogue.Tests;

/// <summary>
/// Covers Tile.Render - the UF-tileset wall dressing (half-wall, south wall-face, corner edges)
/// that used to live in MapGeneratorBase.AddPostGenWallDecorations plus CaveGenerator's
/// ExtraDecorationOn* overrides. Builds small controlled scenarios by overriding tiles on an
/// otherwise-normal generated Game/Map, the same way StairTests does.
/// </summary>
public class TileTests
{
    // Builds a wall tile with the given sides set to open floor and the rest left as wall, so
    // callers can exercise a single frill (half-wall/south-face/edge) without also tripping the
    // "surrounded on all four sides" freestanding case.
    static (Game Game, int X, int Y) SetUpWall(
        string wallSetId,
        bool floorAbove,
        bool floorBelow,
        bool floorLeft,
        bool floorRight,
        string floorSetId = "grey"
    )
    {
        var game = new Game();
        var wallSet = game.Configuration.WallSetById(wallSetId);
        var floorSet = game.Configuration.FloorSetById(floorSetId);

        int x = game.Map.Width / 2;
        int y = game.Map.Height / 2;

        game.Map.Tiles[x, y].TileSet = wallSet;
        game.Map.Tiles[x, y - 1].TileSet = floorAbove ? floorSet : wallSet;
        game.Map.Tiles[x, y + 1].TileSet = floorBelow ? floorSet : wallSet;
        game.Map.Tiles[x - 1, y].TileSet = floorLeft ? floorSet : wallSet;
        game.Map.Tiles[x + 1, y].TileSet = floorRight ? floorSet : wallSet;
        game.Map.Decorations[x, y].Clear();

        return (game, x, y);
    }

    static (Game Game, int X, int Y) SetUpWallSurroundedByFloor(
        string wallSetId,
        string floorSetId = "grey"
    ) => SetUpWall(wallSetId, true, true, true, true, floorSetId);

    [Fact]
    public void ImageUrlCombinesTheUfTerrainFolderWithTheTilesOwnImageName()
    {
        var game = new Game();
        int x = game.Map.Width / 2;
        int y = game.Map.Height / 2;
        game.Map.Tiles[x, y].TileSet = game.Configuration.WallSetById("cave");
        game.Map.Tiles[x, y].TileIndex = 7;

        Assert.Equal("img/uf_terrain/wall_cave_7.png", game.Map.Tiles[x, y].ImageUrl);
    }

    [Fact]
    public void RenderProducesNoDecorationsForNonWallTiles()
    {
        var game = new Game();
        int x = game.Map.Width / 2;
        int y = game.Map.Height / 2;
        game.Map.Tiles[x, y].TileSet = game.Configuration.FloorSetById("grey");
        game.Map.Decorations[x, y].Clear();

        game.Map.Tiles[x, y].Render(game.Map);

        Assert.Empty(game.Map.Decorations[x, y]);
    }

    [Fact]
    public void RenderAddsAHalfWallDecorationWhenFloorIsAbove()
    {
        var (game, x, y) = SetUpWall(
            "cave",
            floorAbove: true,
            floorBelow: false,
            floorLeft: false,
            floorRight: false
        );

        game.Map.Tiles[x, y].Render(game.Map);

        var halfWall = Assert.Single(
            game.Map.Decorations[x, y],
            d => d.VerticalOffset == -1 && d.HorizontalOffset == 0
        );
        Assert.StartsWith("wall_cave_", halfWall.ImageName);
    }

    [Fact]
    public void RenderAddsASouthFaceDecorationWhenFloorIsBelow()
    {
        var (game, x, y) = SetUpWall(
            "cave",
            floorAbove: false,
            floorBelow: true,
            floorLeft: false,
            floorRight: false
        );

        game.Map.Tiles[x, y].Render(game.Map);

        var southFace = Assert.Single(
            game.Map.Decorations[x, y],
            d => d.VerticalOffset == 0 && d.HorizontalOffset == 0
        );
        Assert.StartsWith("wall_cave_", southFace.ImageName);
        // Never mutates the tile's own base pick - the south-face art is a layered decoration,
        // not a TileIndex overwrite, so repeated/localized re-renders stay idempotent.
        Assert.NotEqual(southFace.ImageName, game.Map.Tiles[x, y].ImageName);
    }

    [Theory]
    [InlineData("crypt")]
    [InlineData("dungeon")]
    [InlineData("ruins")]
    [InlineData("cave")]
    [InlineData("hedge")]
    public void RenderUsesTheFreestandingImageWhenOpenOnAllFourSides(string wallSetId)
    {
        var (game, x, y) = SetUpWallSurroundedByFloor(wallSetId);
        var wallSet = game.Configuration.WallSetById(wallSetId);

        game.Map.Tiles[x, y].Render(game.Map);

        // The freestanding sprite has transparent margins, so it's layered as a decoration over a
        // borrowed floor image rather than replacing the tile's own base image outright.
        var pillar = Assert.Single(
            game.Map.Decorations[x, y],
            d => d.VerticalOffset == 0 && d.HorizontalOffset == 0
        );
        Assert.Equal(wallSet.ImageName(wallSet.ImageFreestandingIndex), pillar.ImageName);
        Assert.Equal(game.Map.Tiles[x, y - 1].ImageName, game.Map.Tiles[x, y].ImageName);
    }

    [Fact]
    public void RenderAddsFreeStandingEdgeDecorationsWhenWallSetDefinesThem()
    {
        var game = new Game();
        var wallSet = game.Configuration.WallSetById("cave");
        var floorSet = game.Configuration.FloorSetById("grey");
        int x = game.Map.Width / 2;
        int y = game.Map.Height / 2;

        // Wall above and below - only the free-standing (east/west) edge branch applies.
        game.Map.Tiles[x, y].TileSet = wallSet;
        game.Map.Tiles[x, y - 1].TileSet = wallSet;
        game.Map.Tiles[x, y + 1].TileSet = wallSet;
        game.Map.Tiles[x - 1, y].TileSet = floorSet;
        game.Map.Tiles[x + 1, y].TileSet = floorSet;
        game.Map.Decorations[x, y].Clear();

        game.Map.Tiles[x, y].Render(game.Map);

        Assert.Equal(2, game.Map.Decorations[x, y].Count);
        Assert.All(game.Map.Decorations[x, y], d => Assert.Equal(0, d.VerticalOffset));
        var left = Assert.Single(game.Map.Decorations[x, y], d => d.HorizontalOffset == -1);
        Assert.Equal("wall_cave_edge_3", left.ImageName); // cave's EdgeFreeIndexes = (3, 4)
        var right = Assert.Single(game.Map.Decorations[x, y], d => d.HorizontalOffset == 1);
        Assert.Equal("wall_cave_edge_4", right.ImageName);
    }

    [Fact]
    public void RenderSkipsEdgeDecorationsWhenWallSetHasNoEdgeIndexes()
    {
        // "crypt" defines no "edges" entry in wallsets.json - Tile.Render should silently skip
        // edge art for it rather than throwing, since edges are now opportunistic (available to
        // any generator/wall-set combination that defines them) rather than something only
        // CaveGenerator required.
        var (game, x, y) = SetUpWall(
            "crypt",
            floorAbove: true,
            floorBelow: true,
            floorLeft: false,
            floorRight: false
        );

        game.Map.Tiles[x, y].Render(game.Map);

        // Half-wall (north) and south-face still render; only edge art is skipped.
        Assert.Equal(2, game.Map.Decorations[x, y].Count);
        Assert.All(game.Map.Decorations[x, y], d => Assert.Equal(0, d.HorizontalOffset));
    }

    [Fact]
    public void RenderRestrictsHalfWallToTheSimplePoolWhenThereIsADoorAbove()
    {
        var game = new Game();
        var wallSet = game.Configuration.WallSetById("crypt"); // has a non-empty "decorated" pool
        var floorSet = game.Configuration.FloorSetById("grey");
        int x = game.Map.Width / 2;
        int y = game.Map.Height / 2;

        game.Map.Tiles[x, y].TileSet = wallSet;
        game.Map.Tiles[x, y - 1].TileSet = floorSet;
        game.Map.Tiles[x, y + 1].TileSet = wallSet;
        game.Map.Tiles[x - 1, y].TileSet = wallSet;
        game.Map.Tiles[x + 1, y].TileSet = wallSet;
        game.Map.AddGameObject(
            new Door(x, y - 1, "wood", 1, Orientation.Horizontal, isOpen: false)
        );
        game.Map.Decorations[x, y].Clear();

        game.Map.Tiles[x, y].Render(game.Map);

        var halfWall = Assert.Single(game.Map.Decorations[x, y], d => d.VerticalOffset == -1);
        int index = int.Parse(
            halfWall.ImageName!["wall_crypt_".Length..],
            System.Globalization.CultureInfo.InvariantCulture
        );
        Assert.Contains(index, wallSet.ImageSimpleEdgeNorthIndexes);
        Assert.DoesNotContain(index, wallSet.ImageDecoratedEdgeNorthIndexes);
    }

    [Fact]
    public void RenderForcesSouthFaceIndex14WhenThereIsADoorBelow()
    {
        var game = new Game();
        var wallSet = game.Configuration.WallSetById("cave");
        var floorSet = game.Configuration.FloorSetById("grey");
        int x = game.Map.Width / 2;
        int y = game.Map.Height / 2;

        game.Map.Tiles[x, y].TileSet = wallSet;
        game.Map.Tiles[x, y - 1].TileSet = wallSet;
        game.Map.Tiles[x, y + 1].TileSet = floorSet;
        game.Map.Tiles[x - 1, y].TileSet = wallSet;
        game.Map.Tiles[x + 1, y].TileSet = wallSet;
        game.Map.AddGameObject(
            new Door(x, y + 1, "wood", 1, Orientation.Horizontal, isOpen: false)
        );
        game.Map.Decorations[x, y].Clear();

        game.Map.Tiles[x, y].Render(game.Map);

        var southFace = Assert.Single(
            game.Map.Decorations[x, y],
            d => d.VerticalOffset == 0 && d.HorizontalOffset == 0
        );
        Assert.Equal("wall_cave_14", southFace.ImageName);
    }

    [Fact]
    public void RenderIsIdempotentAcrossRepeatedCalls()
    {
        // The half-wall/south-face picks are random - repeated Render() calls (e.g. triggered by
        // a neighboring tile changing, once dynamic maps exist) must not re-roll a different
        // sprite for a tile that itself hasn't changed.
        var (game, x, y) = SetUpWall(
            "cave",
            floorAbove: true,
            floorBelow: true,
            floorLeft: false,
            floorRight: false
        );

        game.Map.Tiles[x, y].Render(game.Map);
        var firstPass = game
            .Map.Decorations[x, y]
            .Select(d => (d.ImageName, d.VerticalOffset, d.HorizontalOffset))
            .ToList();

        game.Map.Decorations[x, y].Clear();
        game.Map.Tiles[x, y].Render(game.Map);
        var secondPass = game
            .Map.Decorations[x, y]
            .Select(d => (d.ImageName, d.VerticalOffset, d.HorizontalOffset))
            .ToList();

        Assert.Equal(firstPass, secondPass);
    }
}
