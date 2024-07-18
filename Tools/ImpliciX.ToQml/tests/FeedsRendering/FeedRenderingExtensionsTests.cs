using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class FeedRenderingExtensionsTests
{
  private static TestCaseData[] _feedsExpectations = new[]
  {
    new TestCaseData(typeof(Feed1),typeof(Feed1)),
    new TestCaseData(typeof(Feed2),typeof(Feed2)),
    new TestCaseData(typeof(GenFeed2<int>), typeof(Feed2)),
  };
  
  [TestCaseSource(nameof(_feedsExpectations))]
  public void ExpectedRendererIsUsedForDeclare(Type inputFeedType, Type outputFeedType)
  {
    var feed = (Feed)Activator.CreateInstance(inputFeedType);
    Assert.That(_renderers.Declare(feed.OutOfCache("foo")), Is.EqualTo($"Declare_{outputFeedType.Name}_foo"));
  }

  [TestCaseSource(nameof(_feedsExpectations))]
  public void ExpectedRendererIsUsedForDisplay(Type inputFeedType, Type outputFeedType)
  {
    var feed = (Feed)Activator.CreateInstance(inputFeedType);
    Assert.That(_renderers.GetValueOf(feed.OutOfCache("foo").Formatted), Is.EqualTo($"ValueOf_{outputFeedType.Name}_foo_Formatted"));
  }

  [TestCaseSource(nameof(_feedsExpectations))]
  public void ExpectedRendererIsUsedForValueOf(Type inputFeedType, Type outputFeedType)
  {
    var feed = (Feed)Activator.CreateInstance(inputFeedType);
    Assert.That(_renderers.GetValueOf(feed.OutOfCache("foo").RawValue), Is.EqualTo($"ValueOf_{outputFeedType.Name}_foo_RawValue"));
  }

  [TestCaseSource(nameof(_feedsExpectations))]
  public void ExpectedRendererIsUsedForNameOf(Type inputFeedType, Type outputFeedType)
  {
    var feed = (Feed)Activator.CreateInstance(inputFeedType);
    Assert.That(_renderers.Id(feed), Is.EqualTo($"Id_{outputFeedType.Name}"));
  }
  
  [Test]
  public void NoHarmWhenFeedIsNull()
  {
    Assert.That(_renderers.Declare(null), Is.EqualTo(""));
    Assert.That(_renderers.GetValueOf(null), Is.EqualTo("undefined"));
    Assert.That(_renderers.GetValueOf(null), Is.EqualTo("undefined"));
    Assert.That(_renderers.Id(null), Is.EqualTo(""));
  }
  
  class GenFeed2<Z> : Feed2 {}
  private static Dictionary<Type, IRenderFeed> _renderers = new Dictionary<Type, IRenderFeed>
  {
    [typeof(Feed1)] = new FeedRenderingStub<Feed1>(),
    [typeof(Feed2)] = new FeedRenderingStub<Feed2>()
  };
}