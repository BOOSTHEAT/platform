using System;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.FeedsRendering;

public class ConstRenderingTests
{
  [TestCase(2.3f, "2.3", "2_3")]
  [TestCase(-2.3f, "-2.3", "-2_3")]
  [TestCase(0f, "0", "0")]
  public void Float(float input, string expectedValue, string expectedName)
  {
    Check(input, expectedValue, expectedValue, expectedName);
  }

  [TestCase(2.3, "2.3", "2_3")]
  [TestCase(-2.3, "-2.3", "-2_3")]
  [TestCase(0.0, "0", "0")]
  public void Double(double input, string expectedValue, string expectedName)
  {
    Check(input, expectedValue, expectedValue, expectedName);
  }

  [TestCase("foo", "\"foo\"")]
  [TestCase("", "\"\"")]
  public void String(string input, string expectedValue)
  {
    Check(input, expectedValue, expectedValue, input);
  }

  [TestCase("foo", "translate(\"foo\")", "root.translate(\"foo\")")]
  [TestCase("", "translate(\"\")", "root.translate(\"\")")]
  public void StringTranslation(string input, string expectedValueInCache, string expectedValueOutOfCache)
  {
    CheckFeed(Const.IsTranslate(input), expectedValueInCache, expectedValueOutOfCache, input);
  }

  public enum Whatever
  {
    Foo = 28,
    Bar = 34
  }
  [TestCase(Whatever.Foo, "28", "Foo")]
  [TestCase(Whatever.Bar, "34", "Bar")]
  public void Enum(Whatever input, string expectedValue, string expectedName)
  {
    Check(input, expectedValue, expectedValue, expectedName);
  }
  
  enum PrivateWhatever
  {
    Foo = 28,
    Bar = 34
  }
  [Test]
  public void PrivateEnum()
  {
    var feed = Const.Is(PrivateWhatever.Foo);
    var renderer = new ConstRenderer();
    var expectedMessage = "ImpliciX.ToQml.Tests.FeedsRendering.ConstRenderingTests+PrivateWhatever shall be public.";
    Assert.Throws(Has.Message.EqualTo(expectedMessage), () => renderer.Id(feed));
    Assert.Throws(Has.Message.EqualTo(expectedMessage), () => renderer.GetValueOf(feed.InCache()));
  }
  
  private static void Check<T>(T input, string expectedValueInCache, string expectedValueOutOfCache, string expectedName)
  {
    CheckFeed(Const.Is(input), expectedValueInCache, expectedValueOutOfCache, expectedName);
  }
  
  private static void CheckFeed(Feed feed, string expectedValueInCache, string expectedValueOutOfCache, string expectedName)
  {
    var renderer = new ConstRenderer();
    Assert.That(renderer.Id(feed), Is.EqualTo(expectedName));
    var inCache = feed.InCache();
    Assert.That(renderer.Declare(inCache), Is.EqualTo(string.Empty));
    Assert.That(renderer.GetValueOf(inCache), Is.EqualTo(expectedValueInCache));
    Assert.Throws<NotSupportedException>(() => renderer.SetValueOf(inCache, "whatever"));
    var outOfCache = feed.OutOfCache("root");
    Assert.That(renderer.Declare(outOfCache), Is.EqualTo(string.Empty));
    Assert.That(renderer.GetValueOf(outOfCache), Is.EqualTo(expectedValueOutOfCache));
    Assert.Throws<NotSupportedException>(() => renderer.SetValueOf(outOfCache, "whatever"));
  }

}