using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class BoxRenderer : IRenderWidget
  {
    public void Render(WidgetRenderingContext context)
    {
      var border = (BoxWidget)context.Widget;
      context.Code.Open(context.Prefix + " Rectangle");
      context.RenderBase(true);
      context.Code
        .Append($"radius: {border.Radius.GetValueOrDefault(0)}")
        .Append(
          border.Style.FrontColor.HasValue,
          () => $"border.color: {StyleRenderer.ToQmlString(border.Style.FrontColor!.Value)}")
        .Append(
          border.Style.FrontColor.HasValue,
          () => $"border.width: 3")
        .Append(!border.Style.BackColor.HasValue,() => "color : 'transparent'")
        .Append(
          border.Style.BackColor.HasValue,
          () => $"color: {StyleRenderer.ToQmlString(border.Style.BackColor!.Value)}")
        .Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget) => Enumerable.Empty<Feed>();
  }
}