using System;

// GameObjects are all sorts of objects.
// They know how to be rendered in to one of more decorations on this or the surrounding tiles.
public abstract class GameObject
{
    public int x { get; protected set; }
    public int y { get; protected set; }
    public bool Blocking {get; set;} = false;
    public bool BlocksLight {get; set;} = false;
    public bool InvisibleOutsideFov {get; set;} = false;

    public GameObject (int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public abstract void Render(Map map);
}