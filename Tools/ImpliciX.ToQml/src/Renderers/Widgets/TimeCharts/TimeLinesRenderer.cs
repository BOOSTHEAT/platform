using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

internal sealed class TimeLinesRenderer : TimeChartRenderer
{
  public TimeLinesRenderer(Dictionary<Type, IRenderFeed> feedRenderers) : base(feedRenderers)
  {
  }

  public override void RenderTimeChart(TimeChartContext context)
  {
    if (!context.Content.Any())
      throw new InvalidOperationException(
        $"There is no line define in {nameof(TimeLinesWidget)}. It can not be empty.");
    
    var lineSeriesInfos = Feeds.ToChartSeriesInfos(context);

    context.UseTimeLines();
    context.OpenChartView();
    context.DefineXAxisFromDateRange(lineSeriesInfos.First().Series);
    context.DefineStandardYAxis(Feeds, TimeChartCodeGeneration.VerticalAxisPosition.Left);

    foreach (var info in lineSeriesInfos)
    {
      context.OpenSeries("LineSeries", TimeChartCodeGeneration.VerticalAxisPosition.Left);
      context.AddLine(info.Series, info.Values, info.Color);
      context.CloseSeries();
    }
    
    context.CloseChartView();
  }

}