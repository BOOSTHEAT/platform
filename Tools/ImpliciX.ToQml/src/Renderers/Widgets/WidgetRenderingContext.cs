using System;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class WidgetRenderingContext
  {
    public Widget Widget;
    public Style Style;
    public string Prefix = string.Empty;
    public SourceCodeGenerator Code;
    public string Runtime;
    public string Cache => $"{Runtime}.cache";
    public string Api => $"{Runtime}.api";
    public Option<string> OnClickedEventRoute { get; private set; } = Option<string>.None();

    public WidgetRenderingContext Override(Widget widget = default, Style style = default, string prefix = default, SourceCodeGenerator code = default)
    {
      return new WidgetRenderingContext
      {
        Widget = widget ?? Widget,
        Style = style ?? Style,
        Prefix = prefix ?? Prefix,
        Code = code ?? Code,
        Runtime = Runtime
      };
    }

    public WidgetRenderingContext WithOnClickedEventRoute(string route)
    {
      OnClickedEventRoute = route ?? throw new ArgumentNullException(nameof(route));
      return this;
    }

    public void RenderBase(bool useParentSizeIfSizeSettingMissing = false)
    {
      RenderWidthAndHeight();
      RenderAnchors(useParentSizeIfSizeSettingMissing);
    }

    public void RenderBaseWithBaseChild(string baseChild)
    {
      if (string.IsNullOrEmpty(baseChild)) throw new ArgumentNullException(nameof(baseChild));

      Code.Append($"width:{baseChild}.width", $"height:{baseChild}.height");
      RenderAnchors(false);
    }

    private void RenderWidthAndHeight()
    {
      if (Widget.X.Size.HasValue)
        Code.Append($"width: {Widget.X.Size.Value}");

      if (Widget.Y.Size.HasValue)
        Code.Append($"height: {Widget.Y.Size.Value}");
    }

    private void RenderAnchors(bool useParentSizeIfSizeSettingMissing)
    {
      int? Fallback(bool hasSize)
      {
        return (useParentSizeIfSizeSettingMissing && !hasSize) ? 0 : null;
      }
      
      var sideAnchors = new[]
        {
          ("left", Widget.X.FromStart ?? Fallback(Widget.X.Size.HasValue)),
          ("top", Widget.Y.FromStart ?? Fallback(Widget.Y.Size.HasValue)),
          ("right", Widget.X.ToEnd ?? Fallback(Widget.X.Size.HasValue)),
          ("bottom", Widget.Y.ToEnd ?? Fallback(Widget.Y.Size.HasValue)),
        }
        .Where(x => x.Item2.HasValue)
        .SelectMany(x => new[]
        {
          $"{x.Item1}: parent.{x.Item1}",
          $"{x.Item1}Margin: {x.Item2.Value}"
        });

      var centerAnchors = new[]
        {
          ("horizontal", Center: Widget.X.CenterOffset),
          ("vertical", Center: Widget.Y.CenterOffset),
        }
        .Where(x => x.Item2.HasValue)
        .SelectMany(x => new[]
        {
          $"{x.Item1}Center: parent.{x.Item1}Center",
          $"{x.Item1}CenterOffset: {x.Item2.Value}"
        });

      var anchors = sideAnchors.Concat(centerAnchors).Cast<object>().ToArray();
      if (anchors.Length == 0)
        return;

      Code.Open("anchors").Append(anchors).Close();
    }
  }
}