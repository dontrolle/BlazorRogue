using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.Entities
{
  public class StaticDecorativeObjectType(string id, string name, Dictionary<string, string> image, string infoText, int verticalOffset, string character, string characterColor, bool blocking, string imgFolder)
  {
    private readonly Random random = new();

    public string Id { get; } = id;
    public string Name { get; } = name;
    private readonly Dictionary<string, string> imageVariants = image;
    public IReadOnlyDictionary<string, string> ImageVariants => imageVariants;

    public string InfoText { get; } = infoText;
    public int VerticalOffset { get; } = verticalOffset;
    public string Character { get; } = character;
    public string CharacterColor { get; } = characterColor;
    public bool Blocking { get; } = blocking;
    public string ImgFolder { get; } = imgFolder;

    private int RandomImageVariantIndex => random.Next(0, imageVariants.Count - 1);

    public string RandomImage => imageVariants.ElementAt(RandomImageVariantIndex).Value;
  }
}
