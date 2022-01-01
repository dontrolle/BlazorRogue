using System;
using System.Linq;

namespace BlazorRogue.Entities
{
  /// <summary>
  /// A small collection of images and characters for tiles intended to be used together, e.g., a set of blue wall-tiles, 
  /// or a set of floor-tiles that goes well together.
  /// </summary>
  /// <remarks>
  /// Designed for tilesets from Ultimate Fantasy set, right now.
  /// (Interface employed by rest of code is primarily to get <see cref="ImageName"/> and <see cref="Character"/> & <see cref="CharacterColor"/>, though, 
  /// so pretty simple to refactor.)
  /// 
  /// Allows only a single character + color for entire tileset.
  /// </remarks>
  public class TileSet
  {
    public string Id { get; }
    public TileType TileType { get; }
    public string ImgPrefix { get; }

    // TODO I should really do something about the collection types of these arrays
    public int[] ImageBaseIndexes { get; }
    public double[] ImageBaseWeights { get; }
    public int[] ImageSouthEdgeIndexes { get; }
    public double[] ImageSouthEdgeWeights { get; }
    public int[] ImageSimpleEdgeNorthIndexes { get; }
    public int[] ImageDecoratedEdgeNorthIndexes { get; }
    public int[] ImageEdgeNorthIndexes => ImageSimpleEdgeNorthIndexes.Concat(ImageDecoratedEdgeNorthIndexes).ToArray();

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
        string character = "¤",
        string characterColor = "fuchsia")
    {
      Id = id;
      TileType = tileType;
      ImgPrefix = imgPrefix;

      ImageBaseIndexes = imgBaseIndexes;
      ImageBaseWeights = SetWeights(ImageBaseIndexes, imgBaseWeights);

      ImageSouthEdgeIndexes = imgSouthEdgeIndexes ?? Array.Empty<int>();
      ImageSouthEdgeWeights = SetWeights(ImageSouthEdgeIndexes, imgSouthEdgeWeights);

      ImageSimpleEdgeNorthIndexes = imgSimpleEdgeNorthIndexes ?? Array.Empty<int>();
      ImageDecoratedEdgeNorthIndexes = imgDecoratedEdgeNorthIndexes ?? Array.Empty<int>();

      Character = character;
      CharacterColor = characterColor;
    }

    private double[] SetWeights(int[] imgIndexes, double[]? imgWeights)
    {
      if (imgWeights != null)
      {
        if (imgWeights.Length != imgIndexes.Length)
        {
          throw new ArgumentException($"If given, {nameof(imgWeights)} is required to be of same length as {nameof(imgIndexes)}", nameof(imgWeights));
        }

        return imgWeights;
      }
      else
      {
        return Enumerable.Repeat(1.0d, imgIndexes.Length).ToArray();
      }
    }

    public virtual string ImageName(int index) => TileType.ToTileSetPrefix() + "_" + ImgPrefix + "_" + index;
  }
}
