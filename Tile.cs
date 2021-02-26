using BlazorRogue.Entities;

namespace BlazorRogue
{
    public class Tile
    {
        public Tile(int x, int y, TileSet tileSet, int tileIndex)
        {
            this.x = x;
            this.y = y;
            TileSet = tileSet;
            TileIndex = tileIndex;
        }

        public int x { get; }
        public int y { get; }
        public TileSet TileSet { get; set; }
        public int TileIndex { get; set; }

        public string ImageName { get => TileSet.ImageName(TileIndex); }
        public TileType TileType => TileSet.TileType;

        public string Character => TileSet.Character;
        public string CharacterColor => TileSet.CharacterColor;

        // For now, all blocking tiles also block light. If I make windows, this needs to change. 
        public bool Blocking { get; set; } = false;
    }
}
