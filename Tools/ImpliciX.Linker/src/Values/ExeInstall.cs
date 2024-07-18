using System;

namespace ImpliciX.Linker.Values;

public class ExeInstall
{
  public ExeInstall(string definition)
  {
    var items = definition.Split(',',StringSplitOptions.TrimEntries);
    if (items.Length != 2)
      throw new Exception("Unexpected exe install");
    Urn = items[0];
    Path = items[1];
  }

  public string Urn { get; }
  public string Path { get; }

  public static bool IsInvalid(string definition)
  {
    try
    {
      var x = new ExeInstall(definition);
      return false;
    }
    catch
    {
      return true;
    }
  }
}