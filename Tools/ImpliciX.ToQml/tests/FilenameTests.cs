using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class FilenameTests
{
  [TestCaseSource(nameof(_data))]
  public void ResourceToFilePath(string resourceName, string[] expected)
  {
    Assert.That(ResourceManager.GetFilePath("BOOSTHEAT.Device.ToQml.Qml", resourceName), Is.EqualTo(expected));
  }
  
  private static TestCaseData[] _data = new[]
  {
    new TestCaseData("BOOSTHEAT.Device.ToQml.Qml.main.qml", new [] { "main.qml" }),
    new TestCaseData("BOOSTHEAT.Device.ToQml.Qml.JsUtils.qmldir", new [] { "JsUtils", "qmldir" }),
    new TestCaseData("BOOSTHEAT.Device.ToQml.Qml.JsUtils.Logger.js", new [] { "JsUtils", "Logger.js" }),
  };
}