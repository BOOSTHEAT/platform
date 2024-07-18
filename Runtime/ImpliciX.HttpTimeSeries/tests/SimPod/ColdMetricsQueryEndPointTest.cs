using System.Net;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.SimPod;

[TestFixture(typeof(ColdMetricsDbRepository))]
[NonParallelizable]
public class ColdMetricsQueryEndPointTest<R> : QueryEndPointTest<R> where R : IMetricsDbRepository
{
  [Test]
  public void GivenALotOfIntSeries_WhenIPostQueryWithStart_ThenIReturn2TsValues()
  {
    var series = CreateFakeSeries(("foo:g1",10),("foo:g2",10));
    var dataService = CreateDataService(series);
    var sut = new SimPodWebApi(dataService);

    PopulateDb(
      dataService,
      "foo:g1",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:g1"] = Enumerable.Range(
            1,
            200
          )
          .Select(
            o => new TimeSeriesValue(
              TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o),
              o
            )
          )
          .ToArray()
      }
    );

    PopulateDb(
      dataService,
      "foo:g2",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:g2"] = Enumerable.Range(
            1,
            200
          )
          .Select(
            o => new TimeSeriesValue(
              TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o),
              o + 10
            )
          )
          .ToArray()
      }
    );


    var request = new ApiRequest(
      "/query",
      "POST",
      """
      {
        "range": {
          "from": "2023-12-12T03:18:57.236Z",
          "to": "2024-01-01T00:00:00.000Z",
          "raw": {
            "from": "now-6h",
            "to": "now"
          }
        },
        "targets": [
        {"target": "foo:g1"},
        {"target": "foo:g2"}]
      }
      """
    );

    var response = sut.Execute(request);

    Check.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    const string expected = """
                            [
                              {
                                "target": "foo:g1",
                                "datapoints": [
                                  [
                                    199,
                                    1702351140000
                                  ],
                                  [
                                    200,
                                    1702351200000
                                  ]
                                ]
                              },
                              {
                                "target": "foo:g2",
                                "datapoints": [
                                  [
                                    209,
                                    1702351140000
                                  ],
                                  [
                                    210,
                                    1702351200000
                                  ]
                                ]
                              }
                            ]
                            """;

    Check.That(response.Read()).IsEqualTo(expected);
  }
}
