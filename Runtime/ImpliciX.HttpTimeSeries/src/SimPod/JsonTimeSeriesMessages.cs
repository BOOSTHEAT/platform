using System.Text.Json;
using System.Text.Json.Serialization;
using ImpliciX.Data.Api;
using TimeSeriesValue = ImpliciX.RuntimeFoundations.Events.TimeSeriesValue;

namespace ImpliciX.HttpTimeSeries.SimPod;

public record JsonTimeSeriesMessages(Dictionary<string, IEnumerable<TimeSeriesValue>> TimeSeriesValues)
{
  private static readonly JsonSerializerOptions JsonSerializerOptions = new ()
  {
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = { new JsonStringEnumConverter(), new ObjectConverter() }
  };

  private static readonly TimeSpan _epochTicks = new (
    new DateTime(
      1970,
      1,
      1
    ).Ticks
  );


  public string ToJson()
  {
    // JsonTimeSeriesMessage[] messages = { this } ;
    return JsonSerializer.Serialize(
      TimeSeriesValues.ToDictionary(
          pair => pair.Key,
          pair => pair.Value.Select(
            value => new []
            {
              value.Value,
              (value.At - _epochTicks).TotalMilliseconds
            }.ToList()
          )
        )
        .Select(
          pair =>
            new JsonTimeSeriesMessage(
              pair.Key,
              pair.Value.ToList()
            )
        ).ToArray()
      ,
      JsonSerializerOptions
    );
  }
}

public record JsonTimeSeriesMessage(string target, List<List<double>> datapoints)
{
}
