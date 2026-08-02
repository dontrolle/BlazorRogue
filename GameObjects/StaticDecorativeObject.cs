using System;
using BlazorRogue.Entities;

namespace BlazorRogue.GameObjects;

class StaticDecorativeObject : GameObject
{
    readonly string image;
    readonly string imgFolder;
    readonly int verticalOffset;
    readonly string character;
    readonly string characterColor;
    readonly Decoration.Layer decorationLayer;

    public StaticDecorativeObject(
        int x,
        int y,
        StaticDecorativeObjectType staticDecorativeObjectType,
        string? imageTag = null,
        int? verticalOffsetOverride = null,
        string? nameOverride = null,
        string? infoTextOverride = null,
        Decoration.Layer decorationLayer = Decoration.Layer.Middleground
    )
        : base(x, y, nameOverride ?? staticDecorativeObjectType.Name)
    {
        if (imageTag != null)
        {
            if (
                !staticDecorativeObjectType.ImageVariants.TryGetValue(
                    imageTag,
                    out string? imageVariant
                )
            )
            {
                throw new ArgumentException(
                    $"{nameof(imageTag)} must be a key into {nameof(staticDecorativeObjectType.ImageVariants)}."
                );
            }

            image = imageVariant;
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
        this.decorationLayer = decorationLayer;

        Blocking = staticDecorativeObjectType.Blocking;
    }

    public override void Render(Map map) =>
        map.Decorations[X, Y]
            .Add(
                new Decoration(this, image, imgFolder)
                {
                    VerticalOffset = verticalOffset,
                    Character = character,
                    CharacterColor = characterColor,
                    DecorationLayer = decorationLayer,
                }
            );
}
