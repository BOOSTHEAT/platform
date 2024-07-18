#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Helpers;

namespace ImpliciX.ToQml.Renderers.Widgets;

public class BarsChartRenderer : IRenderWidget
{
  private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

  public BarsChartRenderer(Dictionary<Type, IRenderFeed> feedRenderers)
  {
    _feedRenderers = feedRenderers;
  }

  static readonly Style DefaultStyle = new () {FontFamily = Style.Family.Medium, FontSize = 11};

  public void Render(WidgetRenderingContext context)
  {
    if (context == null) throw new ArgumentNullException(nameof(context));

    var widget = (BarsWidget) context.Widget;
    var feedDecorations = widget.Bars
      .Select(feedDecoration => (
          decoration: feedDecoration,
          use: feedDecoration.Value.OutOfCache(context.Cache).UseLocalSettings.RawValue
        ))
      .Select((f, index) => (
          index, f.decoration,
          id: _feedRenderers.Id(f.use.Feed),
          value: _feedRenderers.GetValueOf(f.use)
        )).ToArray();

    context.Code.Open(context.Prefix + "ChartView");
    context.RenderBase(useParentSizeIfSizeSettingMissing: true);
    context.Code
      .Append("legend.visible: false")
      .Append("antialiasing: true")
      .Append(context.OnClickedEventRoute.IsSome, () => ChartRenderHelpers.GetMouseAreaForOnClickedEventRoute(context.OnClickedEventRoute.GetValue()))
      .Open("StackedBarSeries")
      .Open("axisX: BarCategoryAxis")
      .Append($"categories:[{string.Join(',', Enumerable.Range(0, widget.Bars.Length))}]")
      .Append("visible:false")
      .Close();

    context.Code
      .Open("axisY: ValueAxis")
      .Append(GetYMinAxisProperty(feedDecorations.Select(f => f.value), widget, context.Cache))
      .Append(GetYMaxAxisProperty(feedDecorations.Select(f => f.value), widget, context.Cache))
      .Append($"labelsFont:{DefaultStyle.AsQtFont()}")
      .Close();

    context.Code
      .ForEach(feedDecorations, (x, code) =>
      {
        var values = Enumerable.Repeat("0", x.index)
          .Append(x.value)
          .Concat(Enumerable.Repeat("0", widget.Bars.Length - x.index - 1));

        var fillColor = x.decoration.FillColor;

        code
          .Open("BarSet")
          .Append($"label: '{x.id}'")
          .Append($"values: [{string.Join(',', values)}]")
          .Append(fillColor.HasValue, () => $"color: {StyleRenderer.ToQmlString(fillColor!.Value)}")
          .Close();
      })
      .Close()
      .Close();
  }

  private string GetYMinAxisProperty(IEnumerable<string> minValues, BarsWidget widget, string contextCache)
  {
    var yAuto = $"Math.min(0,{string.Join(", ", minValues)})";
    if (widget.YMin == null) return $"min: {yAuto}";

    var yByPropUrn = _feedRenderers.GetValueOf(widget.YMin.OutOfCache(contextCache).RawValue);
    var propertyValue = $"{yByPropUrn} !== undefined ? {yByPropUrn} : {yAuto}";
    return $"min: {propertyValue}";
  }

  private string GetYMaxAxisProperty(IEnumerable<string> maxValues, BarsWidget widget, string contextCache)
  {
    var yAuto = $"Math.max(0,{string.Join(", ", maxValues)})";
    if (widget.YMax == null) return $"max: {yAuto}";

    var yByPropUrn = _feedRenderers.GetValueOf(widget.YMax.OutOfCache(contextCache).RawValue);
    var propertyValue = $"{yByPropUrn} !== undefined ? {yByPropUrn} : {yAuto}";
    return $"max: {propertyValue}";
  }
    
  public IEnumerable<Feed> FindFeeds(Widget widget)
  {
    var l_widget = (BarsWidget) widget;

    var feeds = new List<Feed>(l_widget.Bars.Select(feed => feed.Value));
    if (l_widget.YMin is not null) feeds.Add(l_widget.YMin);
    if (l_widget.YMax is not null) feeds.Add(l_widget.YMax);

    return feeds;
  }
}