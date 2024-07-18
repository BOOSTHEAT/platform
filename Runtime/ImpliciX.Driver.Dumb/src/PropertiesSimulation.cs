using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Dumb
{
  public class PropertiesSimulation : IPropertySimulation
  {
    public Func<TimeSpan, IEnumerable<IDataModelValue>> Discrete<T>(PropertyUrn<T> urn, T nominalValue, params (T, double)[] otherValues)
    {
      var options = otherValues.Aggregate(new List<(T, double)>(), (l, x) =>
      {
        l.Add((x.Item1, x.Item2 + (l.Any() ? l.Last().Item2 : 0)));
        return l;
      }).Append((nominalValue, 1)).ToArray();

      IEnumerable<IDataModelValue> Compute(TimeSpan time)
      {
        var totalMillisecondsSeedPart = Convert.ToInt32(time.TotalMilliseconds % int.MaxValue);
        var seed = urn.GetHashCode() + totalMillisecondsSeedPart;
        var selection = new Random(seed).NextDouble();
        var value = options.First(x => selection < x.Item2).Item1;
        yield return new FakeMeasureValue<T>(urn, value, time);
      }

      return Compute;
    }

    public Func<TimeSpan, IEnumerable<IDataModelValue>> Timed<T>(PropertyUrn<T> urn, Func<TimeSpan, double> f)
      where T : IFloat<T>
    {
      var convert = CreateConverter<T>();
      IEnumerable<IDataModelValue> Compute(TimeSpan time)
      {
        yield return new FakeMeasureValue<T>(urn, convert(f(time)).Value, time);
      }

      return Compute;
    }

    public Func<TimeSpan, IEnumerable<IDataModelValue>> Stepper<T>(PropertyUrn<T> urn, double start, TimeSpan period, double delta)
      where T : IFloat<T>
      => Stepper(urn, start, period, (_, v) => v + delta);

    public Func<TimeSpan, IEnumerable<IDataModelValue>> Stepper<T>(PropertyUrn<T> urn, double start, TimeSpan period, Func<TimeSpan, double, double> delta)
      where T : IFloat<T>
    {
      var previous = Option<(TimeSpan, double)>.None();
      return Timed(urn, time =>
      {
        if (previous.IsNone)
        {
          previous = Option<(TimeSpan, double)>.Some((time, start));
          return start;
        }

        var (previousTime, previousValue) = previous.GetValue();
        var elapsed = time.Subtract(previousTime);
        if (elapsed < period)
          return previousValue;

        var newValue = delta(time, previousValue);
        previous = Option<(TimeSpan, double)>.Some((time, newValue));
        return newValue;
      });
    }

    public Func<TimeSpan, IEnumerable<IDataModelValue>> Sinusoid<T>(PropertyUrn<T> urn, double min, double max)
      where T : IFloat<T>
      => Timed(urn, time => CreateSinusoidalValue(time, 60, urn.GetHashCode(), min, max).value);

    public Func<TimeSpan, IEnumerable<IDataModelValue>> Sinusoid<T>(MeasureNode<T> node, double min, double max, double failThreshold)
      where T : IFloat<T>
    {
      var convert = CreateConverter<T>();
      IEnumerable<IDataModelValue> Compute(TimeSpan time)
      {
        var sinusoidal = CreateSinusoidalValue(time, 50 / failThreshold, node.Urn.GetHashCode(), min, max);
        var s = sinusoidal.sin;
        var t = sinusoidal.value;
        if (s < -failThreshold || s > failThreshold)
        {
          yield return new FakeMeasureValue<MeasureStatus>(node.status, MeasureStatus.Failure, time);
          yield break;
        }

        yield return new FakeMeasureValue<MeasureStatus>(node.status, MeasureStatus.Success, time);
        yield return new FakeMeasureValue<T>(node.measure, convert(t).GetValueOrDefault(), time);
      }

      return Compute;
    }

    private static Func<double, Result<T>> CreateConverter<T>() where T : IFloat<T>
    {
      var converter = typeof(T).GetMethod("FromFloat", BindingFlags.Public | BindingFlags.Static);
      System.Diagnostics.Debug.Assert(converter != null, nameof(converter) + " != null");
      object Convert(double x) => converter.Invoke(null, new object[] {System.Convert.ToSingle(x)});
      return x => (Result<T>) Convert(x);
    }

    private static (double sin, double value) CreateSinusoidalValue(TimeSpan currentTime, double periodInSeconds, int seed, double vMin, double vMax)
    {
      var now = currentTime.TotalMilliseconds / 1000d;
      var s = Math.Sin(now * 2d * Math.PI / periodInSeconds + seed % 10);
      var v = s * (vMax - vMin) / 2 + (vMax + vMin) / 2;
      return (s, v);
    }
  }
}