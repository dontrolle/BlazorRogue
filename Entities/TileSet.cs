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
        // PICKUP: I probably need to make place for the halfwalf wall and wall-decoration stuff that hangs onto wall tiles...?

        public string Id { get; }
        public TileType TileType { get; }
        public string ImgPrefix { get; }
        public int[] ImgIndexes { get; }
        public double[] ImgWeights { get;  }
        public string Character { get; }
        public string CharacterColor { get; }

        public TileSet(string id, TileType tileType, string imgPrefix, int[] imgIndexes, double[]? imgWeights = null, string character = "¤", string characterColor = "fuchsia")
        {
            Id = id;
            TileType = tileType;
            ImgPrefix = imgPrefix;
            ImgIndexes = imgIndexes;
            if (imgWeights != null)
            {
                if(imgWeights.Length != imgIndexes.Length)
                {
                    throw new ArgumentException($"If given, {nameof(imgWeights)} is required to be of same length as {nameof(imgIndexes)}", nameof(imgWeights));
                }

                ImgWeights = imgWeights;
            }
            else
            {
                ImgWeights = Enumerable.Repeat(1.0d, imgIndexes.Length).ToArray();
            }

            Character = character;
            CharacterColor = characterColor;
        }

        public virtual string ImageName(int index) => TileType.ToTileSetPrefix() + "_" + ImgPrefix + "_" + index;
    }
}
