using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

[TestFixture]
public class StyleRendererTests
{
  private Style _fullStyle1 = new Style
  {
    FontSize = 10,
    FrontColor = Colors.Black,
    FontFamily = Style.Family.Light
  };

  private Style _fullStyle2 = new Style
  {
    FontSize = 14,
    FrontColor = Colors.Blue,
    FontFamily = Style.Family.Heavy
  };
  
  private Style _partialStyle1 = new Style
  {
    FontSize = 14,
    FrontColor = Colors.Blue
  };

  private Style _partialStyle2 = new Style
  {
    FontFamily = Style.Family.Heavy
  };

  private Style _nullStyle = default;


  [Test]
  public void NullStylesAreIgnored()
  {
    Assert.That(_nullStyle.Fallback(_nullStyle), Is.EqualTo(_nullStyle));
    Assert.That(_nullStyle.Fallback(_fullStyle1), Is.EqualTo(_fullStyle1));
    Assert.That(_fullStyle1.Fallback(_nullStyle), Is.EqualTo(_fullStyle1));
  }
  
  [Test]
  public void FallbackStyles()
  {
    Assert.That(_fullStyle1.Fallback(_partialStyle1), Is.EqualTo(_fullStyle1));
    Assert.That(_partialStyle1.Fallback(_fullStyle1), Is.EqualTo(new Style
    {
      FontSize = 14,
      FrontColor = Colors.Blue,
      FontFamily = Style.Family.Light
    }));
    Assert.That(_partialStyle2.Fallback(_partialStyle1), Is.EqualTo(_fullStyle2));
    Assert.That(_partialStyle1.Fallback(_partialStyle2), Is.EqualTo(_fullStyle2));
  }

  [Test]
  public void OverrideStyles()
  {
    Assert.That(_fullStyle2.Override(family:Style.Family.Light), Is.EqualTo(new Style
    {
      FontSize = 14,
      FrontColor = Colors.Blue,
      FontFamily = Style.Family.Light
    }));
    Assert.That(_fullStyle2.Override(size:10, frontColor:Colors.Black), Is.EqualTo(new Style
    {
      FontSize = 10,
      FrontColor = Colors.Black,
      FontFamily = Style.Family.Heavy
    }));
  }
  
  [Test]
  public void RenderStyles()
  {
    var code = new SourceCodeGenerator();
    _fullStyle1.Render(code);
    Assert.That(code.Result, Is.EqualTo(
      string.Join("\n", new [] {"font.pixelSize: 10", "color: \"#000000\"", "font.family: UiConst.fontLt", "font.weight: Font.Light"})+"\n"
    ));

    code = new SourceCodeGenerator();
    _partialStyle1.Render(code);
    Assert.That(code.Result, Is.EqualTo(
      string.Join("\n", new [] {"font.pixelSize: 14", "color: \"#71B8DB\""})+"\n"
    ));

    code = new SourceCodeGenerator();
    _partialStyle2.Render(code);
    Assert.That(code.Result, Is.EqualTo(
      string.Join("\n", new [] {"font.family: UiConst.fontHv", "font.weight: Font.Black"})+"\n"
    ));
  }
}