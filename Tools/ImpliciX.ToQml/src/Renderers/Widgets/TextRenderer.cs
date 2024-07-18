using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class TextRenderer : IRenderWidget
  {
    private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

    public TextRenderer(Dictionary<Type, IRenderFeed> feedRenderers)
    {
      _feedRenderers = feedRenderers;
    }

    public void Render(WidgetRenderingContext context)
    {
      var text = (Text) context.Widget;
      var feed = text.Value;
      context.Code.Open(context.Prefix + " Text");
      context.RenderBase();
      context.Code.Append($"text: {_feedRenderers.GetValueOf(feed.OutOfCache(context.Cache).UseLocalSettings.Formatted)}");
      if (text.X.Size.HasValue)
        context.Code.Append(
          "horizontalAlignment: Text.AlignHCenter",
          "verticalAlignment: Text.AlignVCenter",
          "wrapMode: Text.WordWrap");

      text.Style.Fallback(context.Style).Render(context.Code);
      context.Code.Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget)
    {
      var text = (Text) widget;
      yield return text.Value;
    }
  }
}