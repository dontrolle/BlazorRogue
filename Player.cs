using System;

public class Player : GameObject
{
    public Player(int x, int y) : base(x,y) {
    }

    public override void Render(Map map)
    {
        map.MoveableDecorations[x,y].Add(new Decoration(this, null){AnimationClass = "animated_templar"});
    }

    public void Move(int xDelta, int yDelta)
    {
        this.x += xDelta;
        this.y += yDelta;
    }
}