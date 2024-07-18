
using System;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class NowRenderer : IRenderFeed
  {
    public string Id(Feed feed) => "now";
    public string Declare(FeedUse feedUse) => string.Empty;

    public string GetValueOf(FeedUse feedUse)
    {
      var mainVar = feedUse.IsLocalSettings ? "nowLocal" : "now";
      return feedUse.PrependCacheIfNeeded(feedUse.IsFormatted
        ? $"{mainVar}.format('{((NowFeed)feedUse.Feed).Format}')"
        : mainVar);
    }

    public string SetValueOf(FeedUse feedUse, string value) =>
      throw new NotSupportedException("Current date/time feed cannot be assigned. This could change in the future for testing purpose.");
  }
}