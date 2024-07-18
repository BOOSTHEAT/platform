using System;
using System.Collections.Generic;
using System.Numerics;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Model;

namespace ImpliciX.TimeMath.Computers.StateMonitoring;

internal readonly struct StateChange :
  IAdditionOperators<StateChange, TimeSpan, StateChange>,
  IAdditionOperators<StateChange, StateChange, StateChange>,
  ISubtractionOperators<StateChange, StateChange, StateChange>
{
  public StateChange(Enum state, int occurrence, TimeSpan duration)
  {
    State = state;
    Occurrence = occurrence;
    Duration = duration;
  }

  public readonly Enum State;
  public readonly int Occurrence;
  public readonly TimeSpan Duration;
  
  public StateChange With(int occurrence, TimeSpan duration) => new(State, occurrence, duration);
  

  public IEnumerable<(MetricUrn urn, float value)> Publish(StateInfoDataItem info)
  {
    yield return (info.Occurrence, Occurrence);
    yield return (info.Duration, (float)Duration.TotalSeconds);
  }

  public static StateChange operator +(StateChange sc, TimeSpan delta) =>
    sc.With(sc.Occurrence + 1, sc.Duration + delta);

  public static StateChange operator +(StateChange left, StateChange right) =>
    Equals(left.State, right.State)
      ? new StateChange(
        left.State,
        left.Occurrence + right.Occurrence,
        left.Duration + right.Duration
      )
      : throw new ArgumentException("Cannot add state changes for different states");

  public static StateChange operator -(StateChange left, StateChange right) =>
    Equals(left.State, right.State)
      ? new StateChange(
        left.State,
        left.Occurrence - right.Occurrence,
        left.Duration - right.Duration
      )
      : throw new ArgumentException("Cannot add state changes for different states");
  
  public override string ToString() => $"({State},{Occurrence},{Duration})";
}