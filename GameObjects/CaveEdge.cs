using System;

namespace BlazorRogue.GameObjects
{
  public class CaveEdge(int x, int y, int caveEdgeIndex, int offset, int hOffset) : GameObject(x, y, "CaveEdge")
  {
    private int CaveEdgeIndex = caveEdgeIndex;
    private readonly int Offset = offset;
    private readonly int HOffset = hOffset;

    public override void Render(Map map)
    {
      map.Decorations[x, y].Add(new Decoration(this, "wall_cave_edge_" + CaveEdgeIndex) { HorizontalOffset = HOffset, VerticalOffset = Offset });
    }
  }
}
