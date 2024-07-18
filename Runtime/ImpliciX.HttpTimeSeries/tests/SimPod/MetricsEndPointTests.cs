using System.Net;
using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.Language.Model;
using Moq;
using NFluent;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.SimPod;

public class MetricsEndPointTests
{
  [Test]
  public void WhenIPostMetrics_ThenIReturnAllMetricsRootUrn()
  {
    var series = CreateFakeSeries(
      ("foo:a1:accumulated_value",1),
      ("foo:a1:samples_count",1),
      ("foo:v1",1)
      );

    var service = new DataService(series, _ => Mock.Of<IMetricsDbRepository>());
    var simPodWebApi = new SimPodWebApi(service);

    var result = simPodWebApi.Execute(new ("/metrics", "POST", string.Empty));
    Check.That(result.StatusCode).IsEqualTo(HttpStatusCode.OK);
    const string expected = """
                            [
                            {"value": "foo:a1:accumulated_value"},
                            {"value": "foo:a1:samples_count"},
                            {"value": "foo:v1"}
                            ]
                            """;

    Check.That(result.Read()).IsEqualTo(expected);
  }
}