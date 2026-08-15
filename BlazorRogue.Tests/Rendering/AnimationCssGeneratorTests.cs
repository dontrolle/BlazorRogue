using System.Collections.Generic;
using BlazorRogue.Entities;
using BlazorRogue.Rendering;

namespace BlazorRogue.Tests.Rendering;

public class AnimationCssGeneratorTests
{
    static MoveableType MakeMoveableType(string id, string animationClass) =>
        new(
            id,
            id,
            animationClass,
            asciiCharacter: "@",
            asciiColour: "white",
            weaponSkill: 1,
            weaponDamage: 1,
            toughness: 1,
            armour: 0,
            wounds: 1,
            aiComponentId: "",
            aiComponentSettings: new SettingsMap(new Dictionary<string, object>())
        );

    [Fact]
    public void GenerateEmitsKeyframesForAllFourFramesLoopingBackToFrameOne()
    {
        var css = AnimationCssGenerator.Generate([MakeMoveableType("templar", "animated_templar")]);

        Assert.Contains("@keyframes templar {", css);
        Assert.Contains("0% { background-image: url('../img/uf_heroes/templar_1.png'); }", css);
        Assert.Contains("25% { background-image: url('../img/uf_heroes/templar_2.png'); }", css);
        Assert.Contains("50% { background-image: url('../img/uf_heroes/templar_3.png'); }", css);
        Assert.Contains("75% { background-image: url('../img/uf_heroes/templar_4.png'); }", css);
        Assert.Contains("100% { background-image: url('../img/uf_heroes/templar_1.png'); }", css);
    }

    [Fact]
    public void GenerateEmitsAnimationClassRuleMatchingTheOriginalHandAuthoredPattern()
    {
        var css = AnimationCssGenerator.Generate([
            MakeMoveableType("goblinWarrior", "animated_goblin_warrior"),
        ]);

        Assert.Contains(
            ".animated_goblin_warrior {\n"
                + "  animation-name: goblin_warrior;\n"
                + "  animation-duration: 1.5s;\n"
                + "  animation-iteration-count: infinite;\n"
                + "}",
            css
        );
    }

    [Fact]
    public void GenerateDeduplicatesRepeatedAnimationClassesAcrossHeroesAndMonsters()
    {
        var css = AnimationCssGenerator.Generate([
            MakeMoveableType("templar", "animated_templar"),
            MakeMoveableType("otherTemplar", "animated_templar"),
        ]);

        Assert.Equal(1, CountOccurrences(css, "@keyframes templar {"));
    }

    static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
