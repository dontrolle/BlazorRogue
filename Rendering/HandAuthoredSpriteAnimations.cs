using System.Globalization;
using System.Text;

namespace BlazorRogue.Rendering;

/// <summary>
/// The handful of sprite animations that used to be hand-written directly into
/// <c>wwwroot/css/animations.css</c> (torches, the melee hit-flash effect) - not data-driven by
/// any <c>Data/*.json</c> file, so <see cref="AnimationCssGenerator"/> has no entity list to work
/// from. Moved here because their frame coordinates are only knowable once the atlas exists; the
/// keyframe percentages/durations/iteration-counts are preserved exactly as they were
/// originally hand-written.
/// </summary>
static class HandAuthoredSpriteAnimations
{
    sealed record Definition(
        string AnimationClass,
        string KeyframesName,
        string Duration,
        string IterationCount,
        (int Percent, string SpriteKey)[] Frames
    );

    static readonly Definition[] Definitions =
    [
        new(
            "animated_torch_floor",
            "torch_floor",
            "1s",
            "infinite",
            [(0, "torch1_floor"), (50, "torch2_floor"), (100, "torch1_floor")]
        ),
        new(
            "animated_torch",
            "torch",
            "1s",
            "infinite",
            [(0, "torch1"), (50, "torch2"), (100, "torch1")]
        ),
        new(
            // Single-shot (not infinite) - a flash that plays once and freezes on its last frame.
            "animated_hit_fx1",
            "hit_fx1",
            "2s",
            "1",
            [(0, "uf_FX_impact_09"), (33, "uf_FX_impact_08"), (66, "uf_FX_impact_07")]
        ),
        new(
            "animated_lilypad_a",
            "lilypad_a",
            "2.4s",
            "infinite",
            [(0, "lilypad_a1"), (50, "lilypad_a2"), (100, "lilypad_a1")]
        ),
        new(
            "animated_lilypad_b",
            "lilypad_b",
            "2.4s",
            "infinite",
            [(0, "lilypad_b1"), (50, "lilypad_b2"), (100, "lilypad_b1")]
        ),
        // Wall and floor share duration/iteration-count/keyframe percentages so they stay
        // phase-locked as one fountain rather than drifting out of sync with each other.
        new(
            "animated_fountain_wall",
            "fountain_wall",
            "1.2s",
            "infinite",
            [(0, "fountain_a1"), (50, "fountain_b1"), (100, "fountain_a1")]
        ),
        new(
            "animated_fountain_floor",
            "fountain_floor",
            "1.2s",
            "infinite",
            [(0, "fountaina2"), (50, "fountainb2"), (100, "fountaina2")]
        ),
    ];

    public static string Generate(SpriteAtlas atlas)
    {
        if (!atlas.IsAvailable)
        {
            return "";
        }

        StringBuilder css = new();
        foreach (var definition in Definitions)
        {
            _ = css.Append("@keyframes ").Append(definition.KeyframesName).Append(" {\n");
            foreach (var (percent, spriteKey) in definition.Frames)
            {
                string? declarations = atlas.CssDeclarationsFor(spriteKey);
                if (declarations is null)
                {
                    continue;
                }

                _ = css.Append(
                    CultureInfo.InvariantCulture,
                    $"  {percent}% {{ {declarations} }}\n"
                );
            }
            _ = css.Append("}\n");

            _ = css.Append(
                $".{definition.AnimationClass} {{\n"
                    + $"  animation-name: {definition.KeyframesName};\n"
                    + $"  animation-duration: {definition.Duration};\n"
                    + $"  animation-iteration-count: {definition.IterationCount};\n"
                    // See AnimationCssGenerator's AppendAnimationClass for why this is needed -
                    // without it, background-position (numeric/interpolable) eases between frames
                    // instead of snapping, which looks like the sprite is scrolling.
                    + "  animation-timing-function: steps(1);\n"
                    + "}\n"
            );
        }

        return css.ToString();
    }
}
