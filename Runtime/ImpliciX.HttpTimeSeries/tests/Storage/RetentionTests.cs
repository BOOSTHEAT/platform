using System.Net;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.HttpTimeSeries.SimPod;
using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.TestsCommon;
using NFluent;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;

namespace ImpliciX.HttpTimeSeries.Tests.Storage;

public class RetentionTests
{
  private readonly TimeSpan _currentDate = TimeSpan.FromTicks(
    new DateTime(
      2023,
      12,
      12
    ).Ticks
  );

  private readonly TimeHelper T = TimeHelper.Minutes();

  [Test]
  public void GivenSingleTimeSeries_AndTooOldSeriesPointsExists_WhenIPostQuery_ThenIReturnTsValuesAccordingRetention()
  {
    var dataService = CreateDataService(CreateFakeSeries(("foo:g1",6)));

    var sut = new SimPodWebApi(dataService);

    PopulateDb(
      dataService,
      "foo:g1",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:g1"] = Enumerable.Range(
            1,
            10
          )
          .Select(
            o => new TimeSeriesValue(
              _currentDate + TimeSpan.FromMinutes(o),
              o
            )
          )
          .ToArray()
      }
    );

    var request = new ApiRequest(
      "/query",
      "POST",
      """
      {"targets": [{"target": "foo:g1"}]}
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
                                    4,
                                    1702339440000
                                  ],
                                  [
                                    5,
                                    1702339500000
                                  ],
                                  [
                                    6,
                                    1702339560000
                                  ],
                                  [
                                    7,
                                    1702339620000
                                  ],
                                  [
                                    8,
                                    1702339680000
                                  ],
                                  [
                                    9,
                                    1702339740000
                                  ],
                                  [
                                    10,
                                    1702339800000
                                  ]
                                ]
                              }
                            ]
                            """;

    Check.That(response.Read()).IsEqualTo(expected);
  }

  [Test]
  public void GivenTwoTimeSeries_WhenIPostQuery_ThenIReturnTsValues()
  {
    var dataService = CreateDataService(CreateFakeSeries(
      ("foo:v1",5),
      ("foo:v1:_2Minutes",7),
      ("foo:v2",3),
      ("foo:v2:_2Minutes",10)
    ));
    
    var sut = new SimPodWebApi(dataService);

    PopulateDb(
      dataService,
      "foo:v1",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:v1"] = Enumerable.Range(
            1,
            12
          )
          .Select(
            i => new TimeSeriesValue(
              _currentDate + TimeSpan.FromMinutes(i),
              i
            )
          )
          .ToArray()
      }
    );

    PopulateDb(
      dataService,
      "foo:v1:_2Minutes",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:v1:_2Minutes"] = Enumerable.Range(
            1,
            6
          )
          .Select(
            i => new TimeSeriesValue(
              _currentDate + TimeSpan.FromMinutes(i * 2),
              i * 2
            )
          )
          .ToArray()
      }
    );

    PopulateDb(
      dataService,
      "foo:v2",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:v2"] = Enumerable.Range(
            1,
            12
          )
          .Select(
            i => new TimeSeriesValue(
              _currentDate + TimeSpan.FromMinutes(i),
              i
            )
          )
          .ToArray()
      }
    );

    PopulateDb(
      dataService,
      "foo:v2:_2Minutes",
      new Dictionary<string, TimeSeriesValue[]>
      {
        ["foo:v2:_2Minutes"] = Enumerable.Range(
            1,
            6
          )
          .Select(
            i => new TimeSeriesValue(
              _currentDate + TimeSpan.FromMinutes(i * 2),
              i * 2
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
        {"target": "foo:v1"},
        {"target": "foo:v1:_2Minutes"},
        {"target": "foo:v2"},
        {"target": "foo:v2:_2Minutes"}
        ]
      }
      """
    );

    var response = sut.Execute(request);
    Check.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    Check.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

    const string expected = """
                            [
                              {
                                "target": "foo:v1",
                                "datapoints": [
                                  [
                                    7,
                                    1702339620000
                                  ],
                                  [
                                    8,
                                    1702339680000
                                  ],
                                  [
                                    9,
                                    1702339740000
                                  ],
                                  [
                                    10,
                                    1702339800000
                                  ],
                                  [
                                    11,
                                    1702339860000
                                  ],
                                  [
                                    12,
                                    1702339920000
                                  ]
                                ]
                              },
                              {
                                "target": "foo:v1:_2Minutes",
                                "datapoints": [
                                  [
                                    6,
                                    1702339560000
                                  ],
                                  [
                                    8,
                                    1702339680000
                                  ],
                                  [
                                    10,
                                    1702339800000
                                  ],
                                  [
                                    12,
                                    1702339920000
                                  ]
                                ]
                              },
                              {
                                "target": "foo:v2",
                                "datapoints": [
                                  [
                                    9,
                                    1702339740000
                                  ],
                                  [
                                    10,
                                    1702339800000
                                  ],
                                  [
                                    11,
                                    1702339860000
                                  ],
                                  [
                                    12,
                                    1702339920000
                                  ]
                                ]
                              },
                              {
                                "target": "foo:v2:_2Minutes",
                                "datapoints": [
                                  [
                                    2,
                                    1702339320000
                                  ],
                                  [
                                    4,
                                    1702339440000
                                  ],
                                  [
                                    6,
                                    1702339560000
                                  ],
                                  [
                                    8,
                                    1702339680000
                                  ],
                                  [
                                    10,
                                    1702339800000
                                  ],
                                  [
                                    12,
                                    1702339920000
                                  ]
                                ]
                              }
                            ]
                            """;

    Check.That(response.Read()).IsEqualTo(expected);
  }

  private DataService CreateDataService(IDefinedSeries series) => new(series, DbFactory);
  
  static IMetricsDbRepository DbFactory(
    IDefinedSeries definedSeries
  )
  {
    var testFolder = Path.Combine(
      Path.GetTempPath(),
      nameof(RetentionTests)
    );
    if (Directory.Exists(testFolder))
      Directory.Delete(
        testFolder,
        true
      );

    return new TimeSeriesDbRepository(
      definedSeries,
      testFolder,
      "test"
    );
  }
}
