using System;
// Various fairly static TileSet info - should really be config files, I guess
public static class TileSetInfo
{
    public static string ToTileSetPrefix(this TileType tileType){
        switch(tileType)
        {
            case TileType.Floor : return "floor"; 
            case TileType.Wall : return "wall"; 
            case TileType.Door : return "door";
            case TileType.Ground : return "ground";
            default : throw new InvalidOperationException("Unknown TileType" + tileType);
        }
    }
}