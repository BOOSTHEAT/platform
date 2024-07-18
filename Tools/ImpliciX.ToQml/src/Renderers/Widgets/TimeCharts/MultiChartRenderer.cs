using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

public class MultiChartRenderer : IRenderWidget
{
  public TimeChartFeeds Feeds { get; }

  public MultiChartRenderer(Dictionary<Type, IRenderFeed> feedRenderers)
  {
    Feeds = new TimeChartFeeds(feedRenderers);
  }

  public void Render(WidgetRenderingContext context)
  {
    var mainContext = new TimeChartCodeGeneration(context);
    var multiChart = (MultiChartWidget)context.Widget;

    var primaryContext = TimeChartContext.From(context, multiChart.Left);
    var primaryInfos = Feeds.ToChartSeriesInfos(primaryContext);

    var secondaryContext = TimeChartContext.From(context, multiChart.Right);
    var secondaryInfos = Feeds.ToChartSeriesInfos(secondaryContext);

    mainContext.UseTimeLines();
    mainContext.OpenChartView();

    if (multiChart.Left is StackedTimeBarsWidget)
    {
      primaryContext.DefineXAxisFromCategories(primaryInfos.First().Series);
      secondaryContext.DefineXAxisFromCategories(primaryInfos.First().Series, true);

      primaryContext.DefineCumulativeYAxis(Feeds, TimeChartCodeGeneration.VerticalAxisPosition.Left);
      secondaryContext.DefineStandardYAxis(Feeds, TimeChartCodeGeneration.VerticalAxisPosition.Right);
      
      primaryContext.OpenSeries("StackedBarSeries", TimeChartCodeGeneration.VerticalAxisPosition.Left);
      foreach (var info in primaryInfos)
        primaryContext.AddBarSet(info.Label, info.Values, info.Color);
      primaryContext.CloseSeries();

      foreach (var info in secondaryInfos)
      {
        secondaryContext.OpenSeries("LineSeries", TimeChartCodeGeneration.VerticalAxisPosition.Right, true);
        secondaryContext.AddLine(info.Series, info.Values, info.Color);
        secondaryContext.CloseSeries();
      }
    }
    else if (multiChart.Left is TimeLinesWidget)
    {
      primaryContext.DefineXAxisFromDateRange(primaryInfos.First().Series);

      primaryContext.DefineStandardYAxis(Feeds, TimeChartCodeGeneration.VerticalAxisPosition.Left);
      secondaryContext.DefineStandardYAxis(Feeds, TimeChartCodeGeneration.VerticalAxisPosition.Right);
      
      foreach (var info in primaryInfos)
      {
        secondaryContext.OpenSeries("LineSeries", TimeChartCodeGeneration.VerticalAxisPosition.Left);
        secondaryContext.AddLine(info.Series, info.Values, info.Color);
        secondaryContext.CloseSeries();
      }
      foreach (var info in secondaryInfos)
      {
        secondaryContext.OpenSeries("LineSeries", TimeChartCodeGeneration.VerticalAxisPosition.Right);
        secondaryContext.AddLine(info.Series, info.Values, info.Color);
        secondaryContext.CloseSeries();
      }
    }
    else
    {
      throw new NotSupportedException($"Unsupported left chart {multiChart.Left}");
    }
    
    
    mainContext.CloseChartView();
  }

  public IEnumerable<Feed> FindFeeds(Widget widget)
  {
    var multiChart = (MultiChartWidget)widget;
    return TimeChartContext.AllFeeds(multiChart.Left).Concat(TimeChartContext.AllFeeds(multiChart.Right));
  }
}