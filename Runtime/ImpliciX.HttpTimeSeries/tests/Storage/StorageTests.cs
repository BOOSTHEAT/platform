using ImpliciX.HttpTimeSeries.Storage;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using static NUnit.Framework.Assert;
using static ImpliciX.HttpTimeSeries.Tests.Helpers.HttpTimeSeriesTestHelpers;
using PHD = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.HttpTimeSeries.Tests.Storage;

[NonParallelizable]
[Platform("Linux")]
public class StorageTests
{
  private static readonly string StorageFolderPath = Path.Combine(
    Path.GetTempPath(),
    Guid.NewGuid().ToString(),
    "indexed_cold_store"
  );

  private static readonly string Finished = Path.Combine(
    StorageFolderPath,
    "finished"
  );

  private ColdMetricsDbRepository _sut;

  [SetUp]
  public void Setup()
  {
    if (Directory.Exists(StorageFolderPath))
      Directory.Delete(
        StorageFolderPath,
        true
      );
    Directory.CreateDirectory(StorageFolderPath);
  }

  [Test]
  public void ApplyRetentionPolicyShouldRemoveOldFiles()
  {
    var rootUrn = Urn.BuildUrn("foo:g1");
    _sut = new ColdMetricsDbRepository(
      CreateFakeSeries(("foo:g1", 10)),
      StorageFolderPath
    );
    That(
      new FileInfo(Finished).Exists,
      Is.False
    );
    DataModelValue<MetricValue>[] Values =
    {
      PHD.CreateMetricValueProperty(
        rootUrn,
        1,
        TimeSpan.Zero,
        TimeSpan.FromHours(1)
      ),
      PHD.CreateMetricValueProperty(
        rootUrn,
        2,
        TimeSpan.Zero,
        TimeSpan.FromHours(2)
      )
    };
    _sut.WriteMany(
      rootUrn,
      Values
    );
    var files = Directory.GetFiles(StorageFolderPath);
    That(
      files.IsEmpty(),
      Is.False
    );
    That(
      files[0],
      Is.Not.Null
    );
    That(
      files.Length,
      Is.EqualTo(1)
    );

    That(
      new FileInfo(Finished).Exists,
      Is.False
    );

    DataModelValue<MetricValue>[] NewValues =
    {
      PHD.CreateMetricValueProperty(
        rootUrn,
        1,
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(50)
      )
    };
    _sut.WriteMany(
      rootUrn,
      NewValues
    );

    var finisedFiles = Directory.GetFiles(Finished);
    That(
      files.IsEmpty(),
      Is.False
    );
    That(
      files[0],
      Is.Not.Null
    );
    That(
      files.Length,
      Is.EqualTo(1)
    );
    finisedFiles = Directory.GetFiles(Finished);
    That(
      finisedFiles.IsEmpty(),
      Is.False
    );
    That(
      finisedFiles[0],
      Is.Not.Null
    );
    That(
      finisedFiles.Length,
      Is.EqualTo(1)
    );
    _sut.ApplyRetentionPolicy();
    finisedFiles = Directory.GetFiles(Finished);
    That(
      finisedFiles.IsEmpty(),
      Is.True
    );
    var r = _sut.Read(rootUrn).ToArray();
    That(
      r,
      Is.Not.Null
    );
    That(
      r.Length,
      Is.EqualTo(1)
    );
  }

  [Test]
  public void ApplyRetentionPolicyShouldNotRemoveOldFilesContainingRecentData()
  {
    var rootUrn = Urn.BuildUrn("foo:g1");
    
    _sut = new ColdMetricsDbRepository(
      CreateFakeSeries(("foo:g1",10)),
      StorageFolderPath
    );
    That(
      new FileInfo(Finished).Exists,
      Is.False
    );
    DataModelValue<MetricValue>[] Values =
    {
      PHD.CreateMetricValueProperty(
        rootUrn,
        1,
        TimeSpan.Zero,
        TimeSpan.FromHours(1)
      ),
      PHD.CreateMetricValueProperty(
        rootUrn,
        2,
        TimeSpan.Zero,
        TimeSpan.FromHours(2)
      )
    };
    _sut.WriteMany(
      rootUrn,
      Values
    );
    var files = Directory.GetFiles(StorageFolderPath);
    That(
      files.IsEmpty(),
      Is.False
    );
    That(
      files[0],
      Is.Not.Null
    );
    That(
      files.Length,
      Is.EqualTo(1)
    );

    That(
      new FileInfo(Finished).Exists,
      Is.False
    );

    DataModelValue<MetricValue>[] NewValues =
    {
      PHD.CreateMetricValueProperty(
        rootUrn,
        1,
        TimeSpan.FromHours(2),
        TimeSpan.FromHours(25)
      )
    };
    _sut.WriteMany(
      rootUrn,
      NewValues
    );

    var finisedFiles = Directory.GetFiles(Finished);
    That(
      files.IsEmpty(),
      Is.False
    );
    That(
      files[0],
      Is.Not.Null
    );
    That(
      files.Length,
      Is.EqualTo(1)
    );
    finisedFiles = Directory.GetFiles(Finished);
    That(
      finisedFiles.IsEmpty(),
      Is.False
    );
    That(
      finisedFiles[0],
      Is.Not.Null
    );
    That(
      finisedFiles.Length,
      Is.EqualTo(1)
    );
    _sut.ApplyRetentionPolicy();
    finisedFiles = Directory.GetFiles(Finished);
    That(
      finisedFiles.IsEmpty(),
      Is.False
    );
    That(
      finisedFiles[0],
      Is.Not.Null
    );
    That(
      finisedFiles.Length,
      Is.EqualTo(1)
    );
    var r = _sut.Read(rootUrn).ToArray();
    That(
      r,
      Is.Not.Null
    );
    That(
      r.Length,
      Is.EqualTo(3)
    );
  }
}
