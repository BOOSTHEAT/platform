using System;
using System.Collections.Generic;
using System.Globalization;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class SendRendererTests : Screens
{
  [Test]
  public void NominalCase()
  {
    var commandUrn = CommandUrn<NoArg>.Build("root", "my_command");
    var blockWidget = At.Left(50).Top(30).Put(
        Label("MyLabelText")
          .Send(commandUrn)
      )
      .CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(Const)] = new ConstRenderer()};
    var widgetRenderers = new Dictionary<Type, IRenderWidget> {[typeof(Text)] = new TextRenderer(feedRenderers)};
    new SendRenderer(widgetRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(
      """
       ClickableContainer {
        width:visual.width
        height:visual.height
        anchors {
          left: parent.left
          leftMargin: 50
          top: parent.top
          topMargin: 30
        }
        visual: Text {
          text: "MyLabelText"
        }
        onClicked: {
          root.runtime.api.sendCommand("root:my_command");
          if(typeof parent.clicked === 'function') parent.clicked();
        }
      }

      """, StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }
}