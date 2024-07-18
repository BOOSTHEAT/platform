using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

class FeedStub<T> : Feed {}
class Feed1 : FeedStub<Feed1> {}
class Feed2 : FeedStub<Feed2> {}
class FeedRenderingStub<T> : IRenderFeed where T : FeedStub<T>
{
  public string Name { get; } = typeof(T).Name;
  private string IdOf(Feed feed)
  {
    Assert.That(feed, Is.InstanceOf<T>());
    return Name;
  }

  public string Id(Feed feed) => $"Id_{IdOf(feed)}";

  public string Declare(FeedUse feedUse) =>
    feedUse.Cache.IsSome
      ? $"Declare_{IdOf(feedUse.Feed)}_{feedUse.Cache.GetValue()}"
      : $"Declare_{IdOf(feedUse.Feed)}";
  
  public string GetValueOf(FeedUse feedUse) =>
    feedUse.Cache.IsSome
      ? $"ValueOf_{IdOf(feedUse.Feed)}_{feedUse.Cache.GetValue()}_{(feedUse.IsFormatted?"Formatted":"RawValue")}"
      : $"ValueOf_{IdOf(feedUse.Feed)}_{(feedUse.IsFormatted?"Formatted":"RawValue")}";

  public string SetValueOf(FeedUse feedUse, string value)
  {
    throw new System.NotImplementedException();
  }
}