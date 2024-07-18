using System;
using System.IO;

namespace ImpliciX.Linker.Values;

public class SourceAndTarget
{
  public SourceAndTarget(string definition)
  {
    var items = definition.Split(',',StringSplitOptions.TrimEntries);
    if (items.Length != 2)
      throw new Exception("Unexpected source and target");
    Source = new FileInfo(items[0]);
    Target = new FileInfo(items[1]);
  }

  public FileInfo Source { get; }
  public FileInfo Target { get; }

  public static bool IsInvalid(string definition)
  {
    try
    {
      var x = new SourceAndTarget(definition);
      return false;
    }
    catch
    {
      return true;
    }
  }
}