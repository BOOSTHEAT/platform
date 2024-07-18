using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Widgets;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

[TestFixture]
public class ImageRenderingTests
{
  [TestCase("meteo.png",121,95)]
  [TestCase("shower_eco_running.gif",62,62)]
  public void GetImageSize(string filename, int expectedWidth, int expectedHeight)
  {
    var image = new ImageWidget
    {
      ReferencePath = filename
    };
    Assert.That(ImageRenderer.GetSize("referenceImages", image), Is.EqualTo((expectedWidth,expectedHeight)));
  }
}