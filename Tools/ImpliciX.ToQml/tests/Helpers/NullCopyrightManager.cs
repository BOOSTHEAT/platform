using System.IO;

namespace ImpliciX.ToQml.Tests.Helpers;

public class NullCopyrightManager : ICopyrightManager
{
  public void AddCopyright(Stream stream, string filename)
  {
  }

  public string AddCopyright(string content, string filename) => content;
}