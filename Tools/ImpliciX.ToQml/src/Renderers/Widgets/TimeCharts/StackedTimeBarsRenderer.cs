using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

internal sealed class StackedTimeBarsRenderer : TimeChartRenderer
{
  public StackedTimeBarsRenderer(Dictionary<Type, IRenderFeed> feedRenderers) : base(feedRenderers)
  {
  }
  
  public override void RenderTimeChart(TimeChartContext context)
  {
    var seriesInfos = Feeds.ToChartSeriesInfos(context);

    context.OpenChartView();
    context.DefineXAxisFromCategories(seriesInfos.First().Series);
    context.DefineCumulativeYAxis(Feeds, TimeChartCodeGeneration.VerticalAxisPosition.Left);

    context.OpenSeries("StackedBarSeries", TimeChartCodeGeneration.VerticalAxisPosition.Left);
    
    foreach (var info in seriesInfos)
      context.AddBarSet(info.Label, info.Values, info.Color);

    context.CloseSeries();
    context.CloseChartView();
  }
  
}