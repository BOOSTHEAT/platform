using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets;

public class IncrementRenderer : IRenderWidget
{
  private readonly Dictionary<Type, IRenderWidget> _widgetRenderers;
  private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

  public IncrementRenderer(Dictionary<Type, IRenderWidget> widgetRenderers, Dictionary<Type, IRenderFeed> feedRenderers)
  {
    _widgetRenderers = widgetRenderers ?? throw new ArgumentNullException(nameof(widgetRenderers));
    _feedRenderers = feedRenderers ?? throw new ArgumentNullException(nameof(feedRenderers));
  }

  public void Render(WidgetRenderingContext context)
  {
    var widget = (IncrementWidget) context.Widget;

    context.Code.Open(context.Prefix + " ClickableContainer");
    context.RenderBaseWithBaseChild("visual");
    _widgetRenderers.Render(context.Override(widget: widget.Visual, prefix: "visual:").WithOnClickedEventRoute("parent.parent"));

    var inputCacheUrn = _feedRenderers.GetValueOf(widget.InputUrn.OutOfCache(context.Cache));
    var inputUrn = ((PropertyFeed) widget.InputUrn).Urn;

    context.Code
      .Open("onClicked:")
      .Append($"var newValue = Js.toFloat({inputCacheUrn}) + {widget.StepValue};")
      .Append($@"{context.Api}.sendProperty(""{inputUrn}"", newValue);")
      .Append("if(typeof parent.clicked === 'function') parent.clicked();")
      .Close()
      .Close();
  }

  public IEnumerable<Feed> FindFeeds(Widget widget)
  {
    if (widget == null) throw new ArgumentNullException(nameof(widget));
    var incrementWidget = (IncrementWidget) widget;
    return _widgetRenderers
      .FindFeeds(incrementWidget.Visual)
      .Prepend(incrementWidget.InputUrn);
  }
}