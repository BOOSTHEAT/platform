using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers.StateMonitoring;

public class StateMonitoringComputer : ITimeMathComputer
{
  private readonly StateMonitoringInfo _info;
  private StateDigest _currentDigest = null!;
  private TimeSpan _currentPeriodStart = TimeSpan.Zero;
  private TimeSpan _currentStateStart = TimeSpan.Zero;
  private Enum _currentState = null!;
  private readonly WindowProcessor<Dated<StateDigest>> _window;
  private Option<Dated<StateDigest>> _unpublished;
  private TimeSpan _previouslyPublishedGlobalEnd;
  private readonly IStateMonitoringPersistence _persistence;

  public StateMonitoringComputer(StateMonitoringInfo info, ITimeMathWriter writer, ITimeMathReader reader)
  : this(info, new StateMonitoringPersistence(info, writer, reader))
  {
  }

  internal StateMonitoringComputer(StateMonitoringInfo info, IStateMonitoringPersistence persistence)
  {
    _persistence = persistence;
    _info = info;
    RootUrn = info.RootUrn;
    _window = new WindowProcessor<Dated<StateDigest>>(_persistence.WindowSize);
    _unpublished = _persistence.ReadUnpublished();
  }

  public PropertyUrn<MetricValue> RootUrn { get; }

  public void Update(TimeSpan at)
  {
  }

  public void Update(IDataModelValue updateValue)
  {
    var now = updateValue.At;
    UpdateCountersAccordingToCurrentState(now,true);
    if (_currentState == null! && _currentDigest.IsNeutral)
      _currentDigest = StateDigest.Hole;
    _currentState = (Enum)updateValue.ModelValue();
    _currentStateStart = now;
    _unpublished = Option<Dated<StateDigest>>.None();
  }

  public Option<Property<MetricValue>[]> Publish(TimeSpan now)
  {
    var properties = _unpublished.Match(
      () =>
      {
        UpdateCountersAccordingToCurrentState(now, false);
        var newChunk = new Dated<StateDigest>(_currentDigest, _currentPeriodStart, now);
        return StoreChunkAndGetResultingPublication(newChunk);
      },
      unpublished =>
      {
        _unpublished = Option<Dated<StateDigest>>.None();
        if (unpublished.Start + _info.PublicationPeriod == now)
          return StoreChunkAndGetResultingPublication(unpublished.Select(sd => sd + StateDigest.Hole));
        if(unpublished.End > _previouslyPublishedGlobalEnd)
          return unpublished.Value
            .Publish(unpublished.Start, unpublished.Start+_info.PublicationPeriod, _info.States).ToArray();
        var newChunk = new Dated<StateDigest>(StateDigest.Neutral, _currentPeriodStart, now);
        return StoreChunkAndGetResultingPublication(newChunk);
      }
    );
    
    ResetCounters(now);
    return Option<Property<MetricValue>[]>.Some(properties);
  }

  private Property<MetricValue>[] StoreChunkAndGetResultingPublication(Dated<StateDigest> chunk)
  {
    _persistence.StorePublished(chunk);
    var (windowContent, removed) = _window.Push(chunk);
    return windowContent.Value.Publish(windowContent.Start, windowContent.End, _info.States).ToArray();
  }


  public bool IsPublishTimePassed(TimeSpan restartAt, TimeSpan period)
  {
    var windowContent = CreateWindowContentFromStoredData(restartAt, _info, _persistence).ToArray();
    foreach (var chunk in windowContent)
      _window.Push(chunk);
    var endTimes = windowContent
        .Where(d => !d.Value.IsNeutral)
        .Select(d => d.End)
        .ToArray();
    _previouslyPublishedGlobalEnd = endTimes.Any() ? endTimes.Max() : TimeSpan.Zero;
    if (_unpublished.IsSome
        && _unpublished.GetValue().Start + period < restartAt
        && _unpublished.GetValue().End > _previouslyPublishedGlobalEnd )
      return true;
    ResetCounters(restartAt);
    return false;
  }

  private void ResetCounters(TimeSpan now)
  {
    _currentPeriodStart = _unpublished.IsSome && _unpublished.GetValue().End > _previouslyPublishedGlobalEnd
      ? _unpublished.GetValue().Start
      : _persistence.PeriodStartJustBefore(now);
    _currentStateStart = now;
    _currentDigest = _unpublished.IsSome
    ? _unpublished.GetValue().Value
    : StateDigest.Neutral;
  }

  private void UpdateCountersAccordingToCurrentState(TimeSpan now, bool storeAsUnpublished)
  {
    if (_currentState == null!)
      return;
    _currentDigest += new StateChange(_currentState, 1, now - _currentStateStart);
    if (storeAsUnpublished)
      _persistence.StoreUnpublished(now, _currentDigest[_currentState], _info.States[_currentState]);
  }
  
  internal static IEnumerable<Dated<StateDigest>> CreateWindowContentFromStoredData(
    TimeSpan startAt, StateMonitoringInfo info, IStateMonitoringPersistence persistence
    )
  {
    if (persistence.WindowSize == 1)
      return Enumerable.Empty<Dated<StateDigest>>();
    var periodStart = persistence.PeriodStartJustBefore(startAt);
    var startingTimes = Enumerable
      .Range(1, persistence.WindowSize-1)
      .Reverse()
      .Select(n => periodStart - n * info.PublicationPeriod)
      .ToArray();
    var content = startingTimes
      .Select(st => new Dated<StateDigest>(StateDigest.Hole, st, st + info.PublicationPeriod))
      .ToDictionary(h => h.Start, h => h);
    foreach (var pp in persistence.ReadPublished())
    {
      if (content.ContainsKey(pp.Start))
        content[pp.Start] = pp;
    }
    persistence.ReadUnpublished().Tap(
      d =>
      {
        if (content.ContainsKey(d.Start) && content[d.Start].Value.Equals(StateDigest.Hole))
          content[d.Start] = new Dated<StateDigest>(d.Value+StateDigest.Hole, d.Start, content[d.Start].End);
      });
    bool hole = false;
    foreach (var c in content)
    {
      var d = c.Value;
      if (hole)
        content[c.Key] = d.Select(sd => StateDigest.Hole + sd);
      hole = d.Value.Equals(StateDigest.Hole);
    }

    return content.Values;
  }
}