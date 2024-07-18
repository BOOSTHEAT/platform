using System;
using System.Collections.Generic;

namespace ImpliciX.ToQml.Renderers.Feeds
{
  public class EqualToRenderer : BinaryExpressionRenderer
  {
    public EqualToRenderer(Dictionary<Type, IRenderFeed> renderers2) : base(renderers2)
    {
    }

    protected override string Operator() => "==";
    protected override string OperatorName() => "is_equal_to";
  }
}