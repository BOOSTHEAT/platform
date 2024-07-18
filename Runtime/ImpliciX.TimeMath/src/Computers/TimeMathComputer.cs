using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers;

internal interface ITimeMathComputer
{
  public static readonly TimeSpan KeepLatestOnly = TimeSpan.Zero;
  PropertyUrn<MetricValue> RootUrn { get; }

  void Update(
    TimeSpan at
  );

  void Update(
    IDataModelValue updateValue
  );

  Option<Property<MetricValue>[]> Publish(
    TimeSpan now
  );

  bool IsPublishTimePassed(
    TimeSpan restartAt,
    TimeSpan period
  );
}

internal abstract class TimeMathComputer : ITimeMathComputer
{
  private const string SamplingEndAt = "$samplingEndAt";

  private static readonly FloatValueAt Zero = new (
    TimeSpan.Zero,
    0
  );

  private readonly Dictionary<string, SubTimeMathComputer>
    _subComputers;

  private readonly ITimeMathReader _timeMathReader;
  private readonly ITimeMathWriter _timeMathWriter;
  private readonly Option<TimeSpan> _windowRetention;

  protected TimeMathComputer(
    PropertyUrn<MetricValue> rootUrn,
    ITimeMathReader timeMathReader,
    ITimeMathWriter timeMathWriter,
    Option<TimeSpan> windowRetention,
    TimeSpan start,
    Dictionary<string, SubTimeMathComputer> subComputers
  )
  {
    RootUrn = rootUrn ?? throw new ArgumentNullException(nameof(rootUrn));
    _timeMathReader = timeMathReader ?? throw new ArgumentNullException(nameof(timeMathReader));
    _timeMathWriter = timeMathWriter ?? throw new ArgumentNullException(nameof(timeMathWriter));
    _windowRetention = windowRetention;
    _subComputers = subComputers ?? throw new ArgumentNullException(nameof(subComputers));
    var suffixes = subComputers.Keys.ToArray();
    _timeMathWriter.SetupTimeSeries(
      rootUrn,
      suffixes,
      windowRetention.GetValueOrDefault(ITimeMathComputer.KeepLatestOnly)
    );
  }

  public PropertyUrn<MetricValue> RootUrn { get; }

  public virtual void Update(
    TimeSpan at
  )
  {
    _timeMathWriter.UpdateEndAt(
      RootUrn,
      at
    );
  }

  public virtual void Update(
    IDataModelValue updateValue
  )
  {
    var at = updateValue.At;
    foreach (var (suffix, subComputer) in _subComputers)
    {
      var value = subComputer.ComputeValueToStore(
        RootUrn,
        _timeMathReader,
        updateValue
      );

      _timeMathWriter.UpdateLastMetric(
        RootUrn,
        suffix,
        at,
        value
      );
    }

    Update(at);
  }

  public virtual Option<Property<MetricValue>[]> Publish(
    TimeSpan now
  )
  {
    var start = _timeMathReader.ReadStartAt(RootUrn);
    var end = _timeMathReader.ReadEndAt(RootUrn);
    var valuesBySuffix =
        _subComputers
          .ToDictionary(
            pair =>
              pair.Key,
            pair =>
              _timeMathReader.ReadLastUpdate(
                RootUrn,
                pair.Key
              )
          )
          .Where(pair => pair.Value.IsSome)
          .ToDictionary(
            pair =>
              pair.Key,
            pair =>
              pair.Value.GetValue()
          )
      ;

    var res = Option<Property<MetricValue>[]>.None();
    if (valuesBySuffix.IsEmpty())
      valuesBySuffix =
        _subComputers
          .ToDictionary(
            pair =>
              pair.Key,
            pair =>
              pair.Value.GetDefaultValue()
          )
          .Where(pair => pair.Value.IsSome)
          .ToDictionary(
            pair =>
              pair.Key,
            pair =>
              pair.Value.GetValue()
          );
    if (!valuesBySuffix.IsEmpty())
    {
      var properties = valuesBySuffix.Select(
            pair => Property<MetricValue>.Create(
              new MetricUrn(OutputUrn(pair.Key)),
              new MetricValue(
                ComputeValueToPublish(
                  now,
                  pair.Key,
                  pair.Value
                ).Value
                ,
                start,
                end
              ),
              now
            )
          )
          .ToArray()
        ;
      valuesBySuffix.ForEach(
        pair =>
          _timeMathWriter.AddValueAtPublish(
            RootUrn,
            pair.Key,
            now,
            pair.Value.Value
          )
      );
      res = Option<Property<MetricValue>[]>.Some(properties);
    }

    _timeMathWriter.UpdateStartAt(
      RootUrn,
      Start(now)
    );

    return res;
  }

  public bool IsPublishTimePassed(
    TimeSpan restartAt,
    TimeSpan period
  )
  {
    var lastPublishedValue = _timeMathReader.ReadLastPublishedInstant(RootUrn).GetValueOrDefault(TimeSpan.Zero);

    return restartAt > lastPublishedValue + period;
  }

  private FloatValueAt ComputeValueToPublish(
    TimeSpan now,
    string suffix,
    FloatValueAt value
  )
  {
    var valueToPublish = _subComputers[suffix]
      .ComputeValueToPublish(
        RootUrn,
        _timeMathReader,
        Start(now),
        now,
        value
      );

    return valueToPublish;
  }

  private TimeSpan Start(
    TimeSpan now
  )
  {
    return _windowRetention.Match(
      () => now,
      winPeriod => now < winPeriod
        ? TimeSpan.Zero
        : now - winPeriod
    );
  }

  private string OutputUrn(
    string suffix
  )
  {
    return suffix == ""
      ? RootUrn
      : MetricUrn.Build(
        RootUrn,
        suffix
      );
  }
}
