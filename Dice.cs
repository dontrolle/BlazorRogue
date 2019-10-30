using System;
using System.Linq;

namespace BlazorRogue
{
    public static class Dice
    {
        static Random random = new Random();

        public static int RollD100()
        {
            return random.Next(1, 101);
        }

        public static int ReverseD100(int d100Roll)
        {
            CheckValidD100Value(d100Roll);

            // handle 3-digit 
            if (d100Roll == 100)
                return 100;

            (int t, int o) = GetD100Digits(d100Roll);
            return int.Parse($"{o}{t}");
        }

        /// <summary>
        /// Returns a tuple of two ints, the tens and the remainder.
        /// </summary>
        /// <param name="d100Roll">A valid d100 dice roll, between 1 and 100.</param>
        /// <returns>A tuple of the tes: A number between 0 and 10, and the remainder: A number between 0 and 9.</returns>
        public static Tuple<int,int> GetD100Digits(int d100Roll)
        {
            CheckValidD100Value(d100Roll);

            var t = d100Roll / 10;
            return Tuple.Create(t, d100Roll - t * 10);
        }

        private static void CheckValidD100Value(int value)
        {
            if (value < 1 || value > 100)
            {
                throw new ArgumentException("Expected valid d100 value between 1 and 100, not " + value);
            }
        }

        public static int GetSuccessLevel(int d100Roll, int skillLevel)
        {
            CheckValidD100Value(d100Roll);

            var dt = d100Roll / 10;
            var st = skillLevel / 10;

            return st - dt;
        }
    }
}
