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

    // Dedicated art for a wall tile with open (floor/black) tiles on all four orthogonal sides -
    // used instead of the usual base image + half-wall/south-face/edge frills (see Tile.Render).
    public int ImageFreestandingIndex { get; }

    // Cave-generator-style wall edge filler art (see WallEdge): (Left, Right) index pairs, used
    // when the neighboring tile to the west/east respectively is floor/black. Null when this
    // wall-set doesn't define edge art (e.g. the dungeon-generator wall-sets never need it).
    public (int Left, int Right)? EdgeNorthIndexes { get; }
    public (int Left, int Right)? EdgeSouthIndexes { get; }
    public (int Left, int Right)? EdgeFreeIndexes { get; }

    // Per-floorset stair art (see Data/floorsets.json's optional "img_stairs" object): weighted
    // lists of literal image names (not indexes relative to ImgPrefix, so a floorset's stairs can
    // borrow art from a different image prefix entirely). Null when this floorset doesn't define
    // its own stair art, in which case callers fall back to Configuration.DefaultStairsFloorSet.
    public (
        (string Name, double Weight)[] Up,
        (string Name, double Weight)[] Down
    )? StairImages { get; }

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
        ((string Name, double Weight)[] Up, (string Name, double Weight)[] Down)? stairImages =
            null,
        int imgFreestandingIndex = 0,
        string character = "¤",
        string characterColor = "fuchsia"
    )
    {
        Id = id;
        TileType = tileType;
        ImgPrefix = imgPrefix;

        ImageBaseIndexes = imgBaseIndexes;
        ImageBaseWeights = SetWeights(ImageBaseIndexes, imgBaseWeights);
        ImageFreestandingIndex = imgFreestandingIndex;

        ImageSouthEdgeIndexes = imgSouthEdgeIndexes ?? [];
        ImageSouthEdgeWeights = SetWeights(ImageSouthEdgeIndexes, imgSouthEdgeWeights);

        ImageSimpleEdgeNorthIndexes = imgSimpleEdgeNorthIndexes ?? [];
        ImageDecoratedEdgeNorthIndexes = imgDecoratedEdgeNorthIndexes ?? [];

        EdgeNorthIndexes = edgeNorthIndexes;
        EdgeSouthIndexes = edgeSouthIndexes;
        EdgeFreeIndexes = edgeFreeIndexes;

        StairImages = stairImages;

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

    /// <summary>
    /// Weighted-picks a literal image name from a <see cref="StairImages"/> Up/Down list. Kept
    /// standalone (rather than sharing MapGeneratorBase.WeightedPick's T[]-generic implementation)
    /// since it isn't reachable from GameObjects/Stair.cs and reorganizing map-gen code is out of
    /// scope for stair-image selection.
    /// </summary>
    internal static string PickWeighted((string Name, double Weight)[] options, Random rng)
    {
        double r = rng.NextDouble() * options.Sum(o => o.Weight);
        int i;
        for (i = 0; i < options.Length; i++)
        {
            if (r < options[i].Weight)
            {
                break;
            }
            r -= options[i].Weight;
        }

        return options[i].Name;
    }

    /// <summary>
    /// Weighted-picks an element from <paramref name="elements"/>, used by <see cref="Tile"/> for
    /// its own generation-time-independent index picks (e.g. south wall-face art).
    /// </summary>
    internal static T PickWeighted<T>(T[] elements, double[] weights, Random rng)
    {
        int i;
        double r = rng.NextDouble() * weights.Sum();
        for (i = 0; i < weights.Length; i++)
        {
            if (r < weights[i])
            {
                break;
            }
            r -= weights[i];
        }

        return elements[i];
    }
}
