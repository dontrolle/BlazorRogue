namespace BlazorRogue.Tests
{
  public class UtilityMethodsTests
  {
    [Theory]
    [InlineData("goblin", "Goblin")]
    [InlineData("Goblin", "Goblin")]
    [InlineData("g", "G")]
    [InlineData("ALREADY", "ALREADY")]
    public void FirstLetterToUpperCase_CapitalizesOnlyFirstCharacter(string input, string expected)
    {
      Assert.Equal(expected, input.FirstLetterToUpperCase());
    }

    [Fact]
    public void FirstLetterToUpperCase_ReturnsEmptyStringForNull()
    {
      string? input = null;
      Assert.Equal(string.Empty, input!.FirstLetterToUpperCase());
    }

    [Fact]
    public void FirstLetterToUpperCase_ReturnsEmptyStringForEmpty()
    {
      Assert.Equal(string.Empty, "".FirstLetterToUpperCase());
    }
  }
}
