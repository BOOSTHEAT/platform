using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Model;
using NUnit.Framework;
using PHD = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Data.Tests.ColdMetrics;

[NonParallelizable]
[Platform("Linux")]
public class IndexedColdMetricsDbTests
{
  private static readonly string StorageFolderPath = Path.Combine(
    Path.GetTempPath(),
    Guid.NewGuid().ToString(),
    "indexed_cold_store"
  );

  [SetUp]
  public void Setup()
  {
    if (Directory.Exists(StorageFolderPath))
      Directory.Delete(
        StorageFolderPath,
        true
      );
  }

  [Test]
  public void it_should_start_new_file()
  {
    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar" },
      StorageFolderPath
    );
    Assert.That(
      File.Exists(sut.CurrentFiles[0]),
      Is.True
    );
  }

  [Test]
  public void it_should_store_series()
  {
    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );
    var data = new[]
    {
      PHD.CreateMetricValueProperty(
        "foo:bar:fizz",
        1,
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1)
      )
    };

    sut.WriteMany(
      "foo:bar:fizz",
      data
    );
    sut.Dispose();
    var dataPoints = ExtractDataPoints(sut.CurrentFiles[0]);
    Assert.Multiple(
      () =>
      {
        Assert.That(
          dataPoints,
          Has.Count.EqualTo(1)
        );
        Assert.That(
          dataPoints[0].At,
          Is.EqualTo(TimeSpan.FromSeconds(1))
        );
        Assert.That(
          dataPoints[0].Values,
          Is.EquivalentTo(
            new[]
            {
              new DataPointValue(
                "foo:bar:fizz",
                1f
              )
            }
          )
        );
      }
    );
  }

  [Test]
  public void it_should_store_series_in_chronological_order()
  {
    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );
    var data = new[]
    {
      PHD.CreateMetricValueProperty(
        "foo:bar:fizz",
        2,
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2)
      ),
      PHD.CreateMetricValueProperty(
        "foo:bar:fizz",
        1,
        TimeSpan.Zero,
        TimeSpan.FromSeconds(1)
      )
    };

    sut.WriteMany(
      "foo:bar:fizz",
      data
    );
    sut.Dispose();
    var dataPoints = ExtractDataPoints(sut.CurrentFiles[0]);
    Assert.Multiple(
      () =>
      {
        Assert.That(
          dataPoints.Count,
          Is.EqualTo(2)
        );
        Assert.That(
          dataPoints[0].At,
          Is.EqualTo(TimeSpan.FromSeconds(1))
        );
        Assert.That(
          dataPoints[1].At,
          Is.EqualTo(TimeSpan.FromSeconds(2))
        );
      }
    );
  }

  [Test]
  public void it_should_create_one_file_by_day_after_reloading()
  {
    var sut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );

    sut.WriteMany(
      "foo:bar:fizz",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.Zero,
          TimeSpan.FromHours(2)
        )
      }
    );

    sut.Dispose();
    sut =  IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar:fizz",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.Zero,
          TimeSpan.FromHours(25)
        )
      }
    );
    sut.Dispose();
    var finishedFiles = sut.FinishedFiles;

    Assert.Multiple(
      () =>
      {
        Assert.That(
          finishedFiles.Length,
          Is.EqualTo(1)
        );
        Assert.That(
          ExtractDataPoints(sut.CurrentFiles[0]),
          Has.Count.EqualTo(1)
        );
      }
    );
  }

  [Test]
  public void it_should_create_one_uncompressed_file_by_day_even_if_there_is_no_initial_data()
  {
    var sut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );
    var data = new[]
    {
      PHD.CreateMetricValueProperty(
        "foo:bar:fizz",
        1,
        TimeSpan.Zero,
        TimeSpan.FromHours(1)
      ),
      PHD.CreateMetricValueProperty(
        "foo:bar:fizz",
        2,
        TimeSpan.Zero,
        TimeSpan.FromHours(2)
      ),
      PHD.CreateMetricValueProperty(
        "foo:bar:fizz",
        3,
        TimeSpan.Zero,
        TimeSpan.FromHours(25)
      )
    };

    sut.WriteMany(
      "foo:bar:fizz",
      data
    );
    sut.Dispose();
    Assert.Multiple(
      () =>
      {
        Assert.That(
          sut.FinishedFiles.Length,
          Is.EqualTo(1)
        );
        Assert.That(
          sut.FinishedFiles[0],
          Does.Contain(".metrics")
        );
        Assert.That(
          ExtractDataPoints(sut.CurrentFiles[0]),
          Has.Count.EqualTo(1)
        );
      }
    );
  }

  [Test]
  public void it_is_able_to_continue_with_an_existing_file()
  {
    var sut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar:fizz",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );

    sut.Dispose();
    var newSut = IndexedColdMetricsDb.LoadOrCreate(
      new Urn[] { "foo:bar:fizz" },
      StorageFolderPath
    );
    newSut.WriteMany(
      "foo:bar:fizz",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );
    newSut.Dispose();
    Assert.That(
      ExtractDataPoints(sut.CurrentFiles[0]),
      Has.Count.EqualTo(2)
    );
  }

  [Test]
  public void it_loads_cold_stores_from_exising_files()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    var sut1 = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut1.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );

    sut1.Dispose();

    var sut2 = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut2.WriteMany(
      "zoo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "zoo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );

    sut2.Dispose();

    var sut3 = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    Assert.That(
      sut3.CurrentFiles,
      Has.Length.EqualTo(3)
    );
    sut3.Dispose();

    Assert.That(
      ExtractDataPoints(
        sut3.CurrentFiles,
        "foo:bar"
      ), Has.Count.EqualTo(1)
    );
    Assert.That(
      ExtractDataPoints(
        sut3.CurrentFiles,
        "zoo:bar"
      ), Has.Count.EqualTo(1)
    );
    Assert.That(
      ExtractDataPoints(
        sut3.CurrentFiles,
        "boo:bar"
      ), Has.Count.EqualTo(0)
    );
  }

  [Test]
  public void in_safe_mode_it_puts_corrupted_files_in_quarantine_and_dont_throw()
  {
    Directory.CreateDirectory(StorageFolderPath);
    File.WriteAllBytes(
      Path.Combine(
        StorageFolderPath,
        "corrupted_file.metrics"
      ), "toto"u8.ToArray()
    );
    Assert.DoesNotThrow(
      () =>
      {
        using var c = IndexedColdMetricsDb.LoadOrCreate(
          new Urn[] { "foo:bar:fizz" },
          StorageFolderPath,
          true
        );
      }
    );
    Assert.IsTrue(
      File.Exists(
        Path.Combine(
          StorageFolderPath,
          "quarantine",
          "corrupted_file.metrics"
        )
      )
    );
  }

  [Test]
  public void in_failfast_mode_it_puts_corrupted_files_in_quarantine_and_throw()
  {
    Directory.CreateDirectory(StorageFolderPath);
    File.WriteAllBytes(
      Path.Combine(
        StorageFolderPath,
        "corrupted_file.metrics"
      ), "toto"u8.ToArray()
    );
    Assert.Throws(
      Is.AssignableTo(typeof(Exception)),
      () => IndexedColdMetricsDb.LoadOrCreate(
        new Urn[] { "foo:bar:fizz" },
        StorageFolderPath
      )
    );
    Assert.IsTrue(
      File.Exists(
        Path.Combine(
          StorageFolderPath,
          "quarantine",
          "corrupted_file.metrics"
        )
      )
    );
  }

  [Test]
  public void it_creates_indexes()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut1 = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut1.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        )
      }
    );

    sut1.WriteMany(
      "zoo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "zoo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );

    sut1.Dispose();

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    Assert.That(
      sut.TimeIndex.ContainsMetric("foo:bar"),
      Is.True
    );
    Assert.That(
      sut.TimeIndex.ContainsMetric("zoo:bar"),
      Is.True
    );
    sut.Dispose();
  }

  [Test]
  public void it_updates_indexes()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        )
      }
    );

    sut.WriteMany(
      "zoo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "zoo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );
    Assert.That(
      sut.TimeIndex.ContainsMetric("foo:bar"),
      Is.True
    );
    Assert.That(
      sut.TimeIndex.ContainsMetric("zoo:bar"),
      Is.True
    );
  }

  [Test]
  public void read_many_should_return_empty_array_when_no_data()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    var query = new MetricQuery()
      .AddMetric(
        "foo:bar",
        "foo:bar:fizz"
      )
      .AddMetric(
        "zoo:bar",
        "zoo:bar:fizz"
      );
    var result = sut.ReadMany(query);
    Assert.That(
      result,
      Is.Empty
    );
    sut.Dispose();
  }

  [Test]
  public void read_many_should_return_data_from_the_current_collection()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        )
      }
    );

    sut.WriteMany(
      "zoo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "zoo:bar:fizz",
          3,
          TimeSpan.Zero,
          TimeSpan.FromHours(1)
        )
      }
    );
    var query = new MetricQuery()
      .AddMetric(
        "foo:bar",
        "foo:bar:fizz"
      )
      .AddMetric(
        "zoo:bar",
        "zoo:bar:fizz"
      );

    var result = sut.ReadMany(query).ToList();
    Assert.That(
      result,
      Is.EquivalentTo(
        new DataModelValue<MetricValue>[]
        {
          new (
            "foo:bar:fizz",
            new MetricValue(
              1,
              TimeSpan.Zero,
              TimeSpan.FromMinutes(1)
            ), TimeSpan.FromMinutes(1)
          ),
          new (
            "foo:bar:fizz",
            new MetricValue(
              2,
              TimeSpan.FromMinutes(1),
              TimeSpan.FromMinutes(2)
            ), TimeSpan.FromMinutes(2)
          ),
          new (
            "zoo:bar:fizz",
            new MetricValue(
              3,
              TimeSpan.Zero,
              TimeSpan.FromHours(1)
            ), TimeSpan.FromHours(1)
          )
        }
      )
    );
    sut.Dispose();
  }

  [Test]
  public void read_many_should_return_data_from_the_current_and_finished_collections()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.FromMinutes(2),
          TimeSpan.FromHours(25)
        )
      }
    );

    var query = new MetricQuery().AddMetric(
      "foo:bar",
      "foo:bar:fizz"
    );

    var result = sut.ReadMany(query).ToList();
    Assert.That(
      result,
      Is.EquivalentTo(
        new DataModelValue<MetricValue>[]
        {
          new (
            "foo:bar:fizz",
            new MetricValue(
              1,
              TimeSpan.Zero,
              TimeSpan.FromMinutes(1)
            ), TimeSpan.FromMinutes(1)
          ),
          new (
            "foo:bar:fizz",
            new MetricValue(
              2,
              TimeSpan.FromMinutes(1),
              TimeSpan.FromMinutes(2)
            ), TimeSpan.FromMinutes(2)
          ),
          new (
            "foo:bar:fizz",
            new MetricValue(
              3,
              TimeSpan.FromMinutes(2),
              TimeSpan.FromHours(25)
            ), TimeSpan.FromHours(25)
          )
        }
      )
    );
    sut.Dispose();
  }

  [Test]
  public void after_reloading_read_many_should_return_data_from_the_current_and_finished_collections()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.FromMinutes(2),
          TimeSpan.FromHours(25)
        )
      }
    );
    sut.Dispose();

    sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );

    var query = new MetricQuery().AddMetric(
      "foo:bar",
      "foo:bar:fizz"
    );
    var result = sut.ReadMany(query).ToList();

    Assert.That(
      result,
      Is.EquivalentTo(
        new DataModelValue<MetricValue>[]
        {
          new (
            "foo:bar:fizz",
            new MetricValue(
              1,
              TimeSpan.Zero,
              TimeSpan.FromMinutes(1)
            ), TimeSpan.FromMinutes(1)
          ),
          new (
            "foo:bar:fizz",
            new MetricValue(
              2,
              TimeSpan.FromMinutes(1),
              TimeSpan.FromMinutes(2)
            ), TimeSpan.FromMinutes(2)
          ),
          new (
            "foo:bar:fizz",
            new MetricValue(
              3,
              TimeSpan.FromMinutes(2),
              TimeSpan.FromHours(25)
            ), TimeSpan.FromHours(25)
          )
        }
      )
    );
    sut.Dispose();
  }

  [Test]
  public void read_many_should_apply_projections()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:buzz",
          4,
          TimeSpan.FromMinutes(3),
          TimeSpan.FromHours(4)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:qix",
          5,
          TimeSpan.FromMinutes(4),
          TimeSpan.FromHours(5)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.FromMinutes(2),
          TimeSpan.FromHours(25)
        )
      }
    );

    var query = new MetricQuery().AddMetric(
      "foo:bar",
      "foo:bar:buzz",
      "foo:bar:qix"
    );
    var result = sut.ReadMany(query).ToList();
    Assert.That(
      result,
      Has.Count.EqualTo(2)
    );
    Assert.That(
      result.Select(r => r.Urn).Distinct(),
      Is.EquivalentTo(
        new Urn[]
        {
          "foo:bar:buzz", "foo:bar:qix"
        }
      )
    );
    sut.Dispose();
  }

  [Test]
  public void read_many_when_no_specified_projections_all_properties_are_read()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.Zero,
          TimeSpan.FromMinutes(1)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:buzz",
          4,
          TimeSpan.FromMinutes(3),
          TimeSpan.FromHours(4)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:qix",
          5,
          TimeSpan.FromMinutes(4),
          TimeSpan.FromHours(5)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.FromMinutes(2),
          TimeSpan.FromHours(25)
        )
      }
    );

    var query = new MetricQuery().AddMetric("foo:bar");
    var result = sut.ReadMany(query).ToList();
    Assert.That(
      result,
      Has.Count.EqualTo(5)
    );
    Assert.That(
      result.Select(r => r.Urn).Distinct(),
      Is.EquivalentTo(
        new Urn[]
        {
          "foo:bar:fizz", "foo:bar:buzz", "foo:bar:qix"
        }
      )
    );
    sut.Dispose();
  }

  [Test]
  public void read_many_apply_filter_on_time_range()
  {
    Urn[] defs = { "foo:bar", "zoo:bar", "boo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(2),
          TimeSpan.FromMinutes(3)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.FromMinutes(3),
          TimeSpan.FromMinutes(4)
        )
      }
    );

    var query = new MetricQuery(
        TimeSpan.FromMinutes(3),
        TimeSpan.FromMinutes(4)
      )
      .AddMetric("foo:bar");

    var result = sut.ReadMany(query).ToList();
    Assert.That(
      result,
      Has.Count.EqualTo(2)
    );
    sut.Dispose();
  }

  [Test]
  public void read_many_apply_filter_on_time_range_by_considering_data_from_finished_files()
  {
    Urn[] defs = { "foo:bar" };

    using var sut = IndexedColdMetricsDb.LoadOrCreate(
      defs,
      StorageFolderPath
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          1,
          TimeSpan.FromMinutes(1),
          TimeSpan.FromMinutes(2)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          2,
          TimeSpan.FromMinutes(2),
          TimeSpan.FromMinutes(3)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          3,
          TimeSpan.FromMinutes(3),
          TimeSpan.FromMinutes(4)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          4,
          TimeSpan.FromMinutes(4),
          TimeSpan.FromMinutes(5)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:qix",
          5,
          TimeSpan.FromMinutes(5),
          TimeSpan.FromMinutes(6)
        )
      }
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          25,
          TimeSpan.FromHours(25),
          TimeSpan.FromHours(26)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          26,
          TimeSpan.FromHours(26),
          TimeSpan.FromHours(27)
        )
      }
    );
    sut.WriteMany(
      "foo:bar",
      new[]
      {
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          48,
          TimeSpan.FromHours(48),
          TimeSpan.FromHours(49)
        ),
        PHD.CreateMetricValueProperty(
          "foo:bar:fizz",
          49,
          TimeSpan.FromHours(49),
          TimeSpan.FromHours(50)
        )
      }
    );

    var query = new MetricQuery(
        TimeSpan.FromMinutes(4),
        TimeSpan.FromHours(26)
      )
      .AddMetric(
        "foo:bar",
        "foo:bar:fizz"
      );


    var result = sut.ReadMany(query).ToList();
    Assert.That(
      sut.FinishedFiles,
      Has.Length.EqualTo(2)
    );
    Assert.That(
      result,
      Is.EquivalentTo(
        new[]
        {
          new DataModelValue<MetricValue>(
            "foo:bar:fizz",
            new MetricValue(
              3,
              TimeSpan.FromMinutes(3),
              TimeSpan.FromMinutes(4)
            ), TimeSpan.FromMinutes(4)
          ),
          new DataModelValue<MetricValue>(
            "foo:bar:fizz",
            new MetricValue(
              4,
              TimeSpan.FromMinutes(4),
              TimeSpan.FromMinutes(5)
            ), TimeSpan.FromMinutes(5)
          ),
          new DataModelValue<MetricValue>(
            "foo:bar:fizz",
            new MetricValue(
              25,
              TimeSpan.FromHours(25),
              TimeSpan.FromHours(26)
            ), TimeSpan.FromHours(26)
          )
        }
      )
    );
    sut.Dispose();
  }

  private static List<MetricsDataPoint> ExtractDataPoints(
    string filePath
  )
  {
    using var c = ColdMetricsDb.LoadCollection(filePath);
    return c.DataPoints.ToList();
  }

  private static List<MetricsDataPoint> ExtractDataPoints(
    string[] files,
    string urn
  )
  {
    var results = new List<MetricsDataPoint>();
    foreach (var f in files)
    {
      using var c = ColdMetricsDb.LoadCollection(f);
      if (c.MetaData.Urn == urn)
      {
        results.AddRange(c.DataPoints);
        break;
      }
    }

    return results;
  }
}
