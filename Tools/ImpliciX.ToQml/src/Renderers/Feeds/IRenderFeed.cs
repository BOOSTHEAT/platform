using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public interface IRenderFeed
  {
    /// <summary>
    /// Feed identifier used for equality comparison
    /// </summary>
    string Id(Feed feed);

    /// <summary>
    /// QML Code for feed declaration in global QML App Cache
    /// </summary>
    string Declare(FeedUse feedUse);

    /// <summary>
    /// QML Code to get feed value according to options
    /// </summary>
    string GetValueOf(FeedUse feedUse);

    /// <summary>
    /// QML Code to set feed value according to options
    /// </summary>
    string SetValueOf(FeedUse feedUse, string value);
  }

  
  public record FeedUse
  {
    public Feed Feed { get; private set; }
    public Option<string> Cache { get; }
    public bool IsLocalSettings { get; private set; }
    public bool IsFormatted { get; private set; }

    public FeedUse(Feed feed) { Feed = feed; Cache = Option<string>.None(); }
    public FeedUse(Feed feed, string cache) { Feed = feed; Cache = cache; }
    public FeedUse UseNeutralSettings => this with { IsLocalSettings = false };
    public FeedUse UseLocalSettings => this with { IsLocalSettings = true };
    public FeedUse RawValue => this with { IsFormatted = false };
    
    public FeedUse Formatted => this with { IsFormatted = true };
    public FeedUse With(Feed f) => this with { Feed = f };
    
    public string PrependCacheIfNeeded(string call) => Cache.IsSome ? $"{Cache.GetValue()}.{call}" : call;
  }

  public static class RenderFeedExtensions
  {
    public static string Id(this Dictionary<Type, IRenderFeed> renderers, Feed feed)
      => renderers.GetRenderer(feed).Id(feed);
    public static string Declare(this Dictionary<Type, IRenderFeed> renderers, FeedUse feedUse)
      => renderers.GetRenderer(feedUse?.Feed).Declare(feedUse);
    public static string GetValueOf(this Dictionary<Type, IRenderFeed> renderers, FeedUse feedUse)
      => renderers.GetRenderer(feedUse?.Feed).GetValueOf(feedUse);
    public static string SetValueOf(this Dictionary<Type, IRenderFeed> renderers, FeedUse feedUse, string value)
      => renderers.GetRenderer(feedUse?.Feed).SetValueOf(feedUse, value);

    public static FeedUse InCache(this Feed feed) => new (feed);
    public static FeedUse OutOfCache(this Feed feed, string cache) => new (feed, cache);
    
    class UndefinedRenderer : IRenderFeed
    {
      public string Id(Feed feed) => "";
      public string Declare(FeedUse feedUse) => "";
      public string GetValueOf(FeedUse feedUse) => "undefined";
      public string SetValueOf(FeedUse feedUse, string value) => "";
    }

    public static IRenderFeed GetRenderer(this Dictionary<Type, IRenderFeed> renderers, Feed feed)
    {
      if (feed == null)
        return new UndefinedRenderer();
      var actualType = feed.GetType();
      if(!actualType.IsGenericType)
        return renderers[actualType];
      return renderers[actualType.BaseType!];
    }
  }
  
  class FeedEqualityComparer : EqualityComparer<Feed>
  {
    private readonly Dictionary<Type, IRenderFeed> _renderers;
  
    public FeedEqualityComparer(Dictionary<Type, IRenderFeed> renderers)
    {
      _renderers = renderers;
    }
    public override bool Equals(Feed x, Feed y)
      => y != null && x != null && _renderers.Id(x) == _renderers.Id(y);
    public override int GetHashCode(Feed obj) => _renderers.Id(obj).GetHashCode();
  }
  
  
}