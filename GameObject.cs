using System;

// GameObjects are all sorts of objects.
// They know how to be rendered in to one of more decorations on this or the surrounding tiles.
public abstract class GameObject
{
    public int x { get; private set; }
    public int y { get; private set; }

    public GameObject (int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public abstract void Render(Map map);
}