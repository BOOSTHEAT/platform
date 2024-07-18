using System;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Dumb
{
  public class FakeMeasureValue<T> : DataModelValue<T>
  {
    public FakeMeasureValue(string urn, T measure, TimeSpan at) : base(urn, measure, at)
    {
    }
  }
}