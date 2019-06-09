using System;

// GameObjects are all sorts of objects.
// They know how to be rendered in to one of more decorations on this or the surrounding tiles.
public abstract class GameObject
{
    //public abstract string ImageName { get; set; }

    public int x { get; set; }
    public int y { get; set; }

    public abstract void RenderAt(Decoration[,] decorations, string wallSet);
    // {
    //     decorations[x, y].Images.Add(ImageName);
    // }
}