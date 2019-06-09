// TODO rename to Tile
public class MapPosition {

    public MapPosition(int x, int y, TileType tileType, string tileSet, int tileIndex)
    {
        this.x = x;
        this.y = y;
        TileType = tileType;
        TileSet = tileSet;
        TileIndex = tileIndex;
    }

    public string ImageName { get => TileType.ToTileSetPrefix() + "_" + TileSet + "_" + TileIndex; }
    public string TileSet { get; set; }
    public int TileIndex { get; set; }
    public TileType TileType { get; set; }
    public int x { get; }
    public int y { get; }
}