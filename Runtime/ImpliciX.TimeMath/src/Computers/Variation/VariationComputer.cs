using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers.Variation;

internal sealed class VariationComputer : VariationComputeBase
{
  public VariationComputer(
    PropertyUrn<MetricValue> outputUrn,
    ITimeMathWriter timeMathWriter,
    ITimeMathReader timeMathReader,
    TimeSpan start
  ) : base(outputUrn, timeMathWriter, timeMathReader, start)
  {
  }

  protected override void OnNewPeriodStarting(TimeSpan publishAt) => StartNewPeriod(publishAt);

  private void StartNewPeriod(TimeSpan now)
  {
    Store.LastValue.Tap(last => Store.FirstValue = last);
    Store.LastValue = Option<float>.None();
    Store.SamplingStartAt = now;
    Store.SamplingEndAt = now;
  }
}