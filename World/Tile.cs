using System;
using System.Linq;
using BlazorRogue.Entities;
using BlazorRogue.GameObjects;

namespace BlazorRogue.World;

class Tile(int x, int y, TileSet tileSet, int tileIndex)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public TileSet TileSet { get; set; } = tileSet;
    public int TileIndex { get; set; } = tileIndex;

    public string ImageName => TileSet.ImageName(TileIndex);
    public string ImageUrl => $"img/uf_terrain/{ImageName}.png";
    public TileType TileType => TileSet.TileType;

    public string Character => TileSet.Character;
    public string CharacterColor => TileSet.CharacterColor;

    // For now, all blocking tiles also block light. If I make windows, this needs to change.
    public bool Blocking { get; set; }

    static readonly Random Random = new();

    // The wall-face image pool and the half-wall pool both include a random pick, cached here on
    // first Render() so a neighboring tile changing (and re-triggering this tile's Render, e.g.
    // once tunneling exists) doesn't re-roll a different sprite for a tile that itself hasn't
    // changed. Never reset - TileIndex, this tile's own random base pick, is cached the same way
    // (as a constructor argument), so this mirrors that.
    int? halfWallIndex;
    int? southFaceIndex;

    static bool IsOpen(TileType tileType) => tileType is TileType.Floor or TileType.Black;

    static bool ContainsDoor(Map map, int x, int y) =>
        map.GameObjectByCoord[x, y].Any(go => go is Door);

    /// <summary>
    /// Renders this tile's own wall dressing (half-wall, wall-face, corner edges) into
    /// <see cref="Map.Decorations"/> at (X, Y) - the UF-tileset "frills" that used to live in
    /// MapGeneratorBase.AddPostGenWallDecorations and the generator-specific
    /// ExtraDecorationOn*/WallEdge/HalfWall machinery. Purely a function of this tile's own type
    /// plus its neighbors' types (all read from <paramref name="map"/>), so it's safe to call
    /// repeatedly - non-wall tiles simply produce nothing.
    /// </summary>
    public void Render(Map map)
    {
        if (TileType != TileType.Wall)
        {
            return;
        }

        var owner = new TileDecorationOwner(X, Y);

        bool hasFloorAbove = Y > 0 && IsOpen(map.Tiles[X, Y - 1].TileType);
        bool hasFloorBelow = Y < map.Height - 1 && IsOpen(map.Tiles[X, Y + 1].TileType);
        bool hasFloorLeft = X > 0 && IsOpen(map.Tiles[X - 1, Y].TileType);
        bool hasFloorRight = X < map.Width - 1 && IsOpen(map.Tiles[X + 1, Y].TileType);

        if (hasFloorAbove)
        {
            RenderHalfWall(map, owner);
            RenderEdges(map, owner, TileSet.EdgeNorthIndexes, hasFloorLeft, hasFloorRight, -1);
        }

        if (hasFloorBelow)
        {
            RenderSouthFace(map, owner);
            RenderEdges(map, owner, TileSet.EdgeSouthIndexes, hasFloorLeft, hasFloorRight, 0);
        }

        RenderEdges(map, owner, TileSet.EdgeFreeIndexes, hasFloorLeft, hasFloorRight, 0);
    }

    void RenderHalfWall(Map map, TileDecorationOwner owner)
    {
        // if tile above has a door, restrict to the simpler pool, else usually (3/4) simple too -
        // "decorated" half-wall art is rare and never appears directly above a door.
        halfWallIndex ??= PickHalfWallIndex(ContainsDoor(map, X, Y - 1));

        map.Decorations[X, Y]
            .Add(
                new Decoration(owner, TileSet.ImageName(halfWallIndex.Value))
                {
                    VerticalOffset = -1,
                    Character = "",
                }
            );
    }

    int PickHalfWallIndex(bool doorAbove)
    {
        bool restrictToSimplerHalfWall = doorAbove || Random.Next(0, 4) < 3;
        int[] pool = restrictToSimplerHalfWall
            ? TileSet.ImageSimpleEdgeNorthIndexes
            : TileSet.ImageEdgeNorthIndexes;
        return pool[Random.Next(0, pool.Length)];
    }

    void RenderSouthFace(Map map, TileDecorationOwner owner)
    {
        // if tile below has a door, always use index 14; else weighted-pick from the wall-set's
        // south-face pool.
        southFaceIndex ??= ContainsDoor(map, X, Y + 1)
            ? 14 // TODO: UF
            : TileSet.PickWeighted(
                TileSet.ImageSouthEdgeIndexes,
                TileSet.ImageSouthEdgeWeights,
                Random
            );

        map.Decorations[X, Y].Add(new Decoration(owner, TileSet.ImageName(southFaceIndex.Value)));
    }

    void RenderEdges(
        Map map,
        TileDecorationOwner owner,
        (int Left, int Right)? edgeIndexes,
        bool hasFloorLeft,
        bool hasFloorRight,
        int verticalOffset
    )
    {
        // Wall-sets that don't define edge art for this category (e.g. dungeon/crypt/ruins never
        // do) simply get no edge decoration here - this is opportunistic, not required.
        if (edgeIndexes is null)
        {
            return;
        }

        var (left, right) = edgeIndexes.Value;

        if (hasFloorLeft)
        {
            map.Decorations[X, Y]
                .Add(
                    new Decoration(owner, TileSet.EdgeImageName(left))
                    {
                        VerticalOffset = verticalOffset,
                        HorizontalOffset = -1,
                    }
                );
        }

        if (hasFloorRight)
        {
            map.Decorations[X, Y]
                .Add(
                    new Decoration(owner, TileSet.EdgeImageName(right))
                    {
                        VerticalOffset = verticalOffset,
                        HorizontalOffset = 1,
                    }
                );
        }
    }
}
