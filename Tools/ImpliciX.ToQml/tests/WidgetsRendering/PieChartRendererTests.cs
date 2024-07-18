using System;
using System.Collections.Generic;
using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Tests.Helpers;
using NFluent;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class PieChartRendererTests : Screens
{
  public record SliceCodeContent(string FillColorQmlCode, string LabelStyleQmlCode);

  private static object[] _pieChartCases =
  {
    new object[]
    {
      "anchors {\n    left: parent.left\n    leftMargin: 0\n    top: parent.top\n    topMargin: 0\n    right: parent.right\n    rightMargin: 0\n    bottom: parent.bottom\n    bottomMargin: 0\n  }",
      Chart.Pie(Of(MetricUrn.Build("myMetric1")), Of(MetricUrn.Build("myMetric2"))),
      new SliceCodeContent("", DefaultFont),
      new SliceCodeContent("", DefaultFont)
    },
    new object[]
    {
      "anchors {\n    left: parent.left\n    leftMargin: 0\n    top: parent.top\n    topMargin: 0\n    right: parent.right\n    rightMargin: 0\n    bottom: parent.bottom\n    bottomMargin: 0\n  }",
      Chart.Pie(Of(MetricUrn.Build("myMetric1")).Fill(Color.Yellow), Of(MetricUrn.Build("myMetric2")).Fill(Color.Cyan)),
      new SliceCodeContent(@"color: ""#FFFF00""", DefaultFont),
      new SliceCodeContent(@"color: ""#00FFFF""", DefaultFont)
    },
    new object[]
    {
      "anchors {\n    left: parent.left\n    leftMargin: 0\n    top: parent.top\n    topMargin: 0\n    right: parent.right\n    rightMargin: 0\n    bottom: parent.bottom\n    bottomMargin: 0\n  }",
      Chart.Pie(
        Of(MetricUrn.Build("myMetric1"))
          .Fill(Color.Yellow)
          .With(Font.ExtraBold.Size(14).Color(Color.Blue)),
        Of(MetricUrn.Build("myMetric2"))
          .Fill(Color.Cyan)
          .With(Font.ExtraBold.Size(22).Color(Color.Red))
      ),
      new SliceCodeContent(@"color: ""#FFFF00""", @"labelFont:Qt.font({family:UiConst.fontPnEb,pixelSize:14})
      labelColor: ""#0000FF"""),
      new SliceCodeContent(@"color: ""#00FFFF""", @"labelFont:Qt.font({family:UiConst.fontPnEb,pixelSize:22})
      labelColor: ""#FF0000""")
    }
  };

  private const string DefaultFont = "labelFont:Qt.font({family:UiConst.fontBtr,pixelSize:12})";
  
  private static object[] _pieChartHeightWidthCases =
  {
    new object[]
    {
      "anchors {\n    left: parent.left\n    leftMargin: 0\n    top: parent.top\n    topMargin: 0\n    right: parent.right\n    rightMargin: 0\n    bottom: parent.bottom\n    bottomMargin: 0\n  }",
      Chart.Pie(Of(MetricUrn.Build("myMetric1")), Of(MetricUrn.Build("myMetric2"))),
      new SliceCodeContent("", DefaultFont),
      new SliceCodeContent("", DefaultFont)
    },
    new object[]
    {
      "width: 300\n  anchors {\n    top: parent.top\n    topMargin: 0\n    bottom: parent.bottom\n    bottomMargin: 0\n  }",
      Chart.Pie(Of(MetricUrn.Build("myMetric1")), Of(MetricUrn.Build("myMetric2"))).Width(300),
      new SliceCodeContent("", DefaultFont),
      new SliceCodeContent("", DefaultFont)
    },
    new object[]
    {
      "height: 200\n  anchors {\n    left: parent.left\n    leftMargin: 0\n    right: parent.right\n    rightMargin: 0\n  }",
      Chart.Pie(Of(MetricUrn.Build("myMetric1")), Of(MetricUrn.Build("myMetric2"))).Height(200),
      new SliceCodeContent("", DefaultFont),
      new SliceCodeContent("", DefaultFont)
    },
    new object[]
    {
      "width: 300\n  height: 200",
      Chart.Pie(Of(MetricUrn.Build("myMetric1")), Of(MetricUrn.Build("myMetric2"))).Width(300).Height(200),
      new SliceCodeContent("", DefaultFont),
      new SliceCodeContent("", DefaultFont)
    },
  };

  [TestCaseSource(nameof(_pieChartCases))]
  [TestCaseSource(nameof(_pieChartHeightWidthCases))]
  public void GivenPieChart_WhenIRender_ThenIGetCodeGenerateExpected(string sizePart, PieChart pieChart, SliceCodeContent slice1,
    SliceCodeContent slice2)
  {
    var context = new WidgetRenderingContext
    {
      Widget = (PieChartWidget) pieChart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(PropertyFeed)] = new PropertyRenderer()};
    new PieChartRenderer(feedRenderers).Render(context);

    var expected = TestHelperForString.RemoveEmptyLines($@"ChartView {{
  {sizePart}
  legend.visible: false
  antialiasing: true
  PieSeries {{
    holeSize: 0.5
    PieSlice {{
      value: root.runtime.cache.myMetric1.value
      label: ""%1%"".arg((100 * percentage).toFixed(1))
      labelArmLengthFactor: 0
      labelVisible: true
      {slice1.FillColorQmlCode}
      {slice1.LabelStyleQmlCode}
    }}
    PieSlice {{
      value: root.runtime.cache.myMetric2.value
      label: ""%1%"".arg((100 * percentage).toFixed(1))
      labelArmLengthFactor: 0
      labelVisible: true
      {slice2.FillColorQmlCode}
      {slice2.LabelStyleQmlCode}
    }}
  }}
}}") + Environment.NewLine;

    Check.That(context.Code.Result).IsEqualTo(expected);
  }

  [TestCase(null, null)]
  [TestCase(null, 300)]
  [TestCase(200, null)]
  [TestCase(200, 300)]
  public void GivenIWritePieChartWithHeightAndWidth_WhenICreateWidget_ThenWidgetContainsDataExpected(int? height, int? width)
  {
    var pieChart = Chart.Pie(
      Of(MetricUrn.Build("myMetric1")),
      Of(MetricUrn.Build("myMetric2"))
    );

    if (height.HasValue)
      pieChart = pieChart.Height(height.Value);

    if (width.HasValue)
      pieChart = pieChart.Width(width.Value);

    var widget = (PieChartWidget) pieChart.CreateWidget();

    Check.That(widget.Height).IsEqualTo(height);
    Check.That(widget.Width).IsEqualTo(width);
  }
}