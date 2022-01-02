using System;
using System.Linq;
using BlazorRogue.Entities;

namespace BlazorRogue.GameObjects
{
  public class StaticDecorativeObject : GameObject
  {
    private readonly string image;
    private readonly string imgFolder;
    private readonly int verticalOffset;
    private readonly string character;
    private readonly string characterColor;

    public StaticDecorativeObject(
        int x,
        int y,
        StaticDecorativeObjectType staticDecorativeObjectType,
        string? imageTag = null,
        int? verticalOffsetOverride = null,
        string? nameOverride = null,
        string? infoTextOverride = null) : base(x, y, nameOverride ?? staticDecorativeObjectType.Name)
    {
      if (imageTag != null)
      {
        if (!staticDecorativeObjectType.ImageVariants.TryGetValue(imageTag, out image))
        {
          throw new ArgumentException($"{nameof(imageTag)} must be a key into {nameof(staticDecorativeObjectType.ImageVariants)}.");
        }
      }
      else
      {
        // if no tag is given, select a random image among the variants given
        image = staticDecorativeObjectType.RandomImage;
      }

      imgFolder = staticDecorativeObjectType.ImgFolder;
      InfoText = infoTextOverride ?? staticDecorativeObjectType.InfoText;
      verticalOffset = verticalOffsetOverride ?? staticDecorativeObjectType.VerticalOffset;
      character = staticDecorativeObjectType.Character;
      characterColor = staticDecorativeObjectType.CharacterColor;

      Blocking = staticDecorativeObjectType.Blocking;
    }

    public override void Render(Map map)
    {
      map.Decorations[x, y].Add(new Decoration(this, image, imgFolder) { VerticalOffset = verticalOffset, Character = character, CharacterColor = characterColor });
    }
  }
}