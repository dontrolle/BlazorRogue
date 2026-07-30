using System;

namespace BlazorRogue.Combat;

static class Dice
{
    static readonly Random Random = new();

    public static int RollD100() => Random.Next(1, 101);

    public static int ReverseD100(int d100Roll)
    {
        CheckValidD100Value(d100Roll);

        // handle 3-digit 
        if (d100Roll == 100)
            return 100;

        (int t, int o) = GetD100Digits(d100Roll);
        return (o*10)+t;
    }

    /// <summary>
    /// Returns a tuple of two ints, the tens and the remainder.
    /// </summary>
    /// <param name="d100Roll">A valid d100 dice roll, between 1 and 100.</param>
    /// <returns>A tuple of the tes: A number between 0 and 10, and the remainder: A number between 0 and 9.</returns>
    public static Tuple<int, int> GetD100Digits(int d100Roll)
    {
        CheckValidD100Value(d100Roll);

        int t = d100Roll / 10;
        return Tuple.Create(t, d100Roll - (t * 10));
    }

    static void CheckValidD100Value(int value)
    {
        if (value is < 1 or > 100)
        {
            throw new ArgumentException("Expected valid d100 value between 1 and 100, not " + value);
        }
    }

    public static int GetSuccessLevel(int d100Roll, int skillLevel)
    {
        CheckValidD100Value(d100Roll);

        int dt = d100Roll / 10;
        int st = skillLevel / 10;

        return st - dt;
    }
}
