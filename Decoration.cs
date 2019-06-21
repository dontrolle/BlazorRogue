using System;
using System.Linq;
public class Decoration
{
    public GameObject GameObject { get; private set; }
    public string ImageName { get; private set; }
    public string AnimationClass { get; set; }
    public Action OnClick { get; set; }
    public int Offset { get; set; }
    public bool InFront { get; set; }

    public Decoration(GameObject gameObject, string imageName)
    {
        GameObject = gameObject;
        ImageName = imageName;
    }
}