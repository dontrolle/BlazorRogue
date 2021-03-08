using System;

namespace BlazorRogue.GameObjects
{
    public class HalfWall : GameObject
    {
        private readonly int HalfWallIndex;
        public HalfWall(int x, int y, int halfWallIndex) : base(x, y, "Halfwall")
        {
            HalfWallIndex = halfWallIndex;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, "wall_" + map.DungeonWallSet.ImgPrefix + "_" + HalfWallIndex) { VerticalOffset = -Map.DEPRECATE_TileHeight, Character = "" });
        }
    }
}
