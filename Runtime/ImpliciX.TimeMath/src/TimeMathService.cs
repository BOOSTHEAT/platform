using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Computers;

namespace ImpliciX.TimeMath;

internal sealed class TimeMathService //TODO : use Data to define computer metrics postfix URN base one Type
{
  private readonly Func<TimeSpan> _now;

  private readonly List<ITimeMathComputer> _timeMathComputers = new();
  private Dictionary<Urn, ITimeMathComputer[]> _computersByInputUrn;
  private Dictionary<TimeSpan, ITimeMathComputer[]> _computersByPublicationIntervals;
  private ITimeMathReader? _timeMathReader;
  private ITimeMathWriter? _timeMathWriter;

  public TimeMathService(
    Func<TimeSpan> now
  )
  {
    _now = now ?? throw new ArgumentNullException(nameof(now));
    _computersByPublicationIntervals = new Dictionary<TimeSpan, ITimeMathComputer[]>();
    _computersByInputUrn = new Dictionary<Urn, ITimeMathComputer[]>();
  }

  public PropertiesChanged[] Initialize(
    MetricInfoSet metricInfoSet,
    ITimeMathWriter timeMathWriter,
    ITimeMathReader timeMathReader
  )
  {
    if (metricInfoSet == null) throw new ArgumentNullException(nameof(metricInfoSet));
    _timeMathWriter = timeMathWriter ?? throw new ArgumentNullException(nameof(timeMathWriter));
    _timeMathReader = timeMathReader ?? throw new ArgumentNullException(nameof(timeMathReader));
    var metricComputerFactory = new TimeMathComputerFactory(
      _timeMathWriter,
      _timeMathReader
    );

    _timeMathComputers.Clear();

    metricInfoSet
      .GetMetricInfos<IMetricWindowableInfo>()
      .ForEach(AssumeWindowRetentionIsValid);

    var computerRuntimeInfos =
      metricInfoSet
        .GetMetricInfos()
        .SelectMany(
          metric => metricComputerFactory.Create(
            metric,
            _now()
          )
        )
        .ToArray();

    computerRuntimeInfos.ForEach(info => _timeMathComputers.Add(info.Computer));

    _computersByPublicationIntervals =
      (
        from info
          in computerRuntimeInfos
        group info.Computer
          by info.PublicationPeriod
      )
      .ToDictionary(
        pair => pair.Key,
        pair => pair.ToArray()
      );

    _computersByInputUrn =
      (
        from info
          in computerRuntimeInfos
        let computer = info.Computer
        from urn in info.TriggerUrns
        group computer
          by urn
      )
      .ToDictionary(
        pair => pair.Key,
        pair => pair.ToArray()
      );

    var res = _computersByPublicationIntervals
        .SelectMany(
          pair => pair.Value,
          (
            pair,
            computer
          ) => new { pair.Key, computer }
        )
        .Where(
          pair =>
            pair.computer.IsPublishTimePassed(
              _now.Invoke(),
              pair.Key
            )
        )
        .Select(arg => arg.computer)
        .ToDictionary(
          computer =>
            computer.RootUrn,
          computer =>
            computer.Publish(_now.Invoke())
        )
        .Where(
          pair =>
            pair.Value.IsSome
        )
        .ToDictionary(
          pair =>
            pair.Key,
          pair =>
            pair.Value.GetValue()
        )
        .Select(
          p =>
            PropertiesChanged.Create(
              p.Key,
              p.Value,
              _now.Invoke()
            )
        )
        .ToArray()
      ;

    //TODO: update time for next publish
    return res;
  }

  internal ITimeMathComputer[] ComputersByPublicationIntervals(
    TimeSpan publicationInterval
  )
  {
    return _computersByPublicationIntervals.TryGetValue(
      publicationInterval,
      out var intervals
    )
      ? intervals
      : Array.Empty<ITimeMathComputer>();
  }

  internal static void AssumeWindowRetentionIsValid(
    IMetricWindowableInfo metric
  )
  {
    metric.WindowRetention.Tap(
      retention =>
      {
        if (retention <= metric.PublicationPeriod)
          throw new InvalidOperationException(
            "Window period of Metric must be greater than primary publication period"
          );

        if (retention.Ticks % metric.PublicationPeriod.Ticks != 0)
          throw new InvalidOperationException(
            "Window period of Metric must be a multiplier of the primary publication period"
          );
      }
    );
  }

  public DomainEvent[] HandlePropertiesChanged(
    PropertiesChanged trigger
  )
  {
    trigger.ModelValues.ForEach(
      modelValue =>
        _computersByInputUrn.GetValueOrDefault(
            modelValue.Urn,
            Array.Empty<ITimeMathComputer>()
          )
          .ForEach(
            computer =>
              computer.Update(modelValue)
          )
    );

    return Array.Empty<DomainEvent>();
  }

  public DomainEvent[] HandleSystemTicked(
    SystemTicked trigger
  )
  {
    if (trigger == null) throw new ArgumentNullException(nameof(trigger));

    UpdateAllMetrics(trigger.At);

    var events =
        _computersByPublicationIntervals
          .Where(pair => trigger.IsNextDate(pair.Key))
          .SelectMany(
            pair => PublishedMetrics(
              pair.Value,
              trigger.At
            )
          )
          .ToArray()
      ;

    return events;
  }

  private IEnumerable<DomainEvent> PublishedMetrics(
    ITimeMathComputer[] computers,
    TimeSpan at
  )
  {
    Log.Debug(
      "Publish timeMathService.",
      at
    );
    var publishersOutcome =
      from outcome in
        from computer in computers
        select (computer.RootUrn, ComputerPublishedValues: computer.Publish(at))
      where outcome.ComputerPublishedValues.IsSome
      select (outcome.RootUrn, PublishedValues: outcome.ComputerPublishedValues.GetValue());

    return publishersOutcome.Select(
      p =>
        PropertiesChanged.Create(
          p.RootUrn,
          p.PublishedValues,
          at
        )
    );
  }

  private void UpdateAllMetrics(
    TimeSpan at
  )
  {
    _timeMathComputers
      .ForEach(
        computer =>
          computer.Update(at)
      );
  }
}
