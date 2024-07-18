using System;
using System.IO;

namespace ImpliciX.Linker.Values;

public class PartReference
{
  public PartReference(string definition)
  {
    var items = definition.Split(',',StringSplitOptions.TrimEntries);
    if (items.Length < 3 || items.Length > 4)
      throw new Exception("Unexpected part reference");
    Id = items[0];
    Version = items[1];
    Path = new FileInfo(items[2]);
    Category = items.Length == 4 ? items[3] : "APPS";
  }

  public string Id { get; }
  public string Version { get; }
  public string Category { get; }
  public FileInfo Path { get; }

  public static bool IsInvalid(string definition)
  {
    try
    {
      var pr = new PartReference(definition);
      return OptionsExtensions.VersionNumberIsInvalid(pr.Version) || !pr.Path.Exists;
    }
    catch
    {
      return true;
    }
  }
}