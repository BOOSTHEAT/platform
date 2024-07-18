using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

public class TimeChartFeeds
{
  private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

  public TimeChartFeeds(Dictionary<Type, IRenderFeed> feedRenderers) => _feedRenderers = feedRenderers;
  
  public ChartSeriesInfo[] ToChartSeriesInfos(TimeChartContext context) => GetFeeds(context).ToArray();
  
  public string YMax(TimeChartContext context, Func<string[], string> maxAuto)
  {
    var feeds = GetFeeds(context);
    return GetYBound(context.YMax, context.Cache, "max",
      maxAuto(feeds.Select(f => f.Series+".max").ToArray()));
  }

  public string YMin(TimeChartContext context, Func<string[], string> minAuto)
  {
    var feeds = GetFeeds(context);
    return GetYBound(context.YMin, context.Cache, "min",
      minAuto(feeds.Select(f => f.Series+".min").ToArray()));
  }

  private string GetYBound(Feed bound, string cache, string prop, string yAuto)
  {
    if (bound == null) return $"{prop}: {yAuto}";

    var yByPropUrn = _feedRenderers.GetValueOf(bound.OutOfCache(cache).RawValue);
    var propertyValue = $"{yByPropUrn} !== undefined ? {yByPropUrn} : {yAuto}";
    return $"{prop}: {propertyValue}";
  }
  
  private IEnumerable<ChartSeriesInfo> GetFeeds(
    TimeChartContext context) =>
    from feedDecoration in context.Content
    let feedUse = feedDecoration.Value.OutOfCache(context.Cache)
    let feedRenderer = _feedRenderers.GetRenderer(feedUse.Feed)
    select new ChartSeriesInfo(feedDecoration, feedRenderer, feedUse);
}