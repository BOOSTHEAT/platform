using System;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class NowRenderingTests
{
  [TestCase("dddd", "nowLocal", "root.nowLocal")]
  [TestCase("LTS", "nowLocal", "root.nowLocal")]
  public void LocalRawValue(string formatting, string expectedInCache, string expectedOutOfCache)
  {
    var renderer = new NowRenderer();
    var feed = new NowFeed(formatting);
    Assert.That(renderer.GetValueOf(feed.InCache().UseLocalSettings.RawValue), Is.EqualTo(expectedInCache));
    Assert.That(renderer.GetValueOf(feed.OutOfCache("root").UseLocalSettings.RawValue), Is.EqualTo(expectedOutOfCache));
  }

  [TestCase("dddd", "nowLocal.format('dddd')", "root.nowLocal.format('dddd')")]
  [TestCase("LTS", "nowLocal.format('LTS')", "root.nowLocal.format('LTS')")]
  public void LocalFormatted(string formatting, string expectedInCache, string expectedOutOfCache)
  {
    var renderer = new NowRenderer();
    var feed = new NowFeed(formatting);
    Assert.That(renderer.GetValueOf(feed.InCache().UseLocalSettings.Formatted), Is.EqualTo(expectedInCache));
    Assert.That(renderer.GetValueOf(feed.OutOfCache("root").UseLocalSettings.Formatted), Is.EqualTo(expectedOutOfCache));
  }

  [TestCase("dddd", "now", "root.now")]
  [TestCase("LTS", "now", "root.now")]
  public void NeutralRawValue(string formatting, string expectedInCache, string expectedOutOfCache)
  {
    var renderer = new NowRenderer();
    var feed = new NowFeed(formatting);
    Assert.That(renderer.GetValueOf(feed.InCache().UseNeutralSettings.RawValue), Is.EqualTo(expectedInCache));
    Assert.That(renderer.GetValueOf(feed.OutOfCache("root").UseNeutralSettings.RawValue), Is.EqualTo(expectedOutOfCache));
  }

  [TestCase("dddd", "now.format('dddd')", "root.now.format('dddd')")]
  [TestCase("LTS", "now.format('LTS')", "root.now.format('LTS')")]
  public void NeutralFormatted(string formatting, string expectedInCache, string expectedOutOfCache)
  {
    var renderer = new NowRenderer();
    var feed = new NowFeed(formatting);
    Assert.That(renderer.GetValueOf(feed.InCache().UseNeutralSettings.Formatted), Is.EqualTo(expectedInCache));
    Assert.That(renderer.GetValueOf(feed.OutOfCache("root").UseNeutralSettings.Formatted), Is.EqualTo(expectedOutOfCache));
  }
}