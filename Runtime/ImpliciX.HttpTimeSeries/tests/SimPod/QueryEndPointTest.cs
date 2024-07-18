using System.Net;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.SimPod;

[TestFixture(typeof(TimeSeriesDbRepository))]
[TestFixture(typeof(ColdMetricsDbRepository))]
[NonParallelizable]
public class QueryEndPointTest<R> where R : IMetricsDbRepository
{
  private protected readonly long _currentDateTicks = new DateTime(
    2023,
    12,
    12
  ).Ticks;


  [Test]
  public void GivenTwoIntSeries_WhenIPostQuery_ThenIReturnTsValues()
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
            2
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
            2
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
                                    1,
                                    1702339260000
                                  ],
                                  [
                                    2,
                                    1702339320000
                                  ]
                                ]
                              },
                              {
                                "target": "foo:g2",
                                "datapoints": [
                                  [
                                    11,
                                    1702339260000
                                  ],
                                  [
                                    12,
                                    1702339320000
                                  ]
                                ]
                              }
                            ]
                            """;

    Check.That(response.Read()).IsEqualTo(expected);
  }

  [Test]
  public void GivenTwoDoubleSeries_WhenIPostQuery_ThenIReturnTsValues()
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
            2
          )
          .Select(
            o => new TimeSeriesValue(
              TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o),
              (float)o / 10
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
            2
          )
          .Select(
            o => new TimeSeriesValue(
              TimeSpan.FromTicks(_currentDateTicks) + TimeSpan.FromMinutes(o),
              ((float)o + 100) / 100
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
                                    0.10000000149011612,
                                    1702339260000
                                  ],
                                  [
                                    0.20000000298023224,
                                    1702339320000
                                  ]
                                ]
                              },
                              {
                                "target": "foo:g2",
                                "datapoints": [
                                  [
                                    1.0099999904632568,
                                    1702339260000
                                  ],
                                  [
                                    1.0199999809265137,
                                    1702339320000
                                  ]
                                ]
                              }
                            ]
                            """;

    Check.That(response.Read()).IsEqualTo(expected);
  }

  private protected DataService CreateDataService(IDefinedSeries series) => new(series, DbFactory);
  
  IMetricsDbRepository DbFactory(
    IDefinedSeries definedSeries
  )
  {
    var testFolder = Path.Combine(
      Path.GetTempPath(),
      $"QueryEndPointTest{typeof(R).Name}"
    );
    if (Directory.Exists(testFolder))
      Directory.Delete(
        testFolder,
        true
      );

    return typeof(R) switch
    {
      var t when t == typeof(TimeSeriesDbRepository) => new TimeSeriesDbRepository(
        definedSeries,
        testFolder,
        "test"
      ),
      var t when t == typeof(ColdMetricsDbRepository) => new ColdMetricsDbRepository(
        definedSeries,
        testFolder
      ),
      _ => throw new Exception($"Unknown type {typeof(R).Name}")
    };
  }
}
