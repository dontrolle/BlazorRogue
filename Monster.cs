using System;

public class Monster : GameObject
{
    public Monster(int x, int y) : base(x,y) {
        InvisibleOutsideFov = true;
        Blocking = true;
        // Note, can't block light due to the way moveables are treated in Map
    }

    public override void Render(Map map)
    {
        map.MoveableDecorations[x,y].Add(new Decoration(this, null){AnimationClass = "animated_goblin"});
    }

    public void Move(int xDelta, int yDelta)
    {
        this.x += xDelta;
        this.y += yDelta;
    }
}