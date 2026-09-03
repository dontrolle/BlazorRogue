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

    // When set, this tile is a walkable pool of the given liquid (see Map.SetLiquidTile). TileSet
    // is swapped to a plain black substrate; the animated surface plus shoreline dressing are
    // emitted as decorations by RenderLiquid below.
    public LiquidType? Liquid { get; set; }

    // Set (and reset) each Render() call for a freestanding wall tile (see Render below) - the
    // tile is still, in every other respect, a normal wall (Blocking, TileType, Character all stay
    // keyed to TileSet/TileIndex as usual); only the graphical background image is swapped for a
    // neighboring floor tile's, since the freestanding sprite art is a decoration layered on top
    // rather than a full opaque tile in its own right.
    (TileSet TileSet, int TileIndex)? floorUnderlay;

    public string ImageName =>
        floorUnderlay is { } underlay
            ? underlay.TileSet.ImageName(underlay.TileIndex)
            : TileSet.ImageName(TileIndex);
    public string ImageUrl => $"img/uf_terrain/{ImageName}.png";
    public TileType TileType => Liquid is not null ? TileType.Liquid : TileSet.TileType;

    public string Character => Liquid is not null ? LiquidType.AsciiCharacter : TileSet.Character;
    public string CharacterColor => Liquid is not null ? Liquid.AsciiColor : TileSet.CharacterColor;

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

    static bool IsOpen(TileType tileType) =>
        tileType is TileType.Floor or TileType.Black or TileType.Liquid;

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
        if (Liquid is not null)
        {
            RenderLiquid(map);
            return;
        }

        if (TileType != TileType.Wall)
        {
            return;
        }

        floorUnderlay = null;

        var owner = new TileDecorationOwner(X, Y);

        bool hasFloorAbove = Y > 0 && IsOpen(map.Tiles[X, Y - 1].TileType);
        bool hasFloorBelow = Y < map.Height - 1 && IsOpen(map.Tiles[X, Y + 1].TileType);
        bool hasFloorLeft = X > 0 && IsOpen(map.Tiles[X - 1, Y].TileType);
        bool hasFloorRight = X < map.Width - 1 && IsOpen(map.Tiles[X + 1, Y].TileType);

        // A wall tile with open ground on all four sides is drawn as a dedicated freestanding
        // sprite layered over a borrowed floor image (all four neighbors are open, so the north
        // one is always available here), instead of the usual base image + half-wall/south-
        // face/edge frills. The freestanding art has transparent margins - unlike the base wall
        // images - so it needs an actual floor tile underneath it, not just its own background.
        if (hasFloorAbove && hasFloorBelow && hasFloorLeft && hasFloorRight)
        {
            var floorNeighbor = map.Tiles[X, Y - 1];
            floorUnderlay = (floorNeighbor.TileSet, floorNeighbor.TileIndex);
            map.Decorations[X, Y]
                .Add(new Decoration(owner, TileSet.ImageName(TileSet.ImageFreestandingIndex)));
            RenderShadow(map, owner, "shadow_3");
            return;
        }

        if (hasFloorAbove)
        {
            RenderHalfWall(map, owner);
            RenderEdges(map, owner, TileSet.EdgeNorthIndexes, hasFloorLeft, hasFloorRight, -1);
        }

        if (hasFloorBelow)
        {
            RenderSouthFace(map, owner);
            RenderEdges(map, owner, TileSet.EdgeSouthIndexes, hasFloorLeft, hasFloorRight, 0);
            RenderShadow(map, owner, "shadow_1");
        }

        RenderEdges(map, owner, TileSet.EdgeFreeIndexes, hasFloorLeft, hasFloorRight, 0);
    }

    /// <summary>
    /// Emits a liquid pool tile's decorations: the animated surface, the shoreline edging pieces
    /// (see <see cref="LiquidEdging"/>) rotated to face each land neighbour, and - against a
    /// northern shore - the lip shadow on top. All on <see cref="Decoration.Layer.Behind"/> so
    /// moveables, items and blood puddles draw over the water.
    /// </summary>
    void RenderLiquid(Map map)
    {
        var liquid = Liquid!;
        string displayName = char.ToUpperInvariant(liquid.Name[0]) + liquid.Name[1..];

        // The mouse-over tooltip must never be rotated, so it goes only on the un-rotated surface
        // and lip decorations; the rotated edging pieces get a tooltip-less owner (they also have
        // no pointer-events, so a hover falls through to the surface below).
        var describedOwner = new TileDecorationOwner(X, Y, displayName) { InfoText = displayName };
        var edgingOwner = new TileDecorationOwner(X, Y, displayName);

        map.Decorations[X, Y]
            .Add(
                new Decoration(describedOwner, null)
                {
                    AnimationClass = liquid.AnimationClass,
                    DecorationLayer = Decoration.Layer.Behind,
                    // Carries the '~' glyph in ASCII mode (the tile draws the same glyph underneath,
                    // so this just overlays it) - without a Character the decoration renders no div
                    // in ASCII and its mouse-over tooltip has nothing to attach to.
                    Character = LiquidType.AsciiCharacter,
                    CharacterColor = liquid.AsciiColor,
                }
            );

        bool IsLand(int nx, int ny) =>
            nx < 0
            || ny < 0
            || nx >= map.Width
            || ny >= map.Height
            || map.Tiles[nx, ny].Liquid is null;

        bool n = IsLand(X, Y - 1);
        bool e = IsLand(X + 1, Y);
        bool s = IsLand(X, Y + 1);
        bool w = IsLand(X - 1, Y);
        bool nw = IsLand(X - 1, Y - 1);
        bool ne = IsLand(X + 1, Y - 1);
        bool sw = IsLand(X - 1, Y + 1);
        bool se = IsLand(X + 1, Y + 1);

        foreach (var (image, rotation, mirror) in LiquidEdging.Overlays(n, e, s, w, nw, ne, sw, se))
        {
            map.Decorations[X, Y]
                .Add(
                    new Decoration(edgingOwner, image)
                    {
                        DecorationLayer = Decoration.Layer.Behind,
                        RotationDegrees = rotation,
                        MirrorX = mirror,
                    }
                );
        }

        if (n)
        {
            map.Decorations[X, Y]
                .Add(
                    new Decoration(describedOwner, $"water_lip_{liquid.LipIndex}")
                    {
                        DecorationLayer = Decoration.Layer.Behind,
                    }
                );
        }
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

    // Casts the wall's shadow onto the floor tile below - same "own cell, offset into the
    // neighbor" trick as RenderHalfWall's VerticalOffset = -1, just pointed the other way.
    void RenderShadow(Map map, TileDecorationOwner owner, string imageName) =>
        map.Decorations[X, Y].Add(new Decoration(owner, imageName) { VerticalOffset = 1 });

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
