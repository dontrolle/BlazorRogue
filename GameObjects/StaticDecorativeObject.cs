using System;
using BlazorRogue.Entities;
using BlazorRogue.World;

namespace BlazorRogue.GameObjects;

class StaticDecorativeObject : GameObject
{
    readonly string image;
    readonly string? animationClass;
    readonly int verticalOffset;
    readonly string character;
    readonly string characterColor;
    readonly Decoration.Layer decorationLayer;
    readonly bool makeCoveringOffsetDecsTransparent;

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
            animationClass = staticDecorativeObjectType.AnimationClassForTag(imageTag);
        }
        else
        {
            // if no tag is given, select a random image (and whichever animation class shares its
            // tag) among the variants given
            (image, animationClass) = staticDecorativeObjectType.RandomImageWithAnimationClass;
        }

        InfoText = infoTextOverride ?? staticDecorativeObjectType.InfoText;
        verticalOffset = verticalOffsetOverride ?? staticDecorativeObjectType.VerticalOffset;
        character = staticDecorativeObjectType.Character;
        characterColor = staticDecorativeObjectType.CharacterColor;
        Blocking = staticDecorativeObjectType.Blocking;
        makeCoveringOffsetDecsTransparent =
            staticDecorativeObjectType.MakeCoveringOffsetDecsTransparent;
        this.decorationLayer = decorationLayer; // TODO: Shouldn't this be driven from config data.json as well?
    }

    public override void Render(Map map) =>
        map.Decorations[X, Y]
            .Add(
                // Matches Torch/Tile.RenderLiquid: an animated decoration's keyframes drive
                // background-position themselves, so no separate static spr-<image> class is
                // wanted alongside them.
                new Decoration(this, animationClass is null ? image : null)
                {
                    AnimationClass = animationClass,
                    VerticalOffset = verticalOffset,
                    Character = character,
                    CharacterColor = characterColor,
                    DecorationLayer = decorationLayer,
                    MakeCoveringOffsetDecsTransparent = makeCoveringOffsetDecsTransparent,
                }
            );
}
