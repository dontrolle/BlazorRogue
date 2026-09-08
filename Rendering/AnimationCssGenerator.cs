using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BlazorRogue.Entities;

namespace BlazorRogue.Rendering;

/// <summary>
/// Generates CSS <c>@keyframes</c> sprite animations from parsed game data, so the JSON stays the
/// single source of truth instead of a hand-maintained block per entry in a static stylesheet.
/// Covers heroes/monsters (4 frames, looping every 1.5s) and liquid pools (frames per-liquid,
/// per-liquid duration). Sprite positions come from <see cref="SpriteAtlas"/>; this class only
/// knows animation timing and which sprite keys belong to which cycle.
/// </summary>
static class AnimationCssGenerator
{
    const string MoveableAnimationClassPrefix = "animated_";
    const int MoveableFrameCount = 4;
    const string MoveableAnimationDuration = "1.5s";

    public static string Generate(IEnumerable<MoveableType> moveableTypes, SpriteAtlas atlas)
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
            // All 4 frames live inside one atlas entry with an embedded frame stride (see
            // HeroBlock in tools/AtlasPacker) - so every step of the cycle looks up the same
            // sprite key, just at a different frame index.
            AppendKeyframes(
                css,
                spriteName,
                MoveableFrameCount,
                frameNumber => atlas.CssDeclarationsFor(spriteName, frameNumber - 1)
            );
            AppendAnimationClass(
                css,
                MoveableAnimationClassPrefix + spriteName,
                spriteName,
                MoveableAnimationDuration
            );
        }

        return css.ToString();
    }

    public static string Generate(IEnumerable<LiquidType> liquidTypes, SpriteAtlas atlas)
    {
        StringBuilder css = new();

        foreach (var liquid in liquidTypes.DistinctBy(l => l.SpriteName))
        {
            string keyframesName = liquid.AnimationClass;

            // Unlike moveables above, each liquid frame was matched as its own independent file
            // (e.g. "water_blue_1", "water_blue_2", ...), so it's a separate top-level atlas
            // entry rather than one entry with a frame stride.
            AppendKeyframes(
                css,
                keyframesName,
                liquid.FrameCount,
                frameNumber => atlas.CssDeclarationsFor($"{liquid.SpriteName}_{frameNumber}")
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

    /// <summary>
    /// Emits one <c>@keyframes</c> block of <paramref name="frameCount"/> equal-width steps,
    /// looping back to frame 1 at 100% (so it plays 1, 2, ..., N, 1 across the cycle). A step whose
    /// sprite isn't in the atlas is skipped rather than emitted empty.
    /// </summary>
    static void AppendKeyframes(
        StringBuilder css,
        string keyframesName,
        int frameCount,
        System.Func<int, string?> declarationsForFrame
    )
    {
        _ = css.Append("@keyframes ").Append(keyframesName).Append(" {\n");

        for (int step = 0; step <= frameCount; step++)
        {
            int percent = step * 100 / frameCount;
            int frameNumber = step == frameCount ? 1 : step + 1;
            string? declarations = declarationsForFrame(frameNumber);
            if (declarations is null)
            {
                continue;
            }

            _ = css.Append(CultureInfo.InvariantCulture, $"  {percent}% {{ {declarations} }}\n");
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
                // Each keyframe sets background-position, a numeric/interpolable value - without
                // this, the browser's default "ease" timing smoothly slides the crop window across
                // the sheet between frames instead of snapping, which looks like the sprite is
                // scrolling. steps(1) makes every keyframe-to-keyframe segment a single discrete
                // jump instead (this used to be a non-issue when each keyframe set background-image
                // directly - different image URLs were never interpolable in the first place).
                + "  animation-timing-function: steps(1);\n"
                + "}\n"
        );
}
