using System.Net;
using System.Text.Json.Nodes;
using ImpliciX.HttpTimeSeries.HttpApi;

namespace ImpliciX.HttpTimeSeries.SimPod;

internal sealed class SimPodWebApi : HttpWebApiBase
{
  public SimPodWebApi(
    IDataService dataService
  )
  {
    DataService = dataService;
    var metricsResponse = MetricUrnsSelectableToEndPointResult(DataService.OutputUrnsAsStrings);
    Map = new ApiMap()
      .AddHttpGet(
        "/",
        _ => new EndPointResult(
          HttpStatusCode.OK,
          "welcome"
        )
      )
      .AddHttpPost(
        "/metrics",
        _ => metricsResponse
      )
      .AddHttpPost(
        "/query",
        QueryEndPoint
      );
  }

  private IDataService DataService { get; }

  private static EndPointResult MetricUrnsSelectableToEndPointResult(
    string[] metricUrnsSelectable
  )
  {
    var jsonMetricUrns = string.Join(
      ",\n",
      metricUrnsSelectable.Select(m => $"{{\"value\": \"{m}\"}}")
    );
    var json = $"""
                [
                {jsonMetricUrns}
                ]
                """;

    return new EndPointResult(
      HttpStatusCode.OK,
      json
    );
  }

  private IEndPointResult QueryEndPoint(
    ApiRequest request
  )
  {
    var body = JsonNode.Parse(request.Body);

    //var root = body.RootElement;
    var targets = body["targets"]
      .AsArray()
      .Select(t => t["target"].GetValue<string>()!)
      .ToArray();

    var fromNullableTicks =     body["range"]?["from"]?.GetValue<DateTime>().Ticks;
    var toNullableTicks =     body["range"]?["to"]?.GetValue<DateTime>().Ticks;

    var from = fromNullableTicks.HasValue ? TimeSpan.FromTicks(fromNullableTicks.Value) : (TimeSpan?)null;
    var to = toNullableTicks.HasValue ? TimeSpan.FromTicks(toNullableTicks.Value) : (TimeSpan?)null;

    var payload = targets.ToDictionary(
      it => it,
      it => DataService.ReadDbSeriesValues(
        it,
        from,
        to
      )
    );
    return new StreamedEndPointResult(
      HttpStatusCode.OK,
      payload
    );
  }
}
