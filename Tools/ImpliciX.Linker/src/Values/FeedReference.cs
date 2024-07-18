namespace ImpliciX.Linker.Values;

public class FeedReference
{
  public FeedReference(string definition)
  {
    var items = definition.Split(':');
    Name = items[0];
    Version = items.Length > 1 ? items[1] : "*";
  }

  public string Name { get; }
  public string Version { get; }

  public static bool IsInvalid(string definition)
  {
    try
    {
      var fr = new FeedReference(definition);
      return OptionsExtensions.VersionNumberIsInvalid(fr.Version);
    }
    catch
    {
      return true;
    }
  }
}