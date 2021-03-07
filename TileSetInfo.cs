using System;
// Various fairly static TileSet info - should really be config files, I guess
namespace BlazorRogue
{
    public static class TileSetInfo
    {
        public static string ToTileSetPrefix(this TileType tileType)
        {
            return tileType switch
            {
                TileType.Black => "floor",
                TileType.Floor => "floor",
                TileType.Wall => "wall",
                TileType.Ground => "ground",
                _ => throw new InvalidOperationException("Unknown TileType" + tileType),
            };
        }
    }
}
