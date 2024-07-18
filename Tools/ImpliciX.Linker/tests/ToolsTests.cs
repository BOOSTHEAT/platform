using System.IO;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class ToolsTests
{
  [Test]
  public void TempFolder()
  {
    var tmp = Tools.GetTempFolder("foo", "bar");
    Assert.That(Path.IsPathRooted(tmp.FullName));
    Assert.That(tmp.FullName, Does.EndWith(Path.Combine("ImpliciX.Linker","foo","bar")));
  }
}