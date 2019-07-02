using System;

public abstract class Monster : GameObject
{
    public string AnimationClass { get; protected set; }

    // public AIComponent AIComponent {get; set;}
    protected Monster(int x, int y, string animationClass) : base(x,y) {
        InvisibleOutsideFov = true;
        Blocking = true;
        AnimationClass = animationClass;

        // Note, can't block light due to the way moveables are treated in Map
    }

    protected Monster(int x, int y) : base(x,y) {
        InvisibleOutsideFov = true;
        Blocking = true;

        // Note, can't block light due to the way moveables are treated in Map
    }    

    public override void Render(Map map)
    {
        if(AnimationClass==null){
            throw new InvalidOperationException("AnimationClass not set.");
        }

        map.MoveableDecorations[x,y].Add(new Decoration(this, null){AnimationClass = AnimationClass});
    }

    public void Move(int xDelta, int yDelta)
    {
        this.x += xDelta;
        this.y += yDelta;
    }
}