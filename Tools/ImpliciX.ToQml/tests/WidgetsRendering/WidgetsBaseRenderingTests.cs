using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Widgets;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class WidgetsBaseRenderingTests
{
  [TestCase(30,20,10,null,null,15,null,null,false,@"
width: 30
height: 20
anchors {
  left: parent.left
  leftMargin: 10
  top: parent.top
  topMargin: 15
}
  "),
   TestCase(30,20,null,10,null,null,15,null,false,@"
width: 30
height: 20
anchors {
  right: parent.right
  rightMargin: 10
  bottom: parent.bottom
  bottomMargin: 15
}
  "),
   TestCase(30,20,null,null,10,null,null,15,false,@"
width: 30
height: 20
anchors {
  horizontalCenter: parent.horizontalCenter
  horizontalCenterOffset: 10
  verticalCenter: parent.verticalCenter
  verticalCenterOffset: 15
}
  "),
   TestCase(400,250,10,null,null,15,null,null,true,@"
width: 400
height: 250
anchors {
  left: parent.left
  leftMargin: 10
  top: parent.top
  topMargin: 15
}
  "),
   TestCase(null,null,10,null,null,15,null,null,true,@"
anchors {
  left: parent.left
  leftMargin: 10
  top: parent.top
  topMargin: 15
  right: parent.right
  rightMargin: 0
  bottom: parent.bottom
  bottomMargin: 0
}
  "),
   TestCase(400,250,null,null,null,null,null,null,true,@"
width: 400
height: 250
  "),
   TestCase(null,null,null,null,null,null,null,null,true,@"
anchors {
  left: parent.left
  leftMargin: 0
  top: parent.top
  topMargin: 0
  right: parent.right
  rightMargin: 0
  bottom: parent.bottom
  bottomMargin: 0
}
  "),
   TestCase(400,null,null,null,null,null,null,null,true,@"
width: 400
anchors {
  top: parent.top
  topMargin: 0
  bottom: parent.bottom
  bottomMargin: 0
}
  "),
   TestCase(null,250,null,null,null,null,null,null,true,@"
height: 250
anchors {
  left: parent.left
  leftMargin: 0
  right: parent.right
  rightMargin: 0
}
  ")]
  public void SizeAndAnchors(int? width, int? height, int? xStart, int? xEnd, int? xCenter, int? yStart, int? yEnd, int? yCenter, bool useParentSizeIfNeeded, string expectedPosition)
  {
    var w = new Widget();
    w.X.Size = width;
    w.X.FromStart = xStart;
    w.X.ToEnd = xEnd;
    w.X.CenterOffset = xCenter;
    w.Y.Size = height;
    w.Y.FromStart = yStart;
    w.Y.ToEnd = yEnd;
    w.Y.CenterOffset = yCenter;
    var context = new WidgetRenderingContext();
    context.Widget = w;
    context.Code = new SourceCodeGenerator();
    context.RenderBase(useParentSizeIfNeeded);
    Assert.That(context.Code.Result.Trim(),Is.EqualTo(expectedPosition.Trim()));
  }
}