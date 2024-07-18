using System;
using System.Collections.Generic;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class GreaterThanRenderer : BinaryExpressionRenderer
  {
    public GreaterThanRenderer(Dictionary<Type, IRenderFeed> renderers2) : base(renderers2)
    {
    }

    protected override string Operator() => ">";
    protected override string OperatorName() => "is_greater_than";
  }
}