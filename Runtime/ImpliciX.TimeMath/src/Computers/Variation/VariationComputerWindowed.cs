using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers.Variation;

internal sealed class VariationComputerWindowed : VariationComputeBase
{
  private readonly TimeSpan _windowPeriod;
  private readonly string _windowedValuesStoreKey;
  private readonly TimeSpan _publicationPeriod;

  public VariationComputerWindowed(
    PropertyUrn<MetricValue> outputUrn,
    ITimeMathWriter timeMathWriter,
    ITimeMathReader timeMathReader,
    TimeSpan publicationPeriod,
    TimeSpan windowPeriod,
    TimeSpan now
  ) : base(outputUrn, timeMathWriter, timeMathReader, now)
  {
    _publicationPeriod = publicationPeriod;
    _windowPeriod = windowPeriod;

    _windowedValuesStoreKey = $"{outputUrn}${nameof(_windowedValuesStoreKey)}";
    var windowPeriodExcludingValueOnLeftBound = windowPeriod - TimeSpan.FromMilliseconds(1);
    timeMathWriter.SetupTimeSeries(_windowedValuesStoreKey, windowPeriodExcludingValueOnLeftBound);
  }

  private Option<FloatValueAt> WindowedValuesStore_ReadFirst(TimeSpan? from = null) => TimeMathReader.ReadTsFirst(_windowedValuesStoreKey, from);

  private void WindowedValuesStore_Add(FloatValueAt newValue) => TimeMathWriter.WriteTsAndApplyRetention(_windowedValuesStoreKey, newValue.At, newValue.Value);

  protected override void OnNewPeriodStarting(TimeSpan publishAt) => ManageWindow(publishAt);

  private void ManageWindow(TimeSpan publishAt)
  {
    Store.LastValue.Tap(last => WindowedValuesStore_Add(new FloatValueAt(publishAt, last)));

    var nowSynchronizedWithPublicationPeriod = publishAt - TimeSpan.FromTicks(publishAt.Ticks % _publicationPeriod.Ticks);
    var nextPublishWillBeAt = nowSynchronizedWithPublicationPeriod + _publicationPeriod;

    var samplingStart = Store.SamplingStartAt.GetValue();
    var itIsTimeToSlideTheWindow = nextPublishWillBeAt - samplingStart > _windowPeriod;

    if (!itIsTimeToSlideTheWindow) return;

    var minValueForNewSamplingStart = nextPublishWillBeAt - _windowPeriod;
    var newOldestWindowPoint = WindowedValuesStore_ReadFirst(minValueForNewSamplingStart).GetValue();

    Store.FirstValue = newOldestWindowPoint.Value;
    Store.SamplingStartAt = newOldestWindowPoint.At;
  }
}