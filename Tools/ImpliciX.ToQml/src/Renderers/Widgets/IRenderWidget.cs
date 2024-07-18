using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public interface IRenderWidget
  {
    /// <summary>
    /// Render widget inside a screen
    /// </summary>
    void Render(WidgetRenderingContext context);
    
    /// <summary>
    /// list all feeds used in the widget
    /// </summary>
    IEnumerable<Feed> FindFeeds(Widget widget);
  }
  
  public static class RenderWidgetExtensions
  {
    public static void Render(this Dictionary<Type, IRenderWidget> renderers, WidgetRenderingContext context)
      => renderers.GetRenderer(context.Widget).Render(context);

    public static IEnumerable<Feed> FindFeeds(this Dictionary<Type, IRenderWidget> renderers, Widget widget)
      => renderers.GetRenderer(widget).FindFeeds(widget);

    public static IRenderWidget GetRenderer(this Dictionary<Type, IRenderWidget> renderers, Widget widget)
      => renderers.GetRenderer(widget.GetType(),widget.GetType());

    static IRenderWidget GetRenderer(this Dictionary<Type, IRenderWidget> renderers, Type type, Type refType)
    {
      if (type == null)
        throw new Exception($"Cannot find renderer for widget {refType.Name}");
      if (renderers.TryGetValue(type, out IRenderWidget renderer))
        return renderer;
      return renderers.GetRenderer(type.BaseType, refType);
    }
  }
}