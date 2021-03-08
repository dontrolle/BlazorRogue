using System;

namespace BlazorRogue.GameObjects
{
    public class CaveEdge : GameObject
    {
        private int CaveEdgeIndex;
        private readonly int Offset;
        private readonly int HOffset;

        public CaveEdge(int x, int y, int caveEdgeIndex, int offset, int hOffset) : base(x, y, "CaveEdge")
        {
            CaveEdgeIndex = caveEdgeIndex;
            Offset = offset;
            HOffset = hOffset;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, "wall_cave_edge_" + CaveEdgeIndex) { HorizontalOffset = HOffset, VerticalOffset = Offset });
        }
    }
}
