namespace BlazorRogue.Tests;

public class UtilityMethodsTests
{
    [Theory]
    [InlineData("goblin", "Goblin")]
    [InlineData("Goblin", "Goblin")]
    [InlineData("g", "G")]
    [InlineData("g1", "G1")]
    [InlineData("ALREADY", "ALREADY")]
    public void FirstLetterToUpperCaseCapitalizesOnlyFirstCharacter(string input, string expected) => Assert.Equal(expected, input.FirstLetterToUpperCase());

    [Fact]
    public void FirstLetterToUpperCaseReturnsEmptyStringForNull()
    {
        string? input = null;
        Assert.Equal(string.Empty, input!.FirstLetterToUpperCase());
    }

    [Fact]
    public void FirstLetterToUpperCaseReturnsEmptyStringForEmpty() => Assert.Equal(string.Empty, "".FirstLetterToUpperCase());
}
