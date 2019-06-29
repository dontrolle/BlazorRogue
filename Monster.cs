using System;

public class Monster : GameObject
{
    public Monster(int x, int y) : base(x,y) {
        InvisibleOutsideFov = true;
        Blocking = true;
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