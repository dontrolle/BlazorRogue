using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
  public class Decoration
  {
    public GameObject GameObject { get; private set; }
    public string? ImageName { get; private set; }
    public string ImageFolder { get; private set; }
    public string? AnimationClass { get; set; }
    public Action? OnUse { get; set; }
    public int VerticalOffset { get; set; }
    public int HorizontalOffset { get; set; }
    public bool InFront { get; set; }
    public bool BlocksLight => GameObject.BlocksLight;
    public bool Shake { get; set; }

    private string? character;

    public string Character
    {
      get
      {
        if (character != null)
        {
          return character;
        }

        return "";
      }
      set
      {
        character = value;
      }
    }

    public string CharacterColor { get; set; } = "orange";

    public Decoration(GameObject gameObject, string? imageName, string imageFolder = "uf_terrain")
    {
      GameObject = gameObject;
      ImageName = imageName;
      ImageFolder = imageFolder;
    }
  }
}
