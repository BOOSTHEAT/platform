using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers.Variation;

internal abstract class VariationComputeBase : ITimeMathComputer
{
  private string _lastDeltaPublishedAtKey;

  private Option<TimeSpan> _lastDeltaPublishedAt
  {
    get => TimeMathReader.ReadTsLast(_lastDeltaPublishedAtKey).Map(floatAt => floatAt.At);
    set => TimeMathWriter.WriteTsAndApplyRetention(_lastDeltaPublishedAtKey, value.GetValue(), 0);
  }

  private Option<TimeSpan> _wasShutdownAt = Option<TimeSpan>.None();
  protected ITimeMathWriter TimeMathWriter { get; }
  protected ITimeMathReader TimeMathReader { get; }
  protected VariationStore Store { get; }
  public PropertyUrn<MetricValue> RootUrn { get; }

  protected VariationComputeBase(
    PropertyUrn<MetricValue> outputUrn,
    ITimeMathWriter timeMathWriter,
    ITimeMathReader timeMathReader,
    TimeSpan now)
  {
    RootUrn = outputUrn ?? throw new ArgumentNullException(nameof(outputUrn));
    TimeMathWriter = timeMathWriter ?? throw new ArgumentNullException(nameof(timeMathWriter));
    TimeMathReader = timeMathReader ?? throw new ArgumentNullException(nameof(timeMathReader));

    _lastDeltaPublishedAtKey = $"{outputUrn}${nameof(_lastDeltaPublishedAtKey)}";
    timeMathWriter.SetupTimeSeries(_lastDeltaPublishedAtKey, StoreBase.KeepLatestOnly);
    Store = new VariationStore(outputUrn, timeMathReader, timeMathWriter);

    if (Store.SamplingStartAt.IsNone)
      Store.SamplingStartAt = now;

    ApplyStartupRules(now);
  }

  private void ApplyStartupRules(TimeSpan now)
  {
    var computerHasRestarted = now > Store.SamplingStartAt.GetValue();
    if (computerHasRestarted)
      _wasShutdownAt = Store.SamplingEndAt;
  }

  public void Update(TimeSpan at) => Store.SamplingEndAt = at;

  public void Update(IDataModelValue newValue)
  {
    var l_newValue = newValue.ToFloat().Value;

    Store.FirstValue.Tap(
      () => Store.FirstValue = l_newValue,
      _ => Store.LastValue = l_newValue
    );

    Store.SamplingEndAt = newValue.At;
    _wasShutdownAt = Option<TimeSpan>.None();
  }

  public Option<Property<MetricValue>[]> Publish(TimeSpan publishAt)
  {
    var delta = ComputeCurrentDeltaValue();
    var toPublish = CreatePropertiesToPublish(delta, GetSamplingEndToPublish(), publishAt);

    _lastDeltaPublishedAt = publishAt;
    OnNewPeriodStarting(publishAt);
    return toPublish;
  }

  protected abstract void OnNewPeriodStarting(TimeSpan publishAt);

  private float ComputeCurrentDeltaValue()
    => Store.LastValue.Match(() => 0, last => last - Store.FirstValue.GetValue());

  private Property<MetricValue>[] CreatePropertiesToPublish(float variationValue, TimeSpan samplingEnd, TimeSpan publishAt) => new[]
  {
    Property<MetricValue>.Create(RootUrn, new MetricValue(variationValue, Store.SamplingStartAt.GetValue(), samplingEnd), publishAt)
  };

  private TimeSpan GetSamplingEndToPublish()
  {
    if (_wasShutdownAt.IsNone) return Store.SamplingEndAt;

    var wasShutdownAt = _wasShutdownAt.GetValue();
    _wasShutdownAt = Option<TimeSpan>.None();

    var shutdownMomentWasAPublishTimeWhichWasAlreadyPublished = _lastDeltaPublishedAt.IsSome && _lastDeltaPublishedAt.GetValue() == wasShutdownAt;
    return shutdownMomentWasAPublishTimeWhichWasAlreadyPublished
      ? Store.SamplingEndAt
      : wasShutdownAt;
  }

  public bool IsPublishTimePassed(TimeSpan restartAt, TimeSpan period)
    => _lastDeltaPublishedAt.Match(
      () =>
      {
        var samplingStartAt = Store.SamplingStartAt;
        return samplingStartAt.IsSome && restartAt - samplingStartAt.GetValue() >= period;
      },
      lastDeltaPublished => restartAt > lastDeltaPublished + period
    );
}