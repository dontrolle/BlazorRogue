public class MapPosition {

    public MapPosition(int x, int y, TileType tileType, string imageName)
    {
        this.x = x;
        this.y = y;
        TileType = tileType;
        ImageName = imageName;
    }

    public string ImageName { get; set; }
    public TileType TileType { get; set; }
    public int x { get; }
    public int y { get; }
}