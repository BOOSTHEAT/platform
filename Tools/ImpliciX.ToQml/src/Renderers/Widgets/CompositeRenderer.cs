using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class CompositeRenderer : IRenderWidget
  {
    private readonly Dictionary<Type, IRenderWidget> _renderers;

    public CompositeRenderer(Dictionary<Type, IRenderWidget> renderers)
    {
      _renderers = renderers;
    }

    public void Render(WidgetRenderingContext context)
    {
      var composite = (Composite) context.Widget;
      var frames = new Dictionary<Composite.ArrangeAs, string>
      {
        [Composite.ArrangeAs.XY] = "Rectangle",
        [Composite.ArrangeAs.Column] = "Column",
        [Composite.ArrangeAs.Row] = "Row",
      };

      var content = composite.Content.Select((item, index) => (Index: index, Name: $"item{index}", Widget: item)).ToArray();
      context.Code.Open(context.Prefix + " " + frames[composite.Arrange]);
      var widgetWhichIsBase = content.FirstOrDefault(x => x.Widget.IsBase);
      if (widgetWhichIsBase == default)
        context.RenderBase(composite.Arrange == Composite.ArrangeAs.XY);
      else
        context.RenderBaseWithBaseChild(widgetWhichIsBase.Name);

      foreach (var item in content)
      {
        _renderers.Render(context.Override(widget: item.Widget, style: composite.Style.Fallback(context.Style), prefix: $"property var {item.Name}:"));
      }

      context.Code.Append($"data: [{string.Join(',', content.Select(x => x.Name))}]");
      if (composite.Arrange != Composite.ArrangeAs.XY)
        context.Code.Append($"spacing: {composite.Spacing.GetValueOrDefault(0)}");

      context.Code.Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget)
    {
      var composite = (Composite) widget;
      return composite.Content.SelectMany(w => _renderers.FindFeeds(w));
    }
  }
}