using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Renderers.Widgets.TimeCharts;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class ChartsYMinMaxTests : Screens
{
  private const string _rootUrn = nameof(ChartsYMinMaxTests);

  private readonly Dictionary<Type, IRenderFeed> _feedRenderers = new ()
  {
    [typeof(PropertyFeed)] = new PropertyRenderer(),
    [typeof(TimeSeriesFeed)] = new TimeSeriesRenderer()
  };

  private static readonly DecoratedUrn _of = Of(PropertyUrn<Temperature>.Build(nameof(ChartsYMinMaxTests), "temperature"));

  private static WidgetRenderingContext CreateContextForTimeLines(Option<string> yMinUrn, Option<string> yMaxUrn)
  {
    var chart = Chart.TimeLines(_of).Width(600).Height(400);
    yMinUrn.Tap(urn => chart = chart.YMin(PropertyUrn<Flow>.Build(_rootUrn, urn)));
    yMaxUrn.Tap(urn => chart = chart.YMax(PropertyUrn<Flow>.Build(_rootUrn, urn)));

    return new WidgetRenderingContext
    {
      Widget = (TimeLinesWidget) chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };
  }

  private static WidgetRenderingContext CreateContextForStackedTimeBars(Option<string> yMinUrn, Option<string> yMaxUrn)
  {
    var chart = Chart.StackedTimeBars(_of).Width(600).Height(400);
    yMinUrn.Tap(urn => chart = chart.YMin(PropertyUrn<Flow>.Build(_rootUrn, urn)));
    yMaxUrn.Tap(urn => chart = chart.YMax(PropertyUrn<Flow>.Build(_rootUrn, urn)));

    return new WidgetRenderingContext
    {
      Widget = (StackedTimeBarsWidget) chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };
  }

  private static WidgetRenderingContext CreateContextForBars(Option<string> yMinUrn, Option<string> yMaxUrn)
  {
    var chart = Chart.Bars(_of).Width(600).Height(400);
    yMinUrn.Tap(urn => chart = chart.YMin(PropertyUrn<Flow>.Build(_rootUrn, urn)));
    yMaxUrn.Tap(urn => chart = chart.YMax(PropertyUrn<Flow>.Build(_rootUrn, urn)));

    return new WidgetRenderingContext
    {
      Widget = (BarsWidget) chart.CreateWidget(),
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };
  }

  #region No YMin / YMax

  [Test]
  public void GivenBarsChart_WhenNoYMinMax()
  {
    var context = CreateContextForBars(Option<string>.None(), Option<string>.None());
    new BarsChartRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
      axisY: ValueAxis {
            min: Math.min(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            max: Math.max(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
      """);
  }

  [Test]
  public void GivenStackedTimeBars_WhenNoYMinMax()
  {
    var context = CreateContextForStackedTimeBars(Option<string>.None(), Option<string>.None());
    new StackedTimeBarsRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
        property var yLeftAxis: ValueAxis {
          min: Math.min(0, Math.min(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min))
          max: Math.max(0, [root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max].reduce((a,b)=>a+b, 0))
          labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
      """);
  }

  [Test]
  public void GivenTimeLines_WhenNoYMinMax()
  {
    var context = CreateContextForTimeLines(Option<string>.None(), Option<string>.None());
    new TimeLinesRenderer(_feedRenderers).Render(context);
    
    Check.That(context.Code.Result).Contains(
      $$"""
          property var yLeftAxis: ValueAxis {
            min: Js.mathMinOfNotNullValues(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min)
            max: Math.max(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
        """);
  }

  #endregion

  #region YMin

  [Test]
  public void GivenBarsChart_WhenYMinOnly()
  {
    var context = CreateContextForBars("y_min", Option<string>.None());
    new BarsChartRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
      axisY: ValueAxis {
            min: root.runtime.cache.ChartsYMinMaxTests$y_min.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_min.value : Math.min(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            max: Math.max(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
      """);
  }

  [Test]
  public void GivenStackedTimeBars_WhenYMinOnly()
  {
    var context = CreateContextForStackedTimeBars("y_min", Option<string>.None());
    new StackedTimeBarsRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
        property var yLeftAxis: ValueAxis {
          min: root.runtime.cache.ChartsYMinMaxTests$y_min.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_min.value : Math.min(0, Math.min(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min))
          max: Math.max(0, [root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max].reduce((a,b)=>a+b, 0))
          labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
      """);
  }

  [Test]
  public void GivenTimeLines_WhenYMinOnly()
  {
    var context = CreateContextForTimeLines("y_min", Option<string>.None());
    new TimeLinesRenderer(_feedRenderers).Render(context);
    
    Check.That(context.Code.Result).Contains(
      $$"""
        property var yLeftAxis: ValueAxis {
            min: root.runtime.cache.ChartsYMinMaxTests$y_min.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_min.value : Js.mathMinOfNotNullValues(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min)
            max: Math.max(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
        """);
  }

  #endregion

  #region YMax

  [Test]
  public void GivenBarsChart_WhenYMaxOnly()
  {
    var context = CreateContextForBars(Option<string>.None(), "y_max");
    new BarsChartRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
      axisY: ValueAxis {
            min: Math.min(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            max: root.runtime.cache.ChartsYMinMaxTests$y_max.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_max.value : Math.max(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
      """);
  }

  [Test]
  public void GivenStackedTimeBars_WhenYMaxOnly()
  {
    var context = CreateContextForStackedTimeBars(Option<string>.None(), "y_max");
    new StackedTimeBarsRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
        property var yLeftAxis: ValueAxis {
          min: Math.min(0, Math.min(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min))
          max: root.runtime.cache.ChartsYMinMaxTests$y_max.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_max.value : Math.max(0, [root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max].reduce((a,b)=>a+b, 0))
          labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
      """);
  }

  [Test]
  public void GivenTimeLines_WhenYMaxOnly()
  {
    var context = CreateContextForTimeLines(Option<string>.None(), "y_max");
    new TimeLinesRenderer(_feedRenderers).Render(context);
    
    Check.That(context.Code.Result).Contains(
      $$"""
        property var yLeftAxis: ValueAxis {
            min: Js.mathMinOfNotNullValues(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min)
            max: root.runtime.cache.ChartsYMinMaxTests$y_max.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_max.value : Math.max(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
        """);
  }

  #endregion

  #region YMin and YMax

  [Test]
  public void GivenBarsChart_WhenYMinAndYMax()
  {
    var context = CreateContextForBars("y_min", "y_max");
    new BarsChartRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
      axisY: ValueAxis {
            min: root.runtime.cache.ChartsYMinMaxTests$y_min.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_min.value : Math.min(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            max: root.runtime.cache.ChartsYMinMaxTests$y_max.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_max.value : Math.max(0,root.runtime.cache.ChartsYMinMaxTests$temperature.value)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
      """);
  }

  [Test]
  public void GivenStackedTimeBars_WhenYMinAndYMax()
  {
    var context = CreateContextForStackedTimeBars("y_min", "y_max");
    new StackedTimeBarsRenderer(_feedRenderers).Render(context);

    Check.That(context.Code.Result).Contains(
      """
        property var yLeftAxis: ValueAxis {
          min: root.runtime.cache.ChartsYMinMaxTests$y_min.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_min.value : Math.min(0, Math.min(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min))
          max: root.runtime.cache.ChartsYMinMaxTests$y_max.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_max.value : Math.max(0, [root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max].reduce((a,b)=>a+b, 0))
          labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
      """);
  }

  [Test]
  public void GivenTimeLines_WhenYMinAndYMax()
  {
    var context = CreateContextForTimeLines("y_min", "y_max");
    new TimeLinesRenderer(_feedRenderers).Render(context);
    
    Check.That(context.Code.Result).Contains(
      $$"""
        property var yLeftAxis: ValueAxis {
            min: root.runtime.cache.ChartsYMinMaxTests$y_min.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_min.value : Js.mathMinOfNotNullValues(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.min)
            max: root.runtime.cache.ChartsYMinMaxTests$y_max.value !== undefined ? root.runtime.cache.ChartsYMinMaxTests$y_max.value : Math.max(root.runtime.cache.timeSeries$ChartsYMinMaxTests$temperature.max)
            labelsFont:Qt.font({family:UiConst.fontMd,pixelSize:11})
          }
        """);
  }

  #endregion
}