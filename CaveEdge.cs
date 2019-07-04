﻿using System;

namespace BlazorRogue
{
    public class CaveEdge : GameObject
    {
        private int CaveEdgeIndex;
        private readonly int Offset;
        private readonly int HOffset;

        public CaveEdge(int x, int y, int caveEdgeIndex, int offset, int hOffset) : base(x, y)
        {
            CaveEdgeIndex = caveEdgeIndex;
            Offset = offset;
            HOffset = hOffset;
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, "wall_cave_edge_" + CaveEdgeIndex) { HOffset = HOffset, Offset = Offset });
        }
    }
}
