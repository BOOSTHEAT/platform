using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Renderers.Widgets.TimeCharts;
using ImpliciX.ToQml.Tests.Helpers;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class TimeLinesRendererTests : Screens
{
  [Test]
  public void GivenTimeLinesWithOneLineSeries_WhenIRender_ThenIGetCodeGenerateExpected()
  {
    var chart = Chart.TimeLines(Of(MetricUrn.Build("Root", "myMetric1")));
    var context = new WidgetRenderingContext
    {
      Widget = (TimeLinesWidget) chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(TimeSeriesFeed)] = new TimeSeriesRenderer()};
    new TimeLinesRenderer(feedRenderers).Render(context);

    var expected = TestHelperForString.RemoveEmptyLines($@"
function updateLineSeries(lineSeries, timeSeries) {{
  lineSeries.removePoints(0, lineSeries.count);
  for(var i=0; i<timeSeries.values.length; i++) {{
    lineSeries.append(timeSeries.timeline[i], timeSeries.values[i]);
  }}
}}
ChartView {{
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
  anchors {{
    left: parent.left
    leftMargin: 0
    top: parent.top
    topMargin: 0
    right: parent.right
    rightMargin: 0
    bottom: parent.bottom
    bottomMargin: 0
  }}
  legend.visible: false
  antialiasing: true
  property var xMainAxis : DateTimeAxis {{
    min: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.getTimelineMinDate()
    max: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.getTimelineMaxDate()
    format: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.determineTimelineFormat()
    titleText: ""("" + root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.determineTimelineFormat() + "")""
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var yLeftAxis: ValueAxis {{
    min: Js.mathMinOfNotNullValues(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.min)
    max: Math.max(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.max)
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1);
    }}
  }}
}}") + Environment.NewLine;

    Check.That(context.Code.Result).IsEqualTo(expected);
  }

  [Test]
  public void GivenTimeLinesWithTwoLineSeries_WhenIRender_ThenIGetCodeGenerateExpected()
  {
    var chart = Chart.TimeLines(Of(MetricUrn.Build("Root", "myMetric1")), Of(MetricUrn.Build("Root", "myMetric2")));
    var context = new WidgetRenderingContext
    {
      Widget = (TimeLinesWidget) chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(TimeSeriesFeed)] = new TimeSeriesRenderer()};
    new TimeLinesRenderer(feedRenderers).Render(context);

    var expected = TestHelperForString.RemoveEmptyLines($@"
function updateLineSeries(lineSeries, timeSeries) {{
  lineSeries.removePoints(0, lineSeries.count);
  for(var i=0; i<timeSeries.values.length; i++) {{
    lineSeries.append(timeSeries.timeline[i], timeSeries.values[i]);
  }}
}}
ChartView {{
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
  anchors {{
    left: parent.left
    leftMargin: 0
    top: parent.top
    topMargin: 0
    right: parent.right
    rightMargin: 0
    bottom: parent.bottom
    bottomMargin: 0
  }}
  legend.visible: false
  antialiasing: true
  property var xMainAxis : DateTimeAxis {{
    min: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.getTimelineMinDate()
    max: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.getTimelineMaxDate()
    format: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.determineTimelineFormat()
    titleText: ""("" + root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.determineTimelineFormat() + "")""
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var yLeftAxis: ValueAxis {{
    min: Js.mathMinOfNotNullValues(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric2.min)
    max: Math.max(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric2.max)
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric1);
    }}
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric2.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$Root$myMetric2);
    }}
  }}
}}") + Environment.NewLine;

    Check.That(context.Code.Result).IsEqualTo(expected);
  }
}