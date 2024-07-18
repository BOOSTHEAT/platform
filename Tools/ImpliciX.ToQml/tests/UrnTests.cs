using ImpliciX.Language.GUI;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class UrnTests
{
  [TestCaseSource(nameof(_data))]
  public void GuiNodeAsString(GuiNode guiNode, string expected)
  {
    Assert.That(guiNode.Urn.Value, Is.EqualTo(expected));
  }
  
  private static TestCaseData[] _data = new[]
  {
    new TestCaseData(root.screen1, "root:screen1"),
    new TestCaseData(root.screen2, "root:screen2"),
  };
}