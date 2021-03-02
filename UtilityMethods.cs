using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue
{
    public static class UtilityMethods
    {
        /// <summary>
        /// Returns the input string with the first character converted to uppercase, or mutates any nulls passed into string.Empty
        /// </summary>
        public static string FirstLetterToUpperCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }

        public static int Clamp(this int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
