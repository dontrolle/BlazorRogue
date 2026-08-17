using System;
using System.Linq;
using BlazorRogue.World;

namespace BlazorRogue.Entities;

/// <summary>
/// A small collection of images and characters for tiles intended to be used together, e.g., a set of blue wall-tiles,
/// or a set of floor-tiles that goes well together.
/// </summary>
/// <remarks>
/// Designed for tilesets from Ultimate Fantasy set, right now.
/// (Interface employed by rest of code is primarily to get <see cref="ImageName"/> and <see cref="Character"/> and <see cref="CharacterColor"/>, though,
/// so pretty simple to refactor.)
///
/// Allows only a single character + color for entire tileset.
/// </remarks>
class TileSet
{
    public string Id { get; }
    public TileType TileType { get; }
    public string ImgPrefix { get; }

    // TODO: I should really do something about the collection types of these arrays
    public int[] ImageBaseIndexes { get; }
    public double[] ImageBaseWeights { get; }
    public int[] ImageSouthEdgeIndexes { get; }
    public double[] ImageSouthEdgeWeights { get; }
    public int[] ImageSimpleEdgeNorthIndexes { get; }
    public int[] ImageDecoratedEdgeNorthIndexes { get; }
    public int[] ImageEdgeNorthIndexes =>
        [.. ImageSimpleEdgeNorthIndexes, .. ImageDecoratedEdgeNorthIndexes];

    // Cave-generator-style wall edge filler art (see WallEdge): (Left, Right) index pairs, used
    // when the neighboring tile to the west/east respectively is floor/black. Null when this
    // wall-set doesn't define edge art (e.g. the dungeon-generator wall-sets never need it).
    public (int Left, int Right)? EdgeNorthIndexes { get; }
    public (int Left, int Right)? EdgeSouthIndexes { get; }
    public (int Left, int Right)? EdgeFreeIndexes { get; }

    public string Character { get; }
    public string CharacterColor { get; }

    public TileSet(
        string id,
        TileType tileType,
        string imgPrefix,
        int[] imgBaseIndexes,
        double[]? imgBaseWeights = null,
        int[]? imgSouthEdgeIndexes = null,
        double[]? imgSouthEdgeWeights = null,
        int[]? imgSimpleEdgeNorthIndexes = null,
        int[]? imgDecoratedEdgeNorthIndexes = null,
        (int Left, int Right)? edgeNorthIndexes = null,
        (int Left, int Right)? edgeSouthIndexes = null,
        (int Left, int Right)? edgeFreeIndexes = null,
        string character = "¤",
        string characterColor = "fuchsia"
    )
    {
        Id = id;
        TileType = tileType;
        ImgPrefix = imgPrefix;

        ImageBaseIndexes = imgBaseIndexes;
        ImageBaseWeights = SetWeights(ImageBaseIndexes, imgBaseWeights);

        ImageSouthEdgeIndexes = imgSouthEdgeIndexes ?? [];
        ImageSouthEdgeWeights = SetWeights(ImageSouthEdgeIndexes, imgSouthEdgeWeights);

        ImageSimpleEdgeNorthIndexes = imgSimpleEdgeNorthIndexes ?? [];
        ImageDecoratedEdgeNorthIndexes = imgDecoratedEdgeNorthIndexes ?? [];

        EdgeNorthIndexes = edgeNorthIndexes;
        EdgeSouthIndexes = edgeSouthIndexes;
        EdgeFreeIndexes = edgeFreeIndexes;

        Character = character;
        CharacterColor = characterColor;
    }

    static double[] SetWeights(int[] imgIndexes, double[]? imgWeights) =>
        imgWeights != null
            ? imgWeights.Length != imgIndexes.Length
                ? throw new ArgumentException(
                    $"If given, {nameof(imgWeights)} is required to be of same length as {nameof(imgIndexes)}",
                    nameof(imgWeights)
                )
                : imgWeights
            : [.. Enumerable.Repeat(1.0d, imgIndexes.Length)];

    // ImgPrefix is expected to be the literal image filename prefix (e.g. "floor_crusted_grey",
    // "wall_crypt", or "ground_grass" for assets that don't follow the floor_/wall_ convention) -
    // not derived from TileType, since not every asset in the source tileset follows that
    // convention (see wallsets.json/floorsets.json).
    public virtual string ImageName(int index) => ImgPrefix + "_" + index;

    public string EdgeImageName(int index) => ImgPrefix + "_edge_" + index;
}
