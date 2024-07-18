using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.TimeMath.Computers.StateMonitoring;

internal class StateDigest :
  IEqualityComparer<StateDigest>,
  IEquatable<StateDigest>,
  IAdditionOperators<StateDigest, StateDigest, StateDigest>,
  ISubtractionOperators<StateDigest, StateDigest, StateDigest>
{
  public static StateDigest Neutral => new (neutral:true);
  public static StateDigest Hole => new (neutral:false);

  private readonly bool _neutral;
  private readonly ImmutableDictionary<Enum, StateChange> _states;
  private readonly Option<Enum> _start;
  private readonly Option<Enum> _end;

  private StateDigest(
    bool neutral = false,
    ImmutableDictionary<Enum, StateChange> states = null!,
    Option<Enum> start = null!,
    Option<Enum> end = null!
    )
  {
    _neutral = neutral;
    _states = states ?? ImmutableDictionary<Enum, StateChange>.Empty;
    _start = start ?? Option<Enum>.None();
    _end = end ?? Option<Enum>.None();
  }

  public IEnumerable<Property<MetricValue>> Publish(
    TimeSpan start, TimeSpan endIsNow, Dictionary<Enum,StateInfoDataItem> infos
  )
  {
    return (from info in infos
      let stateChange = this[info.Key]
      from output in stateChange.Publish(info.Value)
      select Property<MetricValue>.Create(
        output.urn,
        new MetricValue(output.value, start, endIsNow),
        endIsNow
      ));
  }

  public bool IsNeutral => _neutral;
  public StateChange this[Enum state] => _states.GetValueOrDefault(state,
    new StateChange(state, 0, TimeSpan.Zero));
  
  public static StateDigest operator +(StateDigest left, StateChange right)
  {
    var result = new StateDigest(
      states: left._states.SetItem(right.State, left[right.State] + right),
      start: left._neutral ? right.State : left._start,
      end:right.State);
    return result;
  }
  
  public static StateDigest operator +(StateChange left, StateDigest right)
  {
    var result = new StateDigest(
      states: right._states.SetItem(left.State, right[left.State] + left),
      start:left.State,
      end: right._neutral ? left.State : right._end);
    return result;
  }

  public bool StartsWith(Enum state) => _start.Match(() => false, s => Equals(s, state) );
  public bool EndsWith(Enum state) => _end.Match(() => false, s => Equals(s, state) );
  public TimeSpan TotalDuration => new (_states.Values.Select(sc => sc.Duration.Ticks).Sum());

  
  public static StateDigest operator +(StateDigest left, StateDigest right)
  {
    if (right._neutral)
      return left;
    if (left._neutral)
      return right;
    if (left._end.IsSome && right._end.IsSome && !Equals(left._end, right._start))
      throw new ArgumentException("Incompatible digest concatenation");
    var hole = left._end.IsNone || right._start.IsNone;
    var result = new StateDigest(
      states: Compute(left,right,s=>left[s]+right[s]+AdjustOccurrences(s,!hole && left.EndsWith(s) ? -1 : 0)),
      start: left._start,
      end: right._end);
    return result;
  }

  public static StateDigest operator -(StateDigest left, StateDigest right)
  {
    if (right._neutral)
      return left;
    if (left._neutral || !Equals(left._start,right._start))
      Incompatible();
    var result = right._end
      .Select(state => (state, (left[state] - right[state]).Duration.Ticks > 0))
      .Match(
      () => new StateDigest(
        states: Compute(left, right, s => left[s] - right[s]),
        start: right._end,
        end: left._end),
      pivot => new StateDigest(
        states: Compute(left, right,
          s => left[s] - right[s] + AdjustOccurrences(s, Equals(s, pivot.state) && pivot.Item2 ? 1 : 0)),
        start: pivot.Item2 ? right._end : null!,
        end: left._end)
    );
    if(result.TotalDuration.Ticks < 0)
      Incompatible();
    if(result.TotalDuration.Ticks == 0 && (result._start.IsSome || result._end.IsSome))
      Incompatible();
    return result;
    
    void Incompatible() => throw new ArgumentException("Incompatible digest subtraction");
  }

  private static ImmutableDictionary<Enum, StateChange> Compute(StateDigest left, StateDigest right, Func<Enum,StateChange> getValue)
    => left._states.Keys.Concat(right._states.Keys).Distinct()
    .Aggregate(
      ImmutableDictionary<Enum, StateChange>.Empty,
      (d, s) => d.SetItem(s, getValue(s))
    );

  private static StateChange AdjustOccurrences(Enum state, int delta) => new (state, delta, TimeSpan.Zero);
  
  public bool Equals(StateDigest? x, StateDigest? y) =>
    x != null
    && y != null
    && x._neutral == y._neutral
    && Equals(x._start, y._start)
    && Equals(x._end, y._end)
    && x._states.All(kv => kv.Value.Equals(y[kv.Key]))
    && y._states.All(kv => kv.Value.Equals(x[kv.Key]));

  public int GetHashCode(StateDigest obj) =>
    _states.Aggregate(0, (h, x) => HashCode.Combine(h, x.Key, x.Value));

  public bool Equals(StateDigest? other) => Equals(this, other);
  public override bool Equals(object? obj) => obj is StateDigest change && Equals(change);
  public override int GetHashCode() => GetHashCode(this);

  public override string ToString() =>
    _start.Match(() => "", x => x.ToString())
    + $"\u2794[{string.Join(',', _states.Values)}]\u2794"
    + _end.Match(() => "", x => x.ToString());

  
}