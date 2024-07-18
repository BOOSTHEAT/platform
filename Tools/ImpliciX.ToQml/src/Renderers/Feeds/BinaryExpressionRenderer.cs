using System;
using System.Collections.Generic;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public abstract class BinaryExpressionRenderer : IRenderFeed
  {
    private readonly Dictionary<Type, IRenderFeed> _renderers2;

    protected BinaryExpressionRenderer(Dictionary<Type, IRenderFeed> renderers2)
    {
      _renderers2 = renderers2;
    }

    public string Id(Feed feed) =>
      Proceed2(feed, (r, f) => r.Id(f), OperatorName, "_");
    
    private string Proceed2(Feed feed, Func<IRenderFeed,Feed,string> getOperand, Func<string> getOperator, string separator)
    {
      var be = (BinaryExpression)feed;
      var leftRenderer = _renderers2.GetRenderer(be.Left);
      var rightRenderer = _renderers2.GetRenderer(be.Right);
      return $"{getOperand(leftRenderer,be.Left)}{separator}{getOperator()}{separator}{getOperand(rightRenderer,be.Right)}";
    }

    protected abstract string Operator();
    protected abstract string OperatorName();

    public string Declare(FeedUse feedUse) =>
      $@"property var {Id(feedUse.Feed)}: {GetValueOf(feedUse)}";

    public string GetValueOf(FeedUse feedUse) =>
      Proceed2(feedUse.Feed, (r, f) => r.GetValueOf(feedUse.With(f)), Operator, " ");

    public string SetValueOf(FeedUse feedUse, string value) =>
      throw new NotSupportedException("Expression feed cannot be assigned");
  }
}