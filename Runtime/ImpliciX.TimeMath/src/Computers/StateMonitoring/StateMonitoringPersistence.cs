using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers.StateMonitoring;


internal interface IStateMonitoringPersistence
{
  int WindowSize { get; }
  void StoreUnpublished(TimeSpan at, StateChange state, StateInfoDataItem info);
  Option<Dated<StateDigest>> ReadUnpublished();
  void StorePublished(Dated<StateDigest> chunk);
  IEnumerable<Dated<StateDigest>> ReadPublished();
  TimeSpan PeriodStartJustBefore(TimeSpan time);
}

internal class StateMonitoringPersistence : IStateMonitoringPersistence
{
  private readonly StateMonitoringInfo _info;
  private readonly ITimeMathWriter _writer;
  private readonly ITimeMathReader _reader;

  public int WindowSize { get; }

  public StateMonitoringPersistence(StateMonitoringInfo info, ITimeMathWriter writer, ITimeMathReader reader)
  {
    _info = info;
    _writer = writer;
    _reader = reader;
    WindowSize = info.WindowRetention.Match(
      () => 1,
      windowRetention => (int)(windowRetention.TotalMilliseconds / info.PublicationPeriod.TotalMilliseconds)
    );
    foreach (var stateInfo in _info.States)
    {
      _writer.SetupTimeSeries(UnpublishedKey(stateInfo.Value.Occurrence), _info.PublicationPeriod);
      _writer.SetupTimeSeries(UnpublishedKey(stateInfo.Value.Duration), _info.PublicationPeriod);
      _writer.SetupTimeSeries(PublishedKey(stateInfo.Value.Occurrence), WindowSize * _info.PublicationPeriod);
      _writer.SetupTimeSeries(PublishedKey(stateInfo.Value.Duration), WindowSize * _info.PublicationPeriod);
      _writer.SetupTimeSeries(PublishedBounds(stateInfo.Value.RootUrn), WindowSize * _info.PublicationPeriod);
    }
  }

  public void StoreUnpublished(TimeSpan at, StateChange state, StateInfoDataItem info)
  {
    _writer.WriteTsAndApplyRetention(UnpublishedKey(info.Occurrence), at, state.Occurrence);
    _writer.WriteTsAndApplyRetention(UnpublishedKey(info.Duration), at, (float)state.Duration.TotalSeconds);
  }

  public Option<Dated<StateDigest>> ReadUnpublished()
  {
    var stored = ReadAllStateChanges().ToArray();
    return stored.Any() ? GetDated() : Option<Dated<StateDigest>>.None();

    Dated<StateDigest> GetDated()
    {
      var lastTime = stored.Select(x => x.At).Max();
      var digest = stored.Select(x => x.Change)
        .Aggregate(StateDigest.Neutral, (sd, sc) => sd + sc);
      return new Dated<StateDigest>(digest, PeriodStartJustBefore(lastTime),
        PeriodStartJustBefore(lastTime) + _info.PublicationPeriod);
    }
  }

  private IEnumerable<(TimeSpan At,StateChange Change)> ReadAllStateChanges()
  {
    foreach (var stateInfo in _info.States)
    {
      var occurrence = _reader.ReadTsLast(UnpublishedKey(stateInfo.Value.Occurrence));
      var duration = _reader.ReadTsLast(UnpublishedKey(stateInfo.Value.Duration));
      if (occurrence.IsNone || duration.IsNone)
        continue;
      yield return (occurrence.GetValue().At, new StateChange(
        stateInfo.Key,
        (int)occurrence.GetValue().Value, TimeSpan.FromSeconds(duration.GetValue().Value)
      ));
    }
  }

  public void StorePublished(Dated<StateDigest> chunk)
  {
    foreach (var stateInfo in _info.States)
    {
      var info = stateInfo.Value;
      var data = chunk.Value[stateInfo.Key];
      var now = chunk.End;
      _writer.WriteTsAndApplyRetention(PublishedKey(info.Occurrence), now, data.Occurrence );
      _writer.WriteTsAndApplyRetention(PublishedKey(info.Duration), now, (float)data.Duration.TotalSeconds );
      var bounds =
        (chunk.Value.StartsWith(stateInfo.Key) ? 1 : 0)
        + (chunk.Value.EndsWith(stateInfo.Key) ? 2 : 0);
      _writer.WriteTsAndApplyRetention(PublishedBounds(info.RootUrn), now, bounds );
    }
  }

  public IEnumerable<Dated<StateDigest>> ReadPublished()
  {
    var checkBound = GetBoundsChecker();
    var published = new List<Dated<StateDigest>>();
    foreach (var time in AllStoredStateChanges().GroupBy(x => x.At))
    {
      var digest = time
        .SelectMany(x => x.Changes)
        .Aggregate(StateDigest.Neutral, (sd,sc) => sd+sc);

      var end = time.Key;
      foreach (var stateInfo in _info.States)
      {
        if (checkBound(stateInfo.Value.RootUrn, end).IsStart)
          digest = new StateChange(stateInfo.Key, 0, TimeSpan.Zero) + digest;
        if (checkBound(stateInfo.Value.RootUrn, end).IsEnd)
          digest = digest + new StateChange(stateInfo.Key, 0, TimeSpan.Zero);
      }

      published.Add(new Dated<StateDigest>(digest, end - _info.PublicationPeriod, end));
    }

    return published;
  }

  private IEnumerable<(TimeSpan At, StateChange[] Changes)> AllStoredStateChanges()
  {
    var occurrences =
      from stateInfo in _info.States
      from occurrence in _reader.ReadTsAll(PublishedKey(stateInfo.Value.Occurrence))
      select (stateInfo.Key, 0, occurrence.At, occurrence.Value);
    var durations =
      from stateInfo in _info.States
      from duration in _reader.ReadTsAll(PublishedKey(stateInfo.Value.Duration))
      select (stateInfo.Key, 1, duration.At, duration.Value);
    var stored =
      from entry in occurrences.Concat(durations)
      group entry by entry.At into published
      orderby published.Key
      select (published.Key, GetChangesByState(published).ToArray());
    return stored;
    
    IEnumerable<StateChange> GetChangesByState(IEnumerable<(Enum Key, int Type, TimeSpan At, float Value)> published) =>
      from change in published
      group change by change.Key into state
      let data = state.OrderBy(x => x.Type).ToArray()
      select new StateChange(
        state.Key, (int)data.First().Value, TimeSpan.FromSeconds(data.Last().Value)
      );
  }

  private Func<MetricUrn, TimeSpan, (bool IsStart, bool IsEnd)> GetBoundsChecker()
  {
    var bounds = (
      from stateInfo in _info.States
      from bound in _reader.ReadTsAll(PublishedBounds(stateInfo.Value.RootUrn))
      let isStart = ((int)bound.Value & 1) != 0
      let isEnd = ((int)bound.Value & 2) != 0
      select ((stateInfo.Value.RootUrn, bound.At),(isStart,isEnd)))
      .ToDictionary(x => x.Item1, x => x.Item2);
    return (urn, ts) => bounds.GetValueOrDefault((urn, ts), (false, false));
  }

  public TimeSpan PeriodStartJustBefore(TimeSpan time) => PeriodStartJustBefore(time, _info.PublicationPeriod);

  internal static TimeSpan PeriodStartJustBefore(TimeSpan time, TimeSpan period) =>
    new(period.Ticks * (time.Ticks / period.Ticks));

  private static string UnpublishedKey(MetricUrn urn) => $"{urn.Value}$unpublished";
  private static string PublishedKey(MetricUrn urn) => $"{urn.Value}$published";
  private static string PublishedBounds(MetricUrn urn) => $"{urn.Value}:bounds$published";
}