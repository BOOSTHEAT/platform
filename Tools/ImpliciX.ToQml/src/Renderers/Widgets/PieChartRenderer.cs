using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Helpers;

namespace ImpliciX.ToQml.Renderers.Widgets;

internal sealed class PieChartRenderer : IRenderWidget
{
  private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

  public PieChartRenderer(Dictionary<Type, IRenderFeed> feedRenderers)
  {
    _feedRenderers = feedRenderers ?? throw new ArgumentNullException(nameof(feedRenderers));
  }

  public void Render(WidgetRenderingContext context)
  {
    if (context == null) throw new ArgumentNullException(nameof(context));

    var widget = (PieChartWidget) context.Widget;
    context.Code.Open(context.Prefix + "ChartView");
    context.RenderBase(useParentSizeIfSizeSettingMissing: true);

    context.Code
      .Append("legend.visible: false")
      .Append("antialiasing: true")
      .Append(context.OnClickedEventRoute.IsSome, () => ChartRenderHelpers.GetMouseAreaForOnClickedEventRoute(context.OnClickedEventRoute.GetValue()))
      .Open("PieSeries")
      .Append("holeSize: 0.5");

    AppendSlices(context.Code, context, widget.Slices)
      .Close();
  }

  public IEnumerable<Feed> FindFeeds(Widget widget) => ((PieChartWidget) widget).Slices.Select(slice => slice.Value);

  private SourceCodeGenerator AppendSlices(SourceCodeGenerator sourceGen, WidgetRenderingContext context, FeedDecoration[] slices)
  {
    return sourceGen
      .ForEach(slices, (slice, gen) =>
      {
        var feedUse = slice.Value.OutOfCache(context.Cache);
        var sliceValue = _feedRenderers.GetValueOf(feedUse.RawValue);

        AppendSlice(gen, context, sliceValue, slice.FillColor, slice.LabelStyle);
      })
      .Close();
  }
  
  static readonly Style DefaultStyle = new() { FontFamily = Style.Family.Regular, FontSize = 12 };

  private static void AppendSlice(SourceCodeGenerator sourceGen, WidgetRenderingContext context, string sliceValue, Color? sliceFillColor, Style sliceLabelStyle)
  {
    var labelStyle = sliceLabelStyle.Fallback(context.Style).Fallback(DefaultStyle);
    var fillColor = sliceFillColor.HasValue
      ? $@"color: {StyleRenderer.ToQmlString(sliceFillColor!.Value)}"
      : "";

    var labelColor = sliceLabelStyle is {FrontColor: not null}
      ? $"labelColor: {StyleRenderer.ToQmlString(sliceLabelStyle.FrontColor!.Value)}"
      : "";
    
    sourceGen
      .Open("PieSlice")
      .Append($"value: {sliceValue}")
      .Append(@"label: ""%1%"".arg((100 * percentage).toFixed(1))")
      .Append("labelArmLengthFactor: 0") // '0' to remove arm between slice and label display. The aim is to avoid display like : "4..." instead of "49.1%" 
      .Append("labelVisible: true")
      .Append(fillColor.Length > 0, () => fillColor)
      .Append($"labelFont:{labelStyle.AsQtFont()}")
      .Append(labelColor.Length > 0, () => labelColor)
      .Close();
  }
  
}