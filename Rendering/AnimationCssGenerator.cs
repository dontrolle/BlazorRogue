using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BlazorRogue.Entities;

namespace BlazorRogue.Rendering;

/// <summary>
/// Generates the CSS keyframe animations for heroes and monsters, following the single pattern
/// every one of them uses: 4 frames named <c>{name}_1.png</c>..<c>{name}_4.png</c> under
/// <c>img/uf_heroes/</c>, looping every 1.5s. <c>{name}</c> is <see cref="MoveableType.AnimationClass"/>
/// with its <c>animated_</c> prefix stripped, so heroes.json/monsters.json stay the single source of
/// truth instead of duplicating each entry by hand in animations.css.
/// </summary>
static class AnimationCssGenerator
{
    const string AnimationClassPrefix = "animated_";
    const string ImageFolder = "img/uf_heroes";
    const int FrameCount = 4;
    const string AnimationDuration = "1.5s";

    public static string Generate(IEnumerable<MoveableType> moveableTypes)
    {
        StringBuilder css = new();

        var spriteNames = moveableTypes
            .Select(moveableType => moveableType.AnimationClass)
            .Where(animationClass =>
                animationClass.StartsWith(AnimationClassPrefix, System.StringComparison.Ordinal)
            )
            .Select(animationClass => animationClass[AnimationClassPrefix.Length..])
            .Distinct();

        foreach (string spriteName in spriteNames)
        {
            AppendKeyframes(css, spriteName);
            AppendAnimationClass(css, spriteName);
        }

        return css.ToString();
    }

    static void AppendKeyframes(StringBuilder css, string spriteName)
    {
        _ = css.Append("@keyframes ").Append(spriteName).Append(" {\n");

        for (int frame = 0; frame <= FrameCount; frame++)
        {
            int percent = frame * 100 / FrameCount;
            // The pattern loops back to frame 1 at 100%, so it plays 1, 2, 3, 4, 1 across the cycle.
            int frameNumber = frame == FrameCount ? 1 : frame + 1;

            _ = css.Append(
                CultureInfo.InvariantCulture,
                $"  {percent}% {{ background-image: url('../{ImageFolder}/{spriteName}_{frameNumber}.png'); }}\n"
            );
        }

        _ = css.Append("}\n");
    }

    static void AppendAnimationClass(StringBuilder css, string spriteName) =>
        css.Append(
            $".{AnimationClassPrefix}{spriteName} {{\n"
                + $"  animation-name: {spriteName};\n"
                + $"  animation-duration: {AnimationDuration};\n"
                + "  animation-iteration-count: infinite;\n"
                + "}\n"
        );
}
