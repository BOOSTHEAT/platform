using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class TextRendererTests : Screens
{
  [TestCaseSource(nameof(Cases))]
  public void TestCases(Block block, string expectedQml)
  {
    var blockWidget = At.Left(50).Top(30).Put(block).CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };
    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(PropertyFeed)] = new PropertyRenderer()};

    new TextBoxRenderer(feedRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(expectedQml, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }

  public static object[] Cases =
  {
    new object[]
    {
      Input(PropertyUrn<Literal>.Build("toto", "titi")), 
"""
TextBox {
  x: 50
  y: 30
  runtime: root.runtime
  urn: 'toto:titi'
  text: root.runtime.cache.toto$titi.value
}

"""
    },
    new object[]
    {
      Input(PropertyUrn<Literal>.Build("toto", "titi")).Width(400).With(Font.ExtraBold.Size(22).Color(Color.Blue)), 
"""
TextBox {
  x: 50
  y: 30
  runtime: root.runtime
  urn: 'toto:titi'
  text: root.runtime.cache.toto$titi.value
  width: 400
  font.pixelSize: 22
  color: "#0000FF"
  font.family: UiConst.fontPnEb
  font.weight: Font.Black
}

"""
    },
  };
}