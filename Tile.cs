using BlazorRogue.Entities;

namespace BlazorRogue
{
  public class Tile(int x, int y, TileSet tileSet, int tileIndex)
  {
    public int x { get; } = x;
    public int y { get; } = y;
    public TileSet TileSet { get; set; } = tileSet;
    public int TileIndex { get; set; } = tileIndex;

    public string ImageName { get => TileSet.ImageName(TileIndex); }
    public TileType TileType => TileSet.TileType;

    public string Character => TileSet.Character;
    public string CharacterColor => TileSet.CharacterColor;

    // For now, all blocking tiles also block light. If I make windows, this needs to change. 
    public bool Blocking { get; set; } = false;
  }
}
