using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using MetadataExtractor;

namespace ImpliciX.ToQml.Renderers.Widgets
{
  public class ImageRenderer : IRenderWidget
  {
    private readonly DirectoryInfo _workspace;
    private readonly Dictionary<Type, IRenderFeed> _feedRenderers;

    public ImageRenderer(DirectoryInfo workspace, Dictionary<Type, IRenderFeed> feedRenderers)
    {
      _workspace = workspace;
      _feedRenderers = feedRenderers;
    }

    public void Render(WidgetRenderingContext context)
    {
      var image = (ImageWidget)context.Widget;
      var size = GetSize(_workspace.FullName,image);
      context.Code.Open(context.Prefix+" AnimatedImage"); 
      context.RenderBase();
      context.Code.Append(
          $"width: {size.Width}",
          $"height: {size.Height}",
          $"source: {_feedRenderers.GetValueOf(image.Path.OutOfCache(context.Cache))}",
          "onSourceChanged: playing = true",
          "opacity: 1")
        .Close();
    }

    public static (int Width, int Height) GetSize(string folder, ImageWidget image)
    {
      var referenceImagePath = image.ReferencePath ?? (image.Path as Const<string>)?.Value;
      if (referenceImagePath == null)
        throw new Exception("Image must have constant path or define a reference path");
      return GetSize(folder, referenceImagePath);
    }

    public static (int Width, int Height) GetSize(string folder, string path)
    {
      var imagePath = Path.Combine(folder, path);
      var imageData =
        (from directory in ImageMetadataReader.ReadMetadata(imagePath)
          from tag in directory.Tags
          select (directory.Name, tag.Name, tag.Description)).ToArray();
      var width = GetIntValue(imageData, "Image Width");
      var height = GetIntValue(imageData, "Image Height");
      return (width, height);
    }

    private static int GetIntValue(IEnumerable<(string, string, string Description)> imageData, string key)
      => int.Parse(imageData.First(x => x.Item2 == key).Description);

    public IEnumerable<Feed> FindFeeds(Widget widget) => new[] { ((ImageWidget)widget).Path };
    
  }
}