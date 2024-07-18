using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers;

internal class GaugeComputer : TimeMathComputer
{
  private static readonly Dictionary<string, SubTimeMathComputer> SubComputers = new()
  {
    { "", new SubGaugeComputer() }
  };

  public GaugeComputer(
    PropertyUrn<MetricValue> outputUrn,
    ITimeMathWriter timeMathWriter,
    ITimeMathReader timeMathReader,
    TimeSpan start
  ) : base(
    outputUrn,
    timeMathReader,
    timeMathWriter,
    ITimeMathComputer.KeepLatestOnly,
    start,
    SubComputers
  )
  {
  }

  private class SubGaugeComputer : SubTimeMathComputer
  {
    public override float ComputeValueToStore(
      Urn rootUrn,
      ITimeMathReader timeMathReader,
      IDataModelValue updateValue
    )
    {
      return updateValue.ToFloat().Value;
    }

    public override Option<FloatValueAt> GetDefaultValue()
    {
      return Option<FloatValueAt>.None();
    }
  }
}
