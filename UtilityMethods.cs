using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorRogue
{
  public static class UtilityMethods
  {
    /// <summary>
    /// Returns a string equal to the input string with the first character converted to uppercase, or string.Empty if null or the empty string is passed.
    /// </summary>
    public static string FirstLetterToUpperCase(this string s)
    {
      if (string.IsNullOrEmpty(s))
        return string.Empty;

      return String.Concat(s[..1].ToUpper(), s[1..].AsSpan());
    }
  }
}
