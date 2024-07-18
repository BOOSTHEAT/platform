using System;
using System.Collections.Generic;
using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Renderers.Widgets.TimeCharts;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

[TestFixture]
public class StackedTimeBarsRendererTests : Screens
{
  [Test]
  public void GivenStackedTimeBars_WhenIRender_ThenIGetCodeGenerateExpected()
  {
    const string rootUrn = "toto";

    var chart = Chart.StackedTimeBars(
      Of(Urn.BuildUrn(rootUrn, "MyTimeSeries_1")).Fill(Color.Crimson),
      Of(Urn.BuildUrn(rootUrn, "MyTimeSeries_2")).Fill(Color.Blue)
    ).Width(600).Height(400);

    var context = new WidgetRenderingContext
    {
      Widget = (StackedTimeBarsWidget) chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(TimeSeriesFeed)] = new TimeSeriesRenderer()};
    new StackedTimeBarsRenderer(feedRenderers).Render(context);

    const string expected = $@"ChartView {{
  onSeriesAdded: {{
    if(series.xAxisType == 0) {{
      series.axisX = xMainAxis;
    }}
    else {{
      series.axisX = xSecondaryAxis;
    }}
    if(series.yAxisPosition == 0) {{
      series.axisY = yLeftAxis;
    }}
    else {{
      series.axisYRight = yRightAxis;
    }}
  }}
  width: 600
  height: 400
  legend.visible: false
  antialiasing: true
  property var xMainAxis: BarCategoryAxis {{
    categories: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_1.getFormattedTimeline(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_1.determineTimelineFormat())
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var yLeftAxis: ValueAxis {{
    min: Math.min(0, Math.min(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_1.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_2.min))
    max: Math.max(0, [root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_1.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_2.max].reduce((a,b)=>a+b, 0))
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  StackedBarSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    BarSet {{
      label: ""toto:MyTimeSeries_1""
      values: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_1.values
      color: ""#DC143C""
    }}
    BarSet {{
      label: ""toto:MyTimeSeries_2""
      values: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$MyTimeSeries_2.values
      color: ""#0000FF""
    }}
  }}
}}
";

    Check.That(context.Code.Result).IsEqualTo(expected);
  }
}