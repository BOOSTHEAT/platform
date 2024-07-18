using System.Threading.Tasks;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.QmlTests;

public class MiscTests
{
  [TestCase("reference.qml")]
  [TestCase("swipe.qml")]
  [TestCase("runtimeEvents.qml")]
  [TestCase("cache.qml")]
  public async Task Misc(string filename)
  {
    await Harness.RunTest(filename);
  }

}