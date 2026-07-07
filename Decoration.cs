using System;
using BlazorRogue.GameObjects;

namespace BlazorRogue
{
  public class Decoration(GameObject gameObject, string? imageName, string imageFolder = "uf_terrain")
  {
    public GameObject GameObject { get; private set; } = gameObject;
    public string? ImageName { get; private set; } = imageName;
    public string ImageFolder { get; private set; } = imageFolder;
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
  }
}
