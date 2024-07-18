using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets;

internal sealed class SendRenderer : IRenderWidget
{
  private readonly Dictionary<Type, IRenderWidget> _widgetRenderers;

  public SendRenderer(Dictionary<Type, IRenderWidget> widgetRenderers)
  {
    _widgetRenderers = widgetRenderers ?? throw new ArgumentNullException(nameof(widgetRenderers));
  }

  public void Render(WidgetRenderingContext context)
  {
    var widget = (SendWidget) context.Widget;

    context.Code.Open(context.Prefix + " ClickableContainer");
    context.RenderBaseWithBaseChild("visual");
    _widgetRenderers.Render(context.Override(widget: widget.Visual, prefix: "visual:").WithOnClickedEventRoute("parent.parent"));

    context.Code
      .Open("onClicked:")
      .Append($"{context.Api}.sendCommand(\"{widget.CommandUrn}\");")
      .Append("if(typeof parent.clicked === 'function') parent.clicked();")
      .Close()
      .Close();
  }

  public IEnumerable<Feed> FindFeeds(Widget widget)
  {
    if (widget == null) throw new ArgumentNullException(nameof(widget));
    var sendWidget = (SendWidget) widget;
    return _widgetRenderers.FindFeeds(sendWidget.Visual);
  }
} 