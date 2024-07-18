using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.ColdMetrics;

[NonParallelizable]
public class ColdMetricsCollectionTests
{
  private static readonly Urn MetricUrn = "foo:bar";


  public static object[] BaseLineCases =
  {
    new object[] {86_400, 1},
    new object[] {86_400, 10},
    new object[] {86_400, 100}
  };
  private ColdCollection<MetricsDataPoint> Sut { get; set; }

  [SetUp]
  public void Setup()
  {
    Sut = ColdMetricsDb.NewCollection(Path.GetTempFileName(), MetricUrn);
  }

  [TearDown]
  public void TearDown()
  {
    Sut.Dispose();
    File.Delete(Sut.FilePath);
  }


  [Test]
  public void it_should_write_the_protocol_version_as_byte_at_position_zero_of_the_file()
  {
    Sut.Dispose();
    var bytes = File.ReadAllBytes(Sut.FilePath);
    Assert.That(bytes[0], Is.EqualTo(1));
    Assert.That(Sut.Protocol.Version, Is.EqualTo(1));
  }


  [Test]
  public void it_writes_metric_urn_metadata()
  {
    Sut.Dispose();
    Sut = ColdMetricsDb.LoadCollection(Sut.FilePath);
    Assert.That(Sut.MetaData.Urn, Is.EqualTo(Sut.MetaData.Urn));
  }

  [Test]
  public void it_writes_data_point_nominal_case()
  {
    var dp1 = new MetricsDataPoint(TimeSpan.FromSeconds(1), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f),
      new DataPointValue("foo:bar:buzz", 2f)
    }, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    Sut.WriteDataPoint(dp1);
    var actualDataPoints = Sut.DataPoints.ToArray();
    Assert.Multiple(() =>
    {
      Assert.That(Sut.MetaData.Urn, Is.EqualTo(MetricUrn));
      Assert.That(Sut.MetaData.DataPointsCount, Is.EqualTo(1));
      Assert.That(Sut.MetaData.FirstDataPointTime, Is.EqualTo(dp1.At));
      Assert.That(Sut.MetaData.LastDataPointTime, Is.EqualTo(dp1.At));
      Assert.That(Sut.MetaData.PropertyDescriptors!.Select(d => d.Urn)
        , Is.EquivalentTo(new Urn[] {"foo:bar:fizz", "foo:bar:buzz"}));
    });

    Assert.Multiple(() =>
    {
      Assert.That(actualDataPoints, Has.Length.EqualTo(1));
      Assert.That(actualDataPoints[0].At, Is.EqualTo(dp1.At));
      Assert.That(actualDataPoints[0].SampleStartTime, Is.EqualTo(dp1.SampleStartTime));
      Assert.That(actualDataPoints[0].SampleEndTime, Is.EqualTo(dp1.SampleEndTime));
      Assert.That(actualDataPoints[0].Values, Is.EquivalentTo(dp1.Values));
    });
  }

  [Test]
  public void
    it_writes_datapoints_when_for_a_given_metric_we_receive_a_data_point_with_more_properties_than_the_previous()
  {
    var dp1 = new MetricsDataPoint(TimeSpan.FromSeconds(1), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f),
      new DataPointValue("foo:bar:buzz", 2f)
    }, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    var dp2 = new MetricsDataPoint(TimeSpan.FromSeconds(2), new[]
    {
      new DataPointValue("foo:bar:fizz", 2f),
      new DataPointValue("foo:bar:buzz", 3f),
      new DataPointValue("foo:bar:fizzbuzz", 3f)
    }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

    Sut.WriteDataPoint(dp1);
    Sut.WriteDataPoint(dp2);

    var actualDataPoints = Sut.DataPoints.ToArray();
    Assert.Multiple(() =>
    {
      Assert.That(Sut.MetaData.Urn, Is.EqualTo(MetricUrn));
      Assert.That(Sut.MetaData.DataPointsCount, Is.EqualTo(2));
      Assert.That(Sut.MetaData.FirstDataPointTime, Is.EqualTo(dp1.At));
      Assert.That(Sut.MetaData.LastDataPointTime, Is.EqualTo(dp2.At));
      Assert.That(Sut.MetaData.PropertyDescriptors!.Select(d => d.Urn)
        , Is.EquivalentTo(new Urn[] {"foo:bar:fizz", "foo:bar:buzz", "foo:bar:fizzbuzz"}));
    });

    Assert.Multiple(() =>
    {
      Assert.That(actualDataPoints, Has.Length.EqualTo(2));
      Assert.That(actualDataPoints[0].At, Is.EqualTo(dp1.At));
      Assert.That(actualDataPoints[0].SampleStartTime, Is.EqualTo(dp1.SampleStartTime));
      Assert.That(actualDataPoints[0].SampleEndTime, Is.EqualTo(dp1.SampleEndTime));
      Assert.That(actualDataPoints[0].Values, Is.EquivalentTo(dp1.Values));
      Assert.That(actualDataPoints[1].At, Is.EqualTo(dp2.At));
      Assert.That(actualDataPoints[1].Values, Is.EquivalentTo(dp2.Values));
      Assert.That(actualDataPoints[1].SampleStartTime, Is.EqualTo(dp2.SampleStartTime));
      Assert.That(actualDataPoints[1].SampleEndTime, Is.EqualTo(dp2.SampleEndTime));
    });
  }

  [Test]
  public void
    it_writes_data_points_when_for_a_given_metric_we_receive_a_data_point_with_less_properties_than_the_previous()
  {
    var dp1 = new MetricsDataPoint(TimeSpan.FromSeconds(1), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f),
      new DataPointValue("foo:bar:buzz", 2f),
      new DataPointValue("foo:bar:fizzbuzz", 3f)
    }, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    var dp2 = new MetricsDataPoint(TimeSpan.FromSeconds(2), new[]
    {
      new DataPointValue("foo:bar:buzz", 4f)
    }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

    Sut.WriteDataPoint(dp1);
    Sut.WriteDataPoint(dp2);

    var actualDataPoints = Sut.DataPoints.ToArray();
    Assert.Multiple(() =>
    {
      Assert.That(Sut.MetaData.Urn, Is.EqualTo(MetricUrn));
      Assert.That(Sut.MetaData.DataPointsCount, Is.EqualTo(2));
      Assert.That(Sut.MetaData.FirstDataPointTime, Is.EqualTo(dp1.At));
      Assert.That(Sut.MetaData.LastDataPointTime, Is.EqualTo(dp2.At));
      Assert.That(Sut.MetaData.PropertyDescriptors!.Select(d => d.Urn)
        , Is.EquivalentTo(new Urn[] {"foo:bar:fizz", "foo:bar:buzz", "foo:bar:fizzbuzz"}));
    });

    Assert.Multiple(() =>
    {
      Assert.That(actualDataPoints, Has.Length.EqualTo(2));
      Assert.That(actualDataPoints[0].At, Is.EqualTo(dp1.At));
      Assert.That(actualDataPoints[0].SampleStartTime, Is.EqualTo(dp1.SampleStartTime));
      Assert.That(actualDataPoints[0].SampleEndTime, Is.EqualTo(dp1.SampleEndTime));
      Assert.That(actualDataPoints[0].Values, Is.EquivalentTo(dp1.Values));
      Assert.That(actualDataPoints[1].At, Is.EqualTo(dp2.At));
      Assert.That(actualDataPoints[1].Values, Is.EquivalentTo(new DataPointValue[]
      {
        new("foo:bar:buzz", 4f)
      }));
      Assert.That(actualDataPoints[1].SampleStartTime, Is.EqualTo(dp2.SampleStartTime));
      Assert.That(actualDataPoints[1].SampleEndTime, Is.EqualTo(dp2.SampleEndTime));
    });
  }

  [Test]
  public void
    it_writes_data_points_when_for_given_metric_we_receive_a_data_point_with_a_different_set_of_properties_than_the_previous()
  {
    var dp1 = new MetricsDataPoint(TimeSpan.FromSeconds(1), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f),
      new DataPointValue("foo:bar:buzz", 2f)
    }, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    var dp2 = new MetricsDataPoint(TimeSpan.FromSeconds(2), new[]
    {
      new DataPointValue("foo:bar:qix", 4f),
      new DataPointValue("foo:bar:pix", 5f)
    }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

    Sut.WriteDataPoint(dp1);
    Sut.WriteDataPoint(dp2);

    var actualDataPoints = Sut.DataPoints.ToArray();
    Assert.Multiple(() =>
    {
      Assert.That(Sut.MetaData.Urn, Is.EqualTo(MetricUrn));
      Assert.That(Sut.MetaData.DataPointsCount, Is.EqualTo(2));
      Assert.That(Sut.MetaData.FirstDataPointTime, Is.EqualTo(dp1.At));
      Assert.That(Sut.MetaData.LastDataPointTime, Is.EqualTo(dp2.At));
      Assert.That(Sut.MetaData.PropertyDescriptors!.Select(d => d.Urn)
        , Is.EquivalentTo(new Urn[] {"foo:bar:fizz", "foo:bar:buzz", "foo:bar:qix", "foo:bar:pix"}));
    });

    Assert.Multiple(() =>
    {
      Assert.That(actualDataPoints, Has.Length.EqualTo(2));
      Assert.That(actualDataPoints[0].At, Is.EqualTo(dp1.At));
      Assert.That(actualDataPoints[0].SampleStartTime, Is.EqualTo(dp1.SampleStartTime));
      Assert.That(actualDataPoints[0].SampleEndTime, Is.EqualTo(dp1.SampleEndTime));
      Assert.That(actualDataPoints[0].Values, Is.EquivalentTo(new DataPointValue[]
      {
        new("foo:bar:fizz", 1f),
        new("foo:bar:buzz", 2f)
      }));
      Assert.That(actualDataPoints[1].At, Is.EqualTo(dp2.At));
      Assert.That(actualDataPoints[1].Values, Is.EquivalentTo(new DataPointValue[]
      {
        new("foo:bar:qix", 4f),
        new("foo:bar:pix", 5f)
      }));
      Assert.That(actualDataPoints[1].SampleStartTime, Is.EqualTo(dp2.SampleStartTime));
      Assert.That(actualDataPoints[1].SampleEndTime, Is.EqualTo(dp2.SampleEndTime));
    });
  }

  [Test]
  public void reload_file_with_datapoints_and_metadata()
  {
    var dp1 = new MetricsDataPoint(TimeSpan.FromSeconds(1), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f)
    }, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    var dp2 = new MetricsDataPoint(TimeSpan.FromSeconds(2), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f)
    }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

    Sut.WriteDataPoint(dp1);
    Sut.WriteDataPoint(dp2);
    Sut.Dispose();
    Sut = ColdMetricsDb.LoadCollection(Sut.FilePath);
    Assert.Multiple(() =>
    {
      Assert.That(Sut.MetaData.Urn, Is.EqualTo(MetricUrn));
      Assert.That(Sut.MetaData.DataPointsCount, Is.EqualTo(2));
      Assert.That(Sut.MetaData.FirstDataPointTime, Is.EqualTo(dp1.At));
      Assert.That(Sut.MetaData.LastDataPointTime, Is.EqualTo(dp2.At));
      Assert.That(Sut.MetaData.PropertyDescriptors!.Select(d => d.Urn)
        , Is.EquivalentTo(new Urn[] {"foo:bar:fizz"}));
    });

    var actualDataPoints = Sut.DataPoints.ToArray();
    Assert.Multiple(() =>
    {
      Assert.That(actualDataPoints, Has.Length.EqualTo(2));
      Assert.That(actualDataPoints[0].At, Is.EqualTo(dp1.At));
      Assert.That(actualDataPoints[0].Values, Is.EquivalentTo(dp1.Values));
      Assert.That(actualDataPoints[1].At, Is.EqualTo(dp2.At));
      Assert.That(actualDataPoints[1].Values, Is.EquivalentTo(dp2.Values));
    });
  }

  [Test]
  public void it_writes_data_points_in_exiting_file()
  {
    var dp1 = new MetricsDataPoint(TimeSpan.FromSeconds(1), new[]
    {
      new DataPointValue("foo:bar:fizz", 1f),
      new DataPointValue("foo:bar:buzz", 2f)
    }, TimeSpan.Zero, TimeSpan.FromSeconds(1));

    var dp2 = new MetricsDataPoint(TimeSpan.FromSeconds(2), new[]
    {
      new DataPointValue("foo:bar:fizz", 2f),
      new DataPointValue("foo:bar:buzz", 3f),
      new DataPointValue("foo:bar:fizzbuzz", 3f)
    }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));

    var dp3 = new MetricsDataPoint(TimeSpan.FromSeconds(3), new[]
    {
      new DataPointValue("foo:bar:fizz", 4f),
      new DataPointValue("foo:bar:buzz", 5f)
    }, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3));

    Sut.WriteDataPoint(dp1);
    Sut.WriteDataPoint(dp2);

    Sut.Dispose();
    Sut = ColdMetricsDb.LoadCollection(Sut.FilePath);

    Sut.WriteDataPoint(dp3);

    var actualDataPoints = Sut.DataPoints.ToArray();
    Assert.That(actualDataPoints, Has.Length.EqualTo(3));
    Assert.That(Sut.MetaData.PropertyDescriptors!.Select(d => d.Urn),
      Is.EquivalentTo(new Urn[] {"foo:bar:fizz", "foo:bar:buzz", "foo:bar:fizzbuzz"}));
    Assert.That(Sut.MetaData.FirstDataPointTime, Is.EqualTo(dp1.At));
    Assert.That(Sut.MetaData.LastDataPointTime, Is.EqualTo(dp3.At));
    Assert.That(Sut.MetaData.DataPointsCount, Is.EqualTo(3));
  }

  [Test]
  public void it_should_throw_when_datapoint_has_to_many_properties_that_cant_be_stored_in_header()
  {
    var dps = GenerateDataPoints(1, Sut.Protocol.MaxNumberOfPropertiesPerDataPoint + 1, TimeSpan.Zero).ToArray();
    Assert.Throws<InvalidOperationException>(() => Sut.WriteDataPoint(dps[0]));
  }

  [Test]
  [Category("bug")]
  public void it_should_be_able_to_read_incomplete_files()
  {
    //this can occur when the process is killed while writing the file
    //in this commit we force the flush to disk when disposing the file which is fine when the process is stopped normally (SIGTERM)
    //but the file might be incomplete if the process is killed before the flush is completed (SIGKILL)
    //same problem can occur in case of power outage
    var cf = ColdMetricsDb.LoadCollection(Path.Combine("ColdMetrics", "examples", "bug_incomplete_file.metrics"));
    var actualDataPoints = cf.DataPoints.ToArray();
    Assert.That(actualDataPoints, Has.Length.EqualTo(7972));
  }

  [TestCaseSource(nameof(BaseLineCases))]
  [Category("ExcludeFromCI")]
  [Ignore("performance test")]
  public void generate_big_files(int numberOfPoints, int numberOfProperties)
  {
    var sut = ColdMetricsDb.NewCollection($"/tmp/run5/bigfile_{numberOfProperties}_{numberOfPoints}.metrics",
      "foo:bar");
    var dpSampleStart = TimeSpan.FromTicks(DateTime.UtcNow.Ticks);

    var dataPoints = GenerateDataPoints(numberOfPoints, numberOfProperties, dpSampleStart).ToArray();
    dataPoints
      .Select((dp, i) => (dp, i))
      .ToList()
      .ForEach(t =>
      {
        sut.WriteDataPoint(t.dp);
      });

    sut.Dispose();

    Zip.CreateZipFromFiles(new[] {sut.FilePath}, $"{sut.FilePath}.zip");
    var reloaded = ColdMetricsDb.LoadCollection(sut.FilePath);

    var dataPoints2 = reloaded.DataPoints.ToArray();
    Assert.That(dataPoints2, Has.Length.EqualTo(numberOfPoints));
    Assert.That(dataPoints2[0].At, Is.EqualTo(dataPoints[0].At));
    Assert.That(dataPoints2[0].Values, Is.EquivalentTo(dataPoints[0].Values));
    Assert.That(dataPoints2[10].At, Is.EqualTo(dataPoints[10].At));
    Assert.That(dataPoints2[10].Values, Is.EquivalentTo(dataPoints[10].Values));
  }

  private static IEnumerable<MetricsDataPoint> GenerateDataPoints(int numberOfPoints, int numberOfProperties,
    TimeSpan dpSampleStart)
  {
    var rnd = new Random(42);
    for (var i = 0; i < numberOfPoints; i++)
    {
      var dpSampleEnd = dpSampleStart.Add(TimeSpan.FromSeconds(1));
      var props = Enumerable.Range(0, numberOfProperties)
        .Select(j => new DataPointValue($"foo:bar:_{j}", rnd.NextSingle()))
        .ToArray();

      yield return new MetricsDataPoint(dpSampleStart, props, dpSampleStart, dpSampleEnd);
      dpSampleStart = dpSampleEnd;
    }
  }
}
