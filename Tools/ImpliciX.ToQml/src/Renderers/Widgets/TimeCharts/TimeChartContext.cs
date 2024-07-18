using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

public class TimeChartContext : TimeChartCodeGeneration
{
  private ChartXTimeYWidget Widget { get; }
  public FeedDecoration[] Content => Widget.Content;
  public Feed YMin => Widget.YMin;
  public Feed YMax => Widget.YMax;
  
  public static IEnumerable<Feed> AllFeeds(Widget widget) =>
    Select<IEnumerable<Feed>>(widget, chartWidget =>
    {
      var feeds = new List<Feed>(chartWidget.Content.Select(feed => feed.Value));
      if (chartWidget.YMin is not null) feeds.Add(chartWidget.YMin);
      if (chartWidget.YMax is not null) feeds.Add(chartWidget.YMax);
      return feeds;
    });

  public static TimeChartContext From(WidgetRenderingContext context, ChartXTimeYWidget widget = null) =>
    widget != null
      ? new TimeChartContext(context, widget)
      : Select(context.Widget, w => new TimeChartContext(context, w));

  private static T Select<T>(Widget widget, Func<ChartXTimeYWidget,T> selector)
  {
    if (widget is ChartXTimeYWidget chartWidget)
      return selector(chartWidget);
    throw new InvalidOperationException($"Widget {widget} is not a chart widget with time on x-axis");
  }

  private TimeChartContext(WidgetRenderingContext context, ChartXTimeYWidget widget) : base(context)
  {
    Widget = widget;
  }

  public void DefineStandardYAxis(TimeChartFeeds feeds, VerticalAxisPosition position) =>
    DefineYAxis(
      feeds,
      position,
      feeds.YMin(this, minValues => $"Js.mathMinOfNotNullValues({string.Join(", ", minValues)})"),
      feeds.YMax(this, maxValues => $"Math.max({string.Join(", ", maxValues)})")
    );

  public void DefineCumulativeYAxis(TimeChartFeeds feeds, VerticalAxisPosition position) =>
    DefineYAxis(
      feeds,
      position,
      feeds.YMin(this, minValues => $"Math.min(0, Math.min({string.Join(", ", minValues)}))"),
      feeds.YMax(this, maxValues => $"Math.max(0, [{string.Join(", ", maxValues)}].reduce((a,b)=>a+b, 0))")
    );
}