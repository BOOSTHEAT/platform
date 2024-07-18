using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets;

public class NavigatorRenderer : IRenderWidget
{
  private readonly Dictionary<Type, IRenderWidget> _widgetRenderers;

  public NavigatorRenderer(Dictionary<Type, IRenderWidget> widgetRenderers)
  {
    _widgetRenderers = widgetRenderers;
  }

  public void Render(WidgetRenderingContext context)
  {
    if (context == null) throw new ArgumentNullException(nameof(context));

    var navigator = (NavigatorWidget) context.Widget;
    context.Code.Open(context.Prefix + " Navigator");
    context.RenderBaseWithBaseChild("visual");
    _widgetRenderers.Render(context.Override(widget: navigator.Visual, prefix: "visual:").WithOnClickedEventRoute("parent.parent"));
    _widgetRenderers.Render(context.Override(widget: navigator.OnTarget, prefix: "checkedMark:"));
    context.Code
      .Append($"route: \"{navigator.TargetScreen.Urn.Value}\"")
      .Append("indicator: null")
      .Close();
  }

  public IEnumerable<Feed> FindFeeds(Widget widget)
  {
    if (widget == null) throw new ArgumentNullException(nameof(widget));

    var navigator = (NavigatorWidget) widget;
    return _widgetRenderers.FindFeeds(navigator.Visual)
      .Concat(_widgetRenderers.FindFeeds(navigator.OnTarget));
  }
}