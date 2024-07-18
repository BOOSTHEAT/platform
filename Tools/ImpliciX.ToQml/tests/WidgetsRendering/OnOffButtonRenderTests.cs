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

public class OnOffButtonRenderTests : Screens
{
  public record OnOffCodeContent(string xPosition, string yPosition);

  private static object[] _widgetPositionCases =
  {
    new object[] {OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget(), new OnOffCodeContent("0", "0")},
    new object[] {OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget().SetPositions(left: 10), new OnOffCodeContent("10", "0")},
    new object[] {OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget().SetPositions(left: 10, top: 20), new OnOffCodeContent("10", "20")},
    new object[] {OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget().SetPositions(left: 10, bottom: 40), new OnOffCodeContent("10", "parent.height - height - 40")},
    new object[] {OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget().SetPositions(right: 30), new OnOffCodeContent("parent.width - width - 30", "0")},
    new object[] {OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget().SetPositions(right: 30, top: 20), new OnOffCodeContent("parent.width - width - 30", "20")},
    new object[]
    {
      OnOff(UserSettingUrn<float>.Build("myToggleValue")).CreateWidget().SetPositions(right: 30, bottom: 40),
      new OnOffCodeContent("parent.width - width - 30", "parent.height - height - 40")
    },
  };

  [TestCaseSource(nameof(_widgetPositionCases))]
  public void GivenOnOffButton_WhenIRender_ThenIGetCodeExpected(OnOffButtonWidget widget, OnOffCodeContent codeExpected)
  {
    var targetUrn = ((PropertyFeed) widget.Value).Urn;
    var context = new WidgetRenderingContext
    {
      Widget = widget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    // When
    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(PropertyFeed)] = new PropertyRenderer()};
    new OnOffButtonRender(feedRenderers).Render(context);

    // Then
    Check.That(context.Code.Result).IsEqualTo(@"OnOffButton {" + $@"
  x: {codeExpected.xPosition}
  y: {codeExpected.yPosition}" + @"
  title:""""
  checked: root.runtime.cache.myToggleValue.value != ""0""
    onToggled: {
      checked" + $@"
      ? root.runtime.api.sendProperty(""{targetUrn}"", ""{OnOffButtonRender.ValueToSendToTurnOn}"")
      : root.runtime.api.sendProperty(""{targetUrn}"", ""{OnOffButtonRender.ValueToSendToTurnOff}"")" + @"
    }
  }
", StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.IgnoreSymbols));
  }
}

internal static class WidgetExtensions
{
  public static Widget SetPositions(this Widget widget, int? left = null, int? top = null, int? right = null, int? bottom = null)
  {
    widget.Left = left;
    widget.Top = top;
    widget.Right = right;
    widget.Bottom = bottom;
    return widget;
  }
}