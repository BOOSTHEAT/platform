using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class DataDrivenImageRenderer : IRenderWidget
  {
    private readonly DirectoryInfo _workspace;
    private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

    public DataDrivenImageRenderer(DirectoryInfo workspace, Dictionary<Type, IRenderFeed> feedRenderers)
    {
      _workspace = workspace;
      _feedRenderers = feedRenderers;
    }

    public void Render(WidgetRenderingContext context)
    {
      var image = (DataDrivenImageWidget)context.Widget;
      var size = ImageRenderer.GetSize(_workspace.FullName,(image.Path as Const<string>)?.Value);
      string GetValue(Feed f) => _feedRenderers.GetValueOf(f.OutOfCache(context.Cache).RawValue.UseNeutralSettings);
      context.Code.Open(context.Prefix+" AnimatedImage"); 
      context.RenderBase();
      context.Code.Append(
          $"width: {size.Width}",
          $"height: {size.Height}",
          $"source: {GetValue(image.Path)}",
          $"property var input: {GetValue(image.Value)}",
          $"property var floor: {GetValue(image.Floor)}",
          $"property var step: {GetValue(image.Step)}",
          "currentFrame: Math.round((input - floor)/step)",
          "paused: true",
          "opacity: 1")
        .Close();
    }

    public IEnumerable<Feed> FindFeeds(Widget widget) => new[] { DD(widget).Path, DD(widget).Value, DD(widget).Floor, DD(widget).Step };
    private DataDrivenImageWidget DD(Widget widget) => (DataDrivenImageWidget)widget;

  }
}