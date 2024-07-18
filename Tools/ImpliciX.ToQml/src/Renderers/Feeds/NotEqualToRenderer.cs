using System;
using System.Collections.Generic;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class NotEqualToRenderer : BinaryExpressionRenderer
  {
    public NotEqualToRenderer(Dictionary<Type, IRenderFeed> renderers2) : base(renderers2)
    {
    }

    protected override string Operator() => "!=";
    protected override string OperatorName() => "is_not_equal_to";
  }
}