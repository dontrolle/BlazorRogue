using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorRogue.Entities;

class StaticDecorativeObjectType(
    string id,
    string name,
    Dictionary<string, string> image,
    string infoText,
    int verticalOffset,
    string character,
    string characterColor,
    bool blocking,
    string imgFolder
)
{
    readonly Random random = new();

    public string Id { get; } = id;
    public string Name { get; } = name;
    readonly Dictionary<string, string> imageVariants = image;
    public IReadOnlyDictionary<string, string> ImageVariants => imageVariants;

    public string InfoText { get; } = infoText;
    public int VerticalOffset { get; } = verticalOffset;
    public string Character { get; } = character;
    public string CharacterColor { get; } = characterColor;
    public bool Blocking { get; } = blocking;
    public string ImgFolder { get; } = imgFolder;

    int RandomImageVariantIndex => random.Next(0, imageVariants.Count - 1);

    public string RandomImage => imageVariants.ElementAt(RandomImageVariantIndex).Value;
}
