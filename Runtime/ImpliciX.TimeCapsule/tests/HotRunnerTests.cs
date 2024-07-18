using ImpliciX.Data.Factory;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using static ImpliciX.TimeCapsule.Tests.Helpers;
using Dsl = ImpliciX.Language.Metrics.Metrics;
using TimeUnit = ImpliciX.Language.Model.TimeUnit;

namespace ImpliciX.TimeCapsule.Tests;

[TestFixture]
public class HotRunnerTests
{
  [SetUp]
  public void Init()
  {
    if (Directory.Exists("/tmp/hot_runner")) Directory.Delete("/tmp/hot_runner", true);

    _db = new TimeSeriesDb("/tmp/hot_runner", "tests");
    EventsHelper.ModelFactory = new ModelFactory(typeof(HotRunnerTests).Assembly);
    _groupedStoredMetrics = new Dictionary<string, List<DataModelValue<float>>>
    {
      {
        "fake_analytics:heating:Running:occurence", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:Running:occurence", 1, TimeSpan.FromHours(1)),
          new("fake_analytics:heating:Running:occurence", 2, TimeSpan.FromHours(2))
        }
      },
      {
        "fake_analytics:heating:Running:duration", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:Running:duration", 3600, TimeSpan.FromHours(1)),
          new("fake_analytics:heating:Running:duration", 3600, TimeSpan.FromHours(2))
        }
      },
      {
        "fake_analytics:heating:Running:fizz:accumulated_value", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:Running:fizz:accumulated_value", 10, TimeSpan.FromHours(1)),
          new("fake_analytics:heating:Running:fizz:accumulated_value", 11, TimeSpan.FromHours(2))
        }
      },
      {
        "fake_analytics:heating:Running:fizz:sample_count", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:Running:fizz:sample_count", 42, TimeSpan.FromHours(1)),
          new("fake_analytics:heating:Running:fizz:sample_count", 42, TimeSpan.FromHours(2))
        }
      },
      {
        "fake_analytics:heating:_8Hours:Running:occurence", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:_8Hours:Running:occurence", 81, TimeSpan.FromHours(8)),
          new("fake_analytics:heating:_8Hours:Running:occurence", 82, TimeSpan.FromHours(16))
        }
      },
      {
        "fake_analytics:heating:_8Hours:Running:duration", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:_8Hours:Running:duration", 83600, TimeSpan.FromHours(8)),
          new("fake_analytics:heating:_8Hours:Running:duration", 83600, TimeSpan.FromHours(16))
        }
      },
      {
        "fake_analytics:heating:_8Hours:Running:fizz:accumulated_value", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:_8Hours:Running:fizz:accumulated_value", 810, TimeSpan.FromHours(8)),
          new("fake_analytics:heating:_8Hours:Running:fizz:accumulated_value", 811, TimeSpan.FromHours(16))
        }
      },
      {
        "fake_analytics:heating:_8Hours:Running:fizz:sample_count", new List<DataModelValue<float>>
        {
          new("fake_analytics:heating:_8Hours:Running:fizz:sample_count", 842, TimeSpan.FromHours(8)),
          new("fake_analytics:heating:_8Hours:Running:fizz:sample_count", 842, TimeSpan.FromHours(16))
        }
      }
    };
    _simpleStoredMetrics = new Dictionary<string, List<DataModelValue<float>>>
    {
      {
        fake_analytics.temperature, new List<DataModelValue<float>>
        {
          new(fake_analytics.temperature.Value, 42, TimeSpan.Zero),
          new(fake_analytics.temperature.Value, 41, TimeSpan.FromMinutes(1))
        }
      },
      {
        "fake_analytics:temperature:duration", new List<DataModelValue<float>>
        {
          new("fake_analytics:temperature:duration", 42, TimeSpan.Zero),
          new("fake_analytics:temperature:duration", 60, TimeSpan.FromMinutes(1))
        }
      },
      {
        fake_analytics.temperature_delta, new List<DataModelValue<float>>
        {
          new(fake_analytics.temperature_delta.Value, 0, TimeSpan.Zero),
          new(fake_analytics.temperature_delta.Value, 1, TimeSpan.FromMinutes(1))
        }
      }
    };
  }

  [Test]
  public void GivenMetricsDefinitions_ComputeDefinedSeries()
  {
    var definedSeries = GivenRunner(TimeSpan.Zero, SimpleAndGroupedMetrics).DefinedSeries.Values.ToArray();
    var expected = new[]
    {
      new HotRunner.SeriesDefinition("fake_analytics:temperature", 10, TimeUnit.Minutes, false),
      new HotRunner.SeriesDefinition("fake_analytics:temperature_delta", 10, TimeUnit.Days, false),
      new HotRunner.SeriesDefinition("fake_analytics:heating", 24, TimeUnit.Hours, false),
      new HotRunner.SeriesDefinition("fake_analytics:heating:_8Hours", 5, TimeUnit.Days, true),
      new HotRunner.SeriesDefinition("fake_analytics:heating:_1Days", 1, TimeUnit.Months, true)
    };

    Assert.That(definedSeries, Is.EquivalentTo(expected));
  }

  [Test]
  public void GivenMetricsDefinitionsNotDeclaredInCharts_ComputeDefinedSeries()
  {
    var definedSeries = GivenRunner(TimeSpan.Zero, (
      _simpleMetrics.Metrics.Concat(_groupMetrics.Metrics).ToArray(),
      new (Urn, ChartXTimeSpan)[]
        {
          ("fake_analytics:temperature", new ChartXTimeSpan(10, TimeUnit.Minutes)),
          ("fake_analytics:temperature_delta", new ChartXTimeSpan(0, TimeUnit.Years)),
        }
        .Concat(new (Urn, ChartXTimeSpan)[]
        {
          ("fake_analytics:heating:_8Hours", new ChartXTimeSpan(5, TimeUnit.Days)),
          ("fake_analytics:heating:_1Days", new ChartXTimeSpan(0, TimeUnit.Years)),
        }).ToArray()
    )).DefinedSeries.Values.ToArray();
    var expected = new[]
    {
      new HotRunner.SeriesDefinition("fake_analytics:temperature", 10, TimeUnit.Minutes, false),
      new HotRunner.SeriesDefinition("fake_analytics:heating:_8Hours", 5, TimeUnit.Days, true),
    };

    Assert.That(definedSeries, Is.EquivalentTo(expected));
  }
  
  [Test]
  public void XSpanWithNoAssociatedMetric()
  {
    Assert.Throws<ApplicationException>(() =>
    {
      var definedSeries = GivenRunner(TimeSpan.Zero, (
        new IMetricDefinition[]
        {
          Dsl.Metric(fake_analytics.temperature).Is.Minutely.GaugeOf(fake_model.temperature.Urn)
            .Over.ThePast(50).Years,
          Dsl.Metric(fake_analytics.sample_metric).Is.Minutely.AccumulatorOf(fake_model.fake_index)
        }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray(),
        new (Urn, ChartXTimeSpan)[]
        {
          ("fake_analytics:temperature_delta", new ChartXTimeSpan(0, TimeUnit.Years)),
        }.ToArray()
      )).DefinedSeries.Values.ToArray();

    });
  }

  [Test]
  public void GivenChartsUsingSubpartOfMetric_ComputeDefinedSeries()
  {
    var definedSeries = GivenRunner(TimeSpan.Zero, (
      _simpleMetrics.Metrics.Concat(_groupMetrics.Metrics).ToArray(),
      new (Urn, ChartXTimeSpan)[]
        {
          ("fake_analytics:temperature:foo", new ChartXTimeSpan(10, TimeUnit.Minutes)),
          ("fake_analytics:temperature_delta:foo:bar", new ChartXTimeSpan(10, TimeUnit.Days)),
        }
        .Concat(new (Urn, ChartXTimeSpan)[]
        {
          ("fake_analytics:heating:foo", new ChartXTimeSpan(24, TimeUnit.Hours)),
          ("fake_analytics:heating:_8Hours:foo", new ChartXTimeSpan(5, TimeUnit.Days)),
          ("fake_analytics:heating:_1Days:foo:bar", new ChartXTimeSpan(1, TimeUnit.Months)),
        }).ToArray()
    )).DefinedSeries.Values.ToArray();
    var expected = new[]
    {
      new HotRunner.SeriesDefinition("fake_analytics:temperature", 10, TimeUnit.Minutes, false),
      new HotRunner.SeriesDefinition("fake_analytics:temperature_delta", 10, TimeUnit.Days, false),
      new HotRunner.SeriesDefinition("fake_analytics:heating", 24, TimeUnit.Hours, false),
      new HotRunner.SeriesDefinition("fake_analytics:heating:_8Hours", 5, TimeUnit.Days, true),
      new HotRunner.SeriesDefinition("fake_analytics:heating:_1Days", 1, TimeUnit.Months, true)
    };

    Assert.That(definedSeries, Is.EquivalentTo(expected));
  }


  [Test]
  public void GivenMetricsDefinitions_WhenHavingDuplicatedMetricDefinitions_ComputeDefinedSeries()
  {
    var definedSeries =
      GivenRunner(
        TimeSpan.Zero,
        (
          new IMetricDefinition[]
          {
            Dsl.Metric(fake_analytics.temperature).Is.Minutely.GaugeOf(fake_model.temperature.Urn).Over
              .ThePast(10).Minutes,
            Dsl.Metric(fake_analytics.temperature).Is.Minutely.GaugeOf(fake_model.temperature.Urn).Over
              .ThePast(12).Hours
          }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray(),
          new (Urn, ChartXTimeSpan)[]
          {
            ("fake_analytics:temperature", new ChartXTimeSpan(10, TimeUnit.Minutes)),
            ("fake_analytics:temperature", new ChartXTimeSpan(12, TimeUnit.Hours)),
          }
        )
      ).DefinedSeries.Values.ToArray();

    var expected = new[]
    {
      new HotRunner.SeriesDefinition("fake_analytics:temperature", 12, TimeUnit.Hours, false)
    };

    Check.That(definedSeries).ContainsExactly(expected);
  }

  [Test]
  public void GivenSimpleMetricsAndADatabaseWithExistingDataForTheseMetrics_When_PublishMetrics()
  {
    var outcome =
      GivenRunner(TimeSpan.FromMinutes(2), _simpleMetrics, _simpleStoredMetrics)
        .PublishAllSeries();

    var expected = new[]
    {
      TimeSeriesChanged.Create(fake_analytics.temperature, new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:temperature", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 42f),
            new(TimeSpan.FromMinutes(1), 41f)
          }
        },
        {
          "fake_analytics:temperature:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 42f),
            new(TimeSpan.FromMinutes(1), 60f)
          }
        }
      }, TimeSpan.FromMinutes(2)),

      TimeSeriesChanged.Create(fake_analytics.temperature_delta, new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:temperature_delta", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 0f),
            new(TimeSpan.FromMinutes(1), 1f)
          }
        }
      }, TimeSpan.FromMinutes(2))
    };
    CheckAreEquivalent(outcome, expected);
  }

  [Test]
  public void GivenGroupedMetricsAndADatabaseWithExistingDataForTheseMetrics_When_PublishMetrics()
  {
    var outcome =
      GivenRunner(TimeSpan.FromHours(17), _groupMetrics, _groupedStoredMetrics)
        .PublishAllSeries();

    var expected = new[]
    {
      TimeSeriesChanged.Create(fake_analytics.heating, new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:heating:Running:occurence", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 1f),
            new(TimeSpan.FromHours(2), 2f)
          }
        },
        {
          "fake_analytics:heating:Running:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 3600f),
            new(TimeSpan.FromHours(2), 3600f)
          }
        },
        {
          "fake_analytics:heating:Running:fizz:accumulated_value", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 10f),
            new(TimeSpan.FromHours(2), 11f)
          }
        },
        {
          "fake_analytics:heating:Running:fizz:sample_count", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 42f),
            new(TimeSpan.FromHours(2), 42f)
          }
        }
      }, TimeSpan.FromHours(17)),

      TimeSeriesChanged.Create("fake_analytics:heating:_8Hours", new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:heating:_8Hours:Running:occurence", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 81f),
            new(TimeSpan.FromHours(16), 82f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 83600f),
            new(TimeSpan.FromHours(16), 83600f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:fizz:accumulated_value", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 810f),
            new(TimeSpan.FromHours(16), 811f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:fizz:sample_count", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 842f),
            new(TimeSpan.FromHours(16), 842f)
          }
        }
      }, TimeSpan.FromHours(17))
    };
    CheckAreEquivalent(outcome, expected);
  }

  [Test]
  public void GivenDefinedMetricsAndNoOtherMetricsStored_When_Receiving_Storable_Metrics_Then_StoreTimeSeries()
  {
    var sut = GivenRunner(TimeSpan.Zero, _simpleMetrics);
    var receivedMetrics =
      PropertiesChanged.Create(
        fake_analytics.temperature,
        new IDataModelValue[]
        {
          new DataModelValue<MetricValue>(fake_analytics.temperature, MV(42f), TimeSpan.Zero),
          new DataModelValue<MetricValue>(
            PropertyUrn<MetricValue>.Build("fake_analytics", "temperature", "duration"), MV(12f),
            TimeSpan.Zero)
        },
        TimeSpan.Zero);

    var outcome = sut.StoreSeries(receivedMetrics);
    var expected = new[]
    {
      TimeSeriesChanged.Create(fake_analytics.temperature, new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:temperature", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 42f)
          }
        },
        {
          "fake_analytics:temperature:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 12f)
          }
        }
      }, TimeSpan.Zero)
    };
    CheckAreEquivalent(outcome, expected);
  }
  
  [Test]
  public void GivenDefinedMetricsAndAlreadyStoredMetrics_When_Receiving_Storable_PropertiesChanged_Then_StoreMetrics()
  {
    var sut = GivenRunner(TimeSpan.FromMinutes(2), _simpleMetrics, _simpleStoredMetrics);
    var receivedMetrics =
      PropertiesChanged.Create(
        fake_analytics.temperature,
        new IDataModelValue[]
        {
          new DataModelValue<MetricValue>(fake_analytics.temperature, MV(77f), TimeSpan.FromMinutes(2)),
          new DataModelValue<MetricValue>(
            PropertyUrn<MetricValue>.Build("fake_analytics", "temperature", "duration"), MV(88f),
            TimeSpan.FromMinutes(2))
        },
        TimeSpan.FromMinutes(2));

    var outcome = sut.StoreSeries(receivedMetrics);
    var expected = new[]
    {
      TimeSeriesChanged.Create(fake_analytics.temperature, new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:temperature", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 42f),
            new(TimeSpan.FromMinutes(1), 41f),
            new(TimeSpan.FromMinutes(2), 77f)
          }
        },
        {
          "fake_analytics:temperature:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.Zero, 42f),
            new(TimeSpan.FromMinutes(1), 60f),
            new(TimeSpan.FromMinutes(2), 88f)
          }
        }
      }, TimeSpan.FromMinutes(2))
    };
    
    CheckAreEquivalent(outcome, expected);

    Check.That(_db.RetentionTime("fake_analytics:temperature")).IsEqualTo(TimeSpan.FromMinutes(10));
    Check.That(_db.RetentionTime("fake_analytics:temperature:duration")).IsEqualTo(TimeSpan.FromMinutes(10));
  }

  [Test]
  public void
    GivenDefinedMetricsAndAlreadyStoredMetrics_When_Receiving_Ungrouped_Storable_Metrics_Then_StoreTimeSeries()
  {
    var sut = GivenRunner(TimeSpan.FromHours(4), _groupMetrics, _groupedStoredMetrics);
    var receivedMetrics =
      PropertiesChanged.Create(
        "fake_analytics:heating",
        new IDataModelValue[]
        {
          new DataModelValue<MetricValue>("fake_analytics:heating:Running:occurence", MV(3f),
            TimeSpan.FromHours(3)),
          new DataModelValue<MetricValue>("fake_analytics:heating:Running:duration", MV(3600f),
            TimeSpan.FromHours(3)),
          new DataModelValue<MetricValue>("fake_analytics:heating:Running:fizz:accumulated_value", MV(12f),
            TimeSpan.FromHours(3)),
          new DataModelValue<MetricValue>("fake_analytics:heating:Running:fizz:sample_count", MV(52f),
            TimeSpan.FromHours(3))
        },
        TimeSpan.FromHours(3));

    var outcome = sut.StoreSeries(receivedMetrics);
    var expected = new[]
    {
      TimeSeriesChanged.Create(fake_analytics.heating, new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:heating:Running:occurence", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 1f),
            new(TimeSpan.FromHours(2), 2f),
            new(TimeSpan.FromHours(3), 3f)
          }
        },
        {
          "fake_analytics:heating:Running:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 3600f),
            new(TimeSpan.FromHours(2), 3600f),
            new(TimeSpan.FromHours(3), 3600f)
          }
        },
        {
          "fake_analytics:heating:Running:fizz:accumulated_value", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 10f),
            new(TimeSpan.FromHours(2), 11f),
            new(TimeSpan.FromHours(3), 12f)
          }
        },
        {
          "fake_analytics:heating:Running:fizz:sample_count", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(1), 42f),
            new(TimeSpan.FromHours(2), 42f),
            new(TimeSpan.FromHours(3), 52f)
          }
        }
      }, TimeSpan.FromHours(4))
    };

    CheckAreEquivalent(outcome, expected);

    Check.That(_db.RetentionTime("fake_analytics:heating:Running:occurence")).IsEqualTo(TimeSpan.FromHours(24));
    Check.That(_db.RetentionTime("fake_analytics:heating:Running:duration")).IsEqualTo(TimeSpan.FromHours(24));
    Check.That(_db.RetentionTime("fake_analytics:heating:Running:fizz:accumulated_value"))
      .IsEqualTo(TimeSpan.FromHours(24));
    Check.That(_db.RetentionTime("fake_analytics:heating:Running:fizz:sample_count"))
      .IsEqualTo(TimeSpan.FromHours(24));
  }

  [Test]
  public void
    GivenDefinedMetricsAndAlreadyStoredMetrics_When_Receiving_Grouped_Storable_Metrics_Then_StoreTimeSeries()
  {
    var sut = GivenRunner(currentTime: TimeSpan.FromHours(24), definitions: _groupMetrics,
      alreadyStoredMetrics: _groupedStoredMetrics);
    var receivedMetrics =
      PropertiesChanged.Create(
        "fake_analytics:heating:_8Hours",
        new IDataModelValue[]
        {
          new DataModelValue<MetricValue>("fake_analytics:heating:_8Hours:Running:occurence", MV(83f),
            TimeSpan.FromHours(24)),
          new DataModelValue<MetricValue>("fake_analytics:heating:_8Hours:Running:duration", MV(83600f),
            TimeSpan.FromHours(24)),
          new DataModelValue<MetricValue>("fake_analytics:heating:_8Hours:Running:fizz:accumulated_value", MV(812f),
            TimeSpan.FromHours(24)),
          new DataModelValue<MetricValue>("fake_analytics:heating:_8Hours:Running:fizz:sample_count", MV(852f),
            TimeSpan.FromHours(24))
        },
        TimeSpan.FromHours(24));

    var outcome = sut.StoreSeries(receivedMetrics);
    var expected = new[]
    {
      TimeSeriesChanged.Create("fake_analytics:heating:_8Hours", new Dictionary<Urn, HashSet<TimeSeriesValue>>
      {
        {
          "fake_analytics:heating:_8Hours:Running:occurence", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 81f),
            new(TimeSpan.FromHours(16), 82f),
            new(TimeSpan.FromHours(24), 83f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:duration", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 83600f),
            new(TimeSpan.FromHours(16), 83600f),
            new(TimeSpan.FromHours(24), 83600f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:fizz:accumulated_value", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 810f),
            new(TimeSpan.FromHours(16), 811f),
            new(TimeSpan.FromHours(24), 812f)
          }
        },
        {
          "fake_analytics:heating:_8Hours:Running:fizz:sample_count", new HashSet<TimeSeriesValue>
          {
            new(TimeSpan.FromHours(8), 842f),
            new(TimeSpan.FromHours(16), 842f),
            new(TimeSpan.FromHours(24), 852f)
          }
        }
      }, TimeSpan.FromHours(24))
    };

    CheckAreEquivalent(outcome, expected);

    Check.That(_db.RetentionTime("fake_analytics:heating:_8Hours:Running:occurence"))
      .IsEqualTo(TimeSpan.FromDays(5));
    Check.That(_db.RetentionTime("fake_analytics:heating:_8Hours:Running:duration"))
      .IsEqualTo(TimeSpan.FromDays(5));
    Check.That(_db.RetentionTime("fake_analytics:heating:_8Hours:Running:fizz:accumulated_value"))
      .IsEqualTo(TimeSpan.FromDays(5));
    Check.That(_db.RetentionTime("fake_analytics:heating:_8Hours:Running:fizz:sample_count"))
      .IsEqualTo(TimeSpan.FromDays(5));
  }

  private static readonly (Metric<MetricUrn>[] Metrics, (Urn, ChartXTimeSpan)[] XSpans) _simpleMetrics = (
    new IMetricDefinition[]
    {
      Dsl.Metric(fake_analytics.temperature).Is.Minutely.GaugeOf(fake_model.temperature.Urn)
        .Over.ThePast(50).Years,
      Dsl.Metric(fake_analytics.temperature_delta).Is.Minutely.VariationOf(fake_model.temperature.Urn)
        .Over.ThePast(100).Years,
      Dsl.Metric(fake_analytics.sample_metric).Is.Minutely.AccumulatorOf(fake_model.fake_index)
    }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray(),
    new (Urn, ChartXTimeSpan)[]
    {
      ("fake_analytics:temperature", new ChartXTimeSpan(10, TimeUnit.Minutes)),
      ("fake_analytics:temperature_delta", new ChartXTimeSpan(10, TimeUnit.Days)),
    }
  );

  private static readonly (Metric<MetricUrn>[] Metrics, (Urn, ChartXTimeSpan)[] XSpans) _groupMetrics = (
    new IMetricDefinition[]
    {
      Dsl.Metric(fake_analytics.heating).Is.Hourly
        .StateMonitoringOf(fake_model.public_state)
        .Including("fizz").As.AccumulatorOf(fake_model.supply_temperature.measure)
        .Over.ThePast(150).Months
        .Group.Every(8).Hours.Over.ThePast(16).Hours
        .Group.Every(1).Days.Over.ThePast(10).Years
    }.Select(def => def.Builder.Build<Metric<MetricUrn>>()).ToArray(),
    new (Urn, ChartXTimeSpan)[]
    {
      ("fake_analytics:heating", new ChartXTimeSpan(24, TimeUnit.Hours)),
      ("fake_analytics:heating:_8Hours", new ChartXTimeSpan(5, TimeUnit.Days)),
      ("fake_analytics:heating:_1Days", new ChartXTimeSpan(1, TimeUnit.Months)),
    }
  );

  public (Metric<MetricUrn>[] Metrics, (Urn, ChartXTimeSpan)[] XSpans) SimpleAndGroupedMetrics =>
  (
    _simpleMetrics.Metrics.Concat(_groupMetrics.Metrics).ToArray(),
    _simpleMetrics.XSpans.Concat(_groupMetrics.XSpans).ToArray()
  );


  private Dictionary<string, List<DataModelValue<float>>> _simpleStoredMetrics;
  private Dictionary<string, List<DataModelValue<float>>> _groupedStoredMetrics;

  private static IWriteTimeSeries _tsWriter;
  private static IReadTimeSeries _tsReader;
  private static TimeSeriesDb _db;

  private static HotRunner GivenRunner(
    TimeSpan currentTime,
    (Metric<MetricUrn>[] Metrics, (Urn, ChartXTimeSpan)[] XSpans) definitions,
    Dictionary<string, List<DataModelValue<float>>> alreadyStoredMetrics = null
  )
  {
    PopulateDb(alreadyStoredMetrics);
    _tsReader = _db;
    _tsWriter = _db;
    var clock = Mock.Of<IClock>(it => it.Now() == currentTime);
    var runner = new HotRunner(definitions.Metrics, definitions.XSpans, _tsReader, _tsWriter, clock);
    return runner;
  }

  private static void PopulateDb(Dictionary<string, List<DataModelValue<float>>> alreadyStoredMetrics)
  {
    var valuesToWrite = (alreadyStoredMetrics ?? new()).SelectMany(it => it.Value).ToList();
    var series = valuesToWrite.Select(it => it.Urn).ToArray();

    foreach (var sUrn in series)
    {
      _db.SetupTimeSeries(sUrn, TimeSpan.Zero);
    }

    foreach (var mv in valuesToWrite)
    {
      _db.Write(mv.Urn, mv.At, mv.Value);
    }
  }
  
  private static void CheckAreEquivalent(TimeSeriesChanged[] outcome, TimeSeriesChanged[] expected)
  {
    Assert.That(outcome.Length, Is.EqualTo(expected.Length));
    var compared =
      outcome.OrderBy(o => o.Urn.Value).Zip(
        expected.OrderBy(e => e.Urn.Value)
      ).ToArray();
    foreach (var (actual, exp) in compared)
      CheckAreEquivalentIgnoreOrder(actual, exp);
  }

  private static void CheckAreEquivalentIgnoreOrder(TimeSeriesChanged actual, TimeSeriesChanged expected)
  {
    Assert.That(actual.TimeSeries.Keys, Is.EquivalentTo(expected.TimeSeries.Keys));
    Assert.That(actual.TimeSeries.Values.SelectMany(it => it).ToArray(),
      Is.EquivalentTo(expected.TimeSeries.Values.SelectMany(it => it).ToArray()));
    Assert.That(actual.Urn, Is.EqualTo(expected.Urn));
    Assert.That(actual.At, Is.EqualTo(expected.At));
  }
}