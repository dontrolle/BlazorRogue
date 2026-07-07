using System;

namespace BlazorRogue.GameObjects
{
  public class Torch(int x, int y) : GameObject(x, y, "Torch")
  {
    public override void Render(Map map)
    {
      map.Decorations[x, y].Add(new Decoration(this, null) { AnimationClass = "animated_torch", Character = "#", CharacterColor = "#FFFF00" });
      map.Decorations[x, y].Add(new Decoration(this, null) { AnimationClass = "animated_torch_floor", VerticalOffset = 1 });
    }
  }
}
