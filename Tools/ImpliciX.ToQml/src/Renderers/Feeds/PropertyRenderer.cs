using System;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class PropertyRenderer : IRenderFeed
  {
    public static string Encode(Urn urn) => urn.Value.Replace(":", "$");
    
    public string Id(Feed feed) => Encode(((PropertyFeed)feed).Urn);

    public string Declare(FeedUse feedUse) =>
      $@"property ModelProperty {Id(feedUse.Feed)}: ModelProperty {{}}";

    public string GetValueOf(FeedUse feedUse) =>
      feedUse.PrependCacheIfNeeded(Id(feedUse.Feed)) + (feedUse.IsFormatted ? ".display()" : ".value");

    public string SetValueOf(FeedUse feedUse, string value) =>
      $"{feedUse.PrependCacheIfNeeded(Id(feedUse.Feed))}.value = {value};";
  }
}