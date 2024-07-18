using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

public abstract class TimeChartRenderer : IRenderWidget
{
  public TimeChartFeeds Feeds { get; }

  protected TimeChartRenderer(Dictionary<Type, IRenderFeed> feedRenderers)
  {
    Feeds = new TimeChartFeeds(feedRenderers);
  }

  public void Render(WidgetRenderingContext context) => RenderTimeChart(TimeChartContext.From(context));

  public abstract void RenderTimeChart(TimeChartContext context);

  public IEnumerable<Feed> FindFeeds(Widget widget) => TimeChartContext.AllFeeds(widget);
}