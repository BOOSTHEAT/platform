using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.ThingsBoard.Messages;
using ImpliciX.ThingsBoard.Messages.Formatter;

namespace ImpliciX.ThingsBoard.Publishers
{
  public class Telemetry : Publisher
  {
    public Telemetry(IEnumerable<(Urn urn, TimeSpan period)> model, Queue<IThingsBoardMessage> elementsQueue)
      : base(elementsQueue)
    {
      _cache = model.ToDictionary(x => x.urn, x => new DataPointProducer(x.period));
    }

    public override void Handles(SystemTicked ticked)
    {
      var dt = GetDateTime(ticked);
      var data = _cache.Values.SelectMany(x => x.Tick(dt)).ToArray();
      if (data.Length == 0)
        return;
      var message = new Message(dt, data);
      ElementsQueue.Enqueue(message);
    }

    public override void Handles(PropertiesChanged propertiesChanged)
    {
      if (propertiesChanged.Group != null
          && _cache.TryGetValue(propertiesChanged.Group, out var dpp))
      {
        dpp.Update(new GroupModelValue(propertiesChanged));
        return;
      }

      foreach (var mv in propertiesChanged.ModelValues)
      {
        if (_cache.TryGetValue(mv.Urn, out var pu))
          pu.Update(mv);
      }
    }

    private readonly Dictionary<Urn, DataPointProducer> _cache;

    public static long GetUnixTimestamp(DateTime dateTime) =>
      ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();

    private class DataPointProducer
    {
      private readonly TimeSpan _period;
      private IDataModelValue _data;
      private DateTime? _previous;

      public DataPointProducer(TimeSpan period)
      {
        _period = period;
      }

      public void Update(IDataModelValue data)
      {
        _data = data;
      }

      public IEnumerable<IDataModelValue> Tick(DateTime dateTime)
      {
        if (_data == null || !ShouldSendAt(dateTime))
          yield break;
        var data = _data;
        _previous = dateTime;
        _data = null;
        yield return data;
      }

      private bool ShouldSendAt(DateTime dateTime) =>
        _previous == null || dateTime.Subtract(_previous.Value) >= _period;
    }

    private class GroupModelValue : IDataModelValue
    {
      public GroupModelValue(PropertiesChanged propertiesChanged)
      {
        Urn = propertiesChanged.Group;
        At = propertiesChanged.At;
        var values = propertiesChanged.ModelValues
          .Select(v => (v.Urn, v.ModelValue()))
          .ToArray();
        var convert = ValueConverter(values);
        var startAt = Urn.Value.Length + 1;
        foreach ((Urn urn, object mv) in values)
        {
          var name = urn.Value.Length > startAt ? urn.Value.Substring(startAt) : "value";
          _data[name] = convert(mv);
        }
      }

      private Func<object, object> ValueConverter((Urn Urn, object)[] values)
      {
        if (values.First().Item2 is MetricValue firstMetric)
        {
          _data["start"] = GetUnixTimestamp(new DateTime(firstMetric.SamplingStartDate.Ticks, DateTimeKind.Utc));
          _data["end"] = GetUnixTimestamp(new DateTime(firstMetric.SamplingEndDate.Ticks, DateTimeKind.Utc));
          return x => ((MetricValue)x).Value;
        }
        return x => x;
      }

      public TimeSpan At { get; }
      public Urn Urn { get; }
      public object ModelValue() => _data;
      private readonly IDictionary<string, object> _data = new Dictionary<string, object>();
    }

    private class Message : IThingsBoardMessage
    {
      private readonly DateTime _dateTime;
      private readonly IEnumerable<IDataModelValue> _modelValues;

      public Message(DateTime dateTime, IEnumerable<IDataModelValue> modelValues)
      {
        _dateTime = dateTime;
        _modelValues = modelValues;
      }

      public string Format(IPublishingContext context) =>
        new TelemetryDataJson(GetUnixTimestamp(_dateTime), CreateData(_modelValues)).Format();

      IDictionary<string, object> CreateData(IEnumerable<IDataModelValue> modelValues)
      {
        var obj = new Dictionary<string, object>();
        foreach (var idmv in modelValues)
        {
          if (idmv is GroupModelValue mmv)
          {
            obj.Add(idmv.Urn, mmv.ModelValue());
            continue;
          }

          var result = idmv.ToFloat();
          result.Tap(
            e => Log.Warning(e.Message),
            v => obj.Add(idmv.Urn, v)
          );
        }

        return obj;
      }

      public string GetTopic() => "v1/devices/me/telemetry";
    }

    public readonly struct TelemetryDataJson
    {
      public TelemetryDataJson(long timestamp, IDictionary<string, object> data)
      {
        ts = timestamp;
        values = data;
      }

      public long ts { get; }
      public IDictionary<string, object> values { get; }
    }
  }
}