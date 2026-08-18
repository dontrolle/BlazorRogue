using System;

namespace BlazorRogue;

static class UtilityMethods
{
    /// <summary>
    /// Returns a string equal to the input string with the first character converted to uppercase, or string.Empty if null or the empty string is passed.
    /// </summary>
    public static string FirstLetterToUpperCase(this string s) =>
        string.IsNullOrEmpty(s)
            ? string.Empty
            : string.Concat(
                s[..1].ToUpper(System.Globalization.CultureInfo.CurrentCulture),
                s[1..].AsSpan()
            );
}
