using System;

public class Torch : GameObject
{
    public Torch(int x, int y) : base(x,y) {
    }

    public override void Render(Map map)
    {
        map.Decorations[x,y].Add(new Decoration(this, null){AnimationClass = "animated_torch"});
        map.Decorations[x,y].Add(new Decoration(this, null){AnimationClass = "animated_torch_floor", Offset = Map.TileHeight});
    }
}