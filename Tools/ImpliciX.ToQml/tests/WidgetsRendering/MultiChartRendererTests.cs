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

public class MultiChartRendererTests : Screens
{
  [Test]
  public void GivenMultiChartWithBarsAndLines_WhenIRender_ThenIGetCodeGenerateExpected()
  {
    const string rootUrn = "toto";

    var chart = Chart.Multi(
      Chart.StackedTimeBars(
        Of(Urn.BuildUrn(rootUrn, "BarSeries_1")).Fill(Color.LimeGreen),
        Of(Urn.BuildUrn(rootUrn, "BarSeries_2")).Fill(Color.Orange),
        Of(Urn.BuildUrn(rootUrn, "BarSeries_3")).Fill(Color.Gold)
      ),
      Chart.TimeLines(
        Of(Urn.BuildUrn(rootUrn, "LineSeries_1")).Fill(Color.Crimson),
        Of(Urn.BuildUrn(rootUrn, "LineSeries_2")).Fill(Color.Blue)
      )
    ).Width(700).Height(450);
    

    var context = new WidgetRenderingContext
    {
      Widget = chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> { [typeof(TimeSeriesFeed)] = new TimeSeriesRenderer() };
    new MultiChartRenderer(feedRenderers).Render(context);

    const string expected = $@"function updateLineSeries(lineSeries, timeSeries) {{
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
  width: 700
  height: 450
  legend.visible: false
  antialiasing: true
  property var xMainAxis: BarCategoryAxis {{
    categories: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.getFormattedTimeline(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.determineTimelineFormat())
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var xSecondaryAxis : DateTimeAxis {{
    min: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.getTimelineMinDate(-root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.getHalfPeriod())
    max: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.getTimelineMaxDate(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.getHalfPeriod())
    visible: false
  }}
  property var yLeftAxis: ValueAxis {{
    min: Math.min(0, Math.min(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_2.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_3.min))
    max: Math.max(0, [root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_2.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_3.max].reduce((a,b)=>a+b, 0))
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var yRightAxis: ValueAxis {{
    min: Js.mathMinOfNotNullValues(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_1.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_2.min)
    max: Math.max(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_1.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_2.max)
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  StackedBarSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    BarSet {{
      label: ""toto:BarSeries_1""
      values: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_1.values
      color: ""#32CD32""
    }}
    BarSet {{
      label: ""toto:BarSeries_2""
      values: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_2.values
      color: ""#FFA500""
    }}
    BarSet {{
      label: ""toto:BarSeries_3""
      values: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$BarSeries_3.values
      color: ""#FFD700""
    }}
  }}
  LineSeries {{
    property var xAxisType : 1
    property var yAxisPosition : 1
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_1.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_1);
    }}
    color: ""#DC143C""
  }}
  LineSeries {{
    property var xAxisType : 1
    property var yAxisPosition : 1
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_2.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LineSeries_2);
    }}
    color: ""#0000FF""
  }}
}}
";

    Check.That(context.Code.Result).IsEqualTo(expected);
  }
  
    [Test]
  public void GivenMultiChartWithLines_WhenIRender_ThenIGetCodeGenerateExpected()
  {
    const string rootUrn = "toto";

    var chart = Chart.Multi(
      Chart.TimeLines(
        Of(Urn.BuildUrn(rootUrn, "LeftLinesSeries_1")).Fill(Color.LimeGreen),
        Of(Urn.BuildUrn(rootUrn, "LeftLinesSeries_2")).Fill(Color.Orange),
        Of(Urn.BuildUrn(rootUrn, "LeftLinesSeries_3")).Fill(Color.Gold)
      ),
      Chart.TimeLines(
        Of(Urn.BuildUrn(rootUrn, "RightLineSeries_1")).Fill(Color.Crimson),
        Of(Urn.BuildUrn(rootUrn, "RightLineSeries_2")).Fill(Color.Blue)
      )
    ).Width(700).Height(450);
    

    var context = new WidgetRenderingContext
    {
      Widget = chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> { [typeof(TimeSeriesFeed)] = new TimeSeriesRenderer() };
    new MultiChartRenderer(feedRenderers).Render(context);

    const string expected = $@"function updateLineSeries(lineSeries, timeSeries) {{
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
  width: 700
  height: 450
  legend.visible: false
  antialiasing: true
  property var xMainAxis : DateTimeAxis {{
    min: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.getTimelineMinDate()
    max: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.getTimelineMaxDate()
    format: root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.determineTimelineFormat()
    titleText: ""("" + root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.determineTimelineFormat() + "")""
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var yLeftAxis: ValueAxis {{
    min: Js.mathMinOfNotNullValues(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_2.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_3.min)
    max: Math.max(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_2.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_3.max)
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  property var yRightAxis: ValueAxis {{
    min: Js.mathMinOfNotNullValues(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_1.min, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_2.min)
    max: Math.max(root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_1.max, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_2.max)
    labelsFont:Qt.font({{family:UiConst.fontMd,pixelSize:11}})
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_1);
    }}
    color: ""#32CD32""
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_2.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_2);
    }}
    color: ""#FFA500""
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 0
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_3.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$LeftLinesSeries_3);
    }}
    color: ""#FFD700""
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 1
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_1.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_1);
    }}
    color: ""#DC143C""
  }}
  LineSeries {{
    property var xAxisType : 0
    property var yAxisPosition : 1
    property var tsValues : root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_2.values
    onTsValuesChanged: {{
      updateLineSeries(this, root.runtime.cache.{TimeSeriesRenderer.CacheUrnPrefix}$toto$RightLineSeries_2);
    }}
    color: ""#0000FF""
  }}
}}
";

    Check.That(context.Code.Result).IsEqualTo(expected);
  }
}