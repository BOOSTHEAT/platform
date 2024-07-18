using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Renderers.Widgets.TimeCharts;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class ChartRendersCommonTests : Screens
{
  private readonly Dictionary<Type, IRenderWidget> _widgetRenderers;

  private static object[] _chartCases =
  {
    new object[]
    {
      CreateContext(Chart.Pie(Of(MetricUrn.Build("Root", "myMetric1")), Of(MetricUrn.Build("Root", "myMetric2"))).CreateWidget())
    },
    new object[]
    {
      CreateContext(Chart.StackedTimeBars(Of(MetricUrn.Build("Root", "myMetric1"))).CreateWidget())
    },
    new object[]
    {
      CreateContext(Chart.TimeLines(Of(MetricUrn.Build("Root", "myMetric1"))).CreateWidget())
    }
  };

  [TestCaseSource(nameof(_chartCases))]
  public void GivenOnClickedEventRouteIsSome_WhenIRender_ThenCodeContainsOnClickedFollowToParent(WidgetRenderingContext context)
  {
    context.WithOnClickedEventRoute("parent.parent");
    _widgetRenderers.Render(context);

    Check.That(context.Code.Result).Contains(@"antialiasing: true
  MouseArea {
    anchors.fill: parent
    onClicked: parent.parent.clicked()
  }");
  }

  [TestCaseSource(nameof(_chartCases))]
  public void GivenOnClickedEventRouteIsNone_WhenIRender_ThenCodeContainsOnClickedFollowToParent(WidgetRenderingContext context)
  {
    _widgetRenderers.Render(context);
    Check.That(context.Code.Result).Not.Contains("MouseArea");
  }

  private static WidgetRenderingContext CreateContext(Widget widget)
  {
    return new WidgetRenderingContext
    {
      Widget = widget,
      Code = new SourceCodeGenerator()
    };
  }

  public ChartRendersCommonTests()
  {
    var feedRenderers = new Dictionary<Type, IRenderFeed>
    {
      [typeof(TimeSeriesFeed)] = new TimeSeriesRenderer(),
      [typeof(PropertyFeed)] = new PropertyRenderer()
    };

    _widgetRenderers = new Dictionary<Type, IRenderWidget>
    {
      [typeof(PieChartWidget)] = new PieChartRenderer(feedRenderers),
      [typeof(TimeLinesWidget)] = new TimeLinesRenderer(feedRenderers),
      [typeof(StackedTimeBarsWidget)] = new StackedTimeBarsRenderer(feedRenderers)
    };
  }
}