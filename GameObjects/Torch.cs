using System;

namespace BlazorRogue.GameObjects
{
    public class Torch : GameObject
    {
        public Torch(int x, int y) : base(x, y, "Torch")
        {
        }

        public override void Render(Map map)
        {
            map.Decorations[x, y].Add(new Decoration(this, null) { AnimationClass = "animated_torch" });
            map.Decorations[x, y].Add(new Decoration(this, null) { AnimationClass = "animated_torch_floor", VerticalOffset = Map.DEPRECATE_TileHeight });
        }
    }
}
