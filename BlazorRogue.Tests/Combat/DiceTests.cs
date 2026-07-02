using System;
using BlazorRogue.Combat;

namespace BlazorRogue.Tests.Combat
{
  public class DiceTests
  {
    [Fact]
    public void RollD100_IsAlwaysWithinValidRange()
    {
      for (int i = 0; i < 1000; i++)
      {
        var roll = Dice.RollD100();
        Assert.InRange(roll, 1, 100);
      }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void ReverseD100_ThrowsForInvalidValues(int invalidRoll)
    {
      Assert.Throws<ArgumentException>(() => Dice.ReverseD100(invalidRoll));
    }

    [Theory]
    [InlineData(100, 100)]
    [InlineData(1, 10)]
    [InlineData(45, 54)]
    [InlineData(10, 1)]
    [InlineData(99, 99)]
    public void ReverseD100_SwapsTensAndUnits(int roll, int expected)
    {
      Assert.Equal(expected, Dice.ReverseD100(roll));
    }

    [Theory]
    [InlineData(45, 4, 5)]
    [InlineData(1, 0, 1)]
    [InlineData(100, 10, 0)]
    [InlineData(10, 1, 0)]
    public void GetD100Digits_ReturnsTensAndRemainder(int roll, int expectedTens, int expectedRemainder)
    {
      var digits = Dice.GetD100Digits(roll);
      Assert.Equal(expectedTens, digits.Item1);
      Assert.Equal(expectedRemainder, digits.Item2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GetD100Digits_ThrowsForInvalidValues(int invalidRoll)
    {
      Assert.Throws<ArgumentException>(() => Dice.GetD100Digits(invalidRoll));
    }

    [Theory]
    [InlineData(1, 50, 5)]   // roll's tens digit 0, skill's tens digit 5 => 5 - 0
    [InlineData(50, 50, 0)]  // equal tens digits
    [InlineData(91, 20, -7)] // skill tens 2, roll tens 9 => 2 - 9
    public void GetSuccessLevel_ComparesTensDigitsOfRollAndSkill(int d100Roll, int skillLevel, int expected)
    {
      Assert.Equal(expected, Dice.GetSuccessLevel(d100Roll, skillLevel));
    }

    [Fact]
    public void GetSuccessLevel_ThrowsForInvalidRoll()
    {
      Assert.Throws<ArgumentException>(() => Dice.GetSuccessLevel(0, 50));
    }
  }
}
