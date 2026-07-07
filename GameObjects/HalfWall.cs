using System;

namespace BlazorRogue.GameObjects
{
  public class HalfWall(int x, int y, int halfWallIndex) : GameObject(x, y, "Halfwall")
  {
    private readonly int HalfWallIndex = halfWallIndex;

    public override void Render(Map map)
    {
      map.Decorations[x, y].Add(new Decoration(this, map.DungeonWallSet.ImageName(HalfWallIndex)) { VerticalOffset = -1, Character = "" });
    }
  }
}
