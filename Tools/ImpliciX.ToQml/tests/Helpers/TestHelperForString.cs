using System.Text.RegularExpressions;

namespace ImpliciX.ToQml.Tests.Helpers;

internal static class TestHelperForString
{
  public static string RemoveEmptyLines(string text)
  {
    return Regex.Replace(text, @"^\s*$\n|\r", string.Empty, RegexOptions.Multiline).TrimEnd();
  }
}