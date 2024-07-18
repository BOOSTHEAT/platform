using System;
using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

public readonly struct ChartSeriesInfo
{
  private readonly FeedDecoration _feedDecoration;
  private readonly IRenderFeed _renderTimeSeries;
  private readonly FeedUse _feedUse;
    
  public string Series { get; }
  public string Label => ((TimeSeriesFeed) _feedDecoration.Value).Urn.Value;
  public string Values => _renderTimeSeries.GetValueOf(_feedUse);
  public Color? Color => _feedDecoration.FillColor;

  public ChartSeriesInfo(FeedDecoration feedDecoration, IRenderFeed renderTimeSeries, FeedUse feedUse)
  {
    _feedDecoration = feedDecoration;
    _renderTimeSeries = renderTimeSeries ?? throw new ArgumentNullException(nameof(renderTimeSeries));
    _feedUse = feedUse ?? throw new ArgumentNullException(nameof(feedUse));
    Series = _feedUse.PrependCacheIfNeeded(_renderTimeSeries.Id(_feedUse.Feed));
  }
}