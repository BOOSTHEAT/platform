using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Tests.Helpers;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class FindFeedsInWidgetsTests
{
  [Test]
  public void TextWidgets()
  {
    var feed = CreateFeed<Literal>();
    var widget = new Text {Value = feed};
    var actual = GetFeedsFor(widget);
    Assert.That(actual, Is.EqualTo(new[] {feed}));
  }

  [Test]
  public void IncrementWidgets()
  {
    var visualFeed = CreateFeed<Literal>();
    var incrementFeed = CreateFeed<Flow>();
    var widget = new IncrementWidget
    {
      InputUrn = incrementFeed,
      Visual = new Text {Value = visualFeed}
    };

    var actual = GetFeedsFor(widget);
    Assert.That(actual, Is.EqualTo(new[] {incrementFeed, visualFeed}));
  }

  [Test]
  public void SendWidgets()
  {
    var visualFeed = CreateFeed<Literal>();
    var widget = new SendWidget
    {
      Visual = new Text {Value = visualFeed}
    };

    var actual = GetFeedsFor(widget);
    Assert.That(actual, Is.EqualTo(new[] {visualFeed}));
  }

  class MyBinaryExpression : BinaryExpression
  {
  }

  [Test]
  public void SwitchWidgets()
  {
    var feeds = Enumerable.Range(0, 7).Select(_ => CreateFeed<Literal>()).ToArray();
    var widget = new SwitchWidget
    {
      Cases = new[]
      {
        new SwitchWidget.Case
        {
          When = new MyBinaryExpression {Left = feeds[1], Right = feeds[2]},
          Then = new Text {Value = feeds[0]}
        },
        new SwitchWidget.Case
        {
          When = new MyBinaryExpression {Left = feeds[4], Right = feeds[5]},
          Then = new Text {Value = feeds[3]}
        }
      },
      Default = new Text {Value = feeds[6]}
    };

    var actual = GetFeedsFor(widget).ToArray();
    Assert.That(actual, Is.EqualTo(feeds));
  }

  private static IEnumerable<Feed> GetFeedsFor(Widget widget)
  {
    var rendering = new QmlRenderer(new DirectoryInfo("/"), new NullCopyrightManager());
    var renderer = rendering.WidgetRenderers.GetRenderer(widget);
    return renderer.FindFeeds(widget);
  }

  private static Feed CreateFeed<T>() => PropertyFeed.Subscribe(PropertyUrn<T>.Build());
}