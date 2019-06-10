using System;
using System.Linq;
public class Decoration 
{
    // public int x { get; set; }
    // public int y { get; set; }
    public GameObject GameObject { get; private set; }
    public string ImageName { get; private set; }

    public Action Interact { get; set; }

    public Decoration(GameObject gameObject, string imageName)
    {
        GameObject = gameObject;
        ImageName = imageName;
    }
}