using System;
using ImpliciX.Language.Model;
using TimeZone = ImpliciX.Language.Model.TimeZone;

namespace ImpliciX.ToQml.Renderers;

public static class I18NRenderer
{
  public static SourceCodeGenerator CreateTranslationDictionary(
    this SourceCodeGenerator code,
    Translations translation)
  {
    if (translation.Entries.Count == 0)
      return code;
    foreach (var language in translation.Languages)
    {
      code.Open($"'{language}':");
      foreach (var entry in translation.Entries)
        code.Append($"'{entry.Key}': \"{entry.Value[language].Replace("\n","\\n")}\",");
      code.Close("},");
    }
    return code;
  }

  public static SourceCodeGenerator CreateLocaleList(this SourceCodeGenerator code)
  {
    code.Open("function localeList()");
    code.Append("return [");

    foreach (var locale in Enum.GetValues(typeof(Locale)))
    {
      code.Append($"'{locale}', ");
    }
      
    code.Append("];");
    code.Close();

    return code;
  }
    
  public static SourceCodeGenerator CreateTimezoneList(this SourceCodeGenerator code)
  {
    code.Open("function timezoneList()");
    code.Append("return [");

    foreach (var locale in Enum.GetValues(typeof(TimeZone)))
    {
      code.Append($"'{locale}', ");
    }
      
    code.Append("];");
    code.Close();

    return code;
  }
}