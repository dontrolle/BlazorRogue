using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BlazorRogue.Entities;

namespace BlazorRogue.Rendering;

/// <summary>
/// Generates CSS <c>@keyframes</c> sprite animations from parsed game data, so the JSON stays the
/// single source of truth instead of a hand-maintained block per entry in a static stylesheet.
/// Covers heroes/monsters (4 frames under <c>img/uf_heroes/</c>, looping every 1.5s) and liquid
/// pools (frames under <c>img/uf_terrain/</c>, per-liquid duration).
/// </summary>
static class AnimationCssGenerator
{
    const string MoveableAnimationClassPrefix = "animated_";
    const string MoveableImageFolder = "img/uf_heroes";
    const int MoveableFrameCount = 4;
    const string MoveableAnimationDuration = "1.5s";

    const string LiquidImageFolder = "img/uf_terrain";

    public static string Generate(IEnumerable<MoveableType> moveableTypes)
    {
        StringBuilder css = new();

        var spriteNames = moveableTypes
            .Select(moveableType => moveableType.AnimationClass)
            .Where(animationClass =>
                animationClass.StartsWith(
                    MoveableAnimationClassPrefix,
                    System.StringComparison.Ordinal
                )
            )
            .Select(animationClass => animationClass[MoveableAnimationClassPrefix.Length..])
            .Distinct();

        foreach (string spriteName in spriteNames)
        {
            AppendKeyframes(css, spriteName, spriteName, MoveableImageFolder, MoveableFrameCount);
            AppendAnimationClass(
                css,
                MoveableAnimationClassPrefix + spriteName,
                spriteName,
                MoveableAnimationDuration
            );
        }

        return css.ToString();
    }

    public static string Generate(IEnumerable<LiquidType> liquidTypes)
    {
        StringBuilder css = new();

        foreach (var liquid in liquidTypes.DistinctBy(l => l.SpriteName))
        {
            string keyframesName = liquid.AnimationClass;
            AppendKeyframes(
                css,
                keyframesName,
                liquid.SpriteName,
                LiquidImageFolder,
                liquid.FrameCount
            );
            AppendAnimationClass(
                css,
                liquid.AnimationClass,
                keyframesName,
                liquid.AnimationDurationSeconds.ToString("0.0#", CultureInfo.InvariantCulture) + "s"
            );
        }

        return css.ToString();
    }

    static void AppendKeyframes(
        StringBuilder css,
        string keyframesName,
        string imageFileStem,
        string imageFolder,
        int frameCount
    )
    {
        _ = css.Append("@keyframes ").Append(keyframesName).Append(" {\n");

        for (int frame = 0; frame <= frameCount; frame++)
        {
            int percent = frame * 100 / frameCount;
            // The pattern loops back to frame 1 at 100%, so it plays 1, 2, ..., N, 1 across the cycle.
            int frameNumber = frame == frameCount ? 1 : frame + 1;

            _ = css.Append(
                CultureInfo.InvariantCulture,
                $"  {percent}% {{ background-image: url('../{imageFolder}/{imageFileStem}_{frameNumber}.png'); }}\n"
            );
        }

        _ = css.Append("}\n");
    }

    static void AppendAnimationClass(
        StringBuilder css,
        string className,
        string keyframesName,
        string duration
    ) =>
        css.Append(
            $".{className} {{\n"
                + $"  animation-name: {keyframesName};\n"
                + $"  animation-duration: {duration};\n"
                + "  animation-iteration-count: infinite;\n"
                + "}\n"
        );
}
