using System;
using System.Collections.Generic;
using System.Numerics;

namespace ImpliciX.TimeMath.Computers;

public class Dated<T> :
  IAdditionOperators<Dated<T>, Dated<T>, Dated<T>>,
  ISubtractionOperators<Dated<T>, Dated<T>, Dated<T>>,
  IEquatable<Dated<T>>
  where T : IEquatable<T>, IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>
{
  public Dated(T t, TimeSpan start, TimeSpan end)
  {
    Value = t;
    Start = start;
    End = end;
  }

  public T Value { get; }
  public TimeSpan Start { get; }
  public TimeSpan End { get; }

  public static Dated<T> operator +(Dated<T> left, Dated<T> right) =>
    left.End == right.Start
      ? new(left.Value + right.Value, left.Start, right.End)
      : throw new ArgumentException($"Incompatible end {left.End} and start {right.Start} dates");

  public static Dated<T> operator -(Dated<T> left, Dated<T> right) =>
    left.Start != right.Start
      ? throw new ArgumentException($"Incompatible start {left.Start} and end {right.Start} dates")
      : new Dated<T>(left.Value - right.Value, right.End, left.End);
  
  public override string ToString() => $"{Start} {Value} {End}";

  public bool Equals(Dated<T>? other)
  {
    if (ReferenceEquals(null, other)) return false;
    if (ReferenceEquals(this, other)) return true;
    return EqualityComparer<T>.Default.Equals(Value, other.Value) && Start.Equals(other.Start) && End.Equals(other.End);
  }

  public override bool Equals(object? obj)
  {
    if (ReferenceEquals(null, obj)) return false;
    if (ReferenceEquals(this, obj)) return true;
    if (obj.GetType() != this.GetType()) return false;
    return Equals((Dated<T>)obj);
  }

  public override int GetHashCode()
  {
    return HashCode.Combine(Value, Start, End);
  }

}

public static partial class DatedExtensions
{
  public static Dated<TResult> Select<TSource, TResult>(this Dated<TSource> source, Func<TSource, TResult> selector)
    where TSource : IEquatable<TSource>, IAdditionOperators<TSource,TSource,TSource>, ISubtractionOperators<TSource,TSource,TSource>
    where TResult : IEquatable<TResult>, IAdditionOperators<TResult,TResult,TResult>, ISubtractionOperators<TResult,TResult,TResult>
  {
    return new Dated<TResult>(selector(source.Value), source.Start, source.End);
  }
  
  public static Dated<TResult> SelectMany<TSource, TResult>(this Dated<TSource> source, Func<TSource, Dated<TResult>> selector)
    where TSource : IEquatable<TSource>, IAdditionOperators<TSource,TSource,TSource>, ISubtractionOperators<TSource,TSource,TSource>
    where TResult : IEquatable<TResult>, IAdditionOperators<TResult,TResult,TResult>, ISubtractionOperators<TResult,TResult,TResult>
  {
    return selector(source.Value);
  }
}
