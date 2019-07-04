using System;

namespace BlazorRogue
{
    public class HalfWall : GameObject
    {
        private int HalfWallIndex;
        public HalfWall(int x, int y, int halfWallIndex) : base(x, y)
        {
            HalfWallIndex = halfWallIndex;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, "wall_" + map.DungeonWallSet + "_" + HalfWallIndex) { Offset = -Map.TileHeight });
        }
    }
}
