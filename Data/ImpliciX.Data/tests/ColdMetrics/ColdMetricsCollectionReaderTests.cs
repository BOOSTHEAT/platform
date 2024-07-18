using System;
using System.Linq;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Data.Tests.Helpers;
using ImpliciX.Data.Tools;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.ColdMetrics;

public class ColdMetricsCollectionReaderTests
{
  private IStoreFolder _examples;
  [SetUp]
  public void Setup()
  {
    IStoreFolder coldMetrics = new TestStoreFolder("ColdMetrics");
    _examples = IStoreFolder.Combine(coldMetrics, "examples");
  }

  [Test]
  public void it_should_be_able_to_read_complete_files()
  {
    var completeFile = IStoreFile.CreateFile(_examples, "complete_file.metrics");
    var cf = ColdMetricsDb.LoadCollectionFromStore(completeFile);
    Assert.That(cf.MetaData.Urn.Value, Is.EqualTo("foo:bar:fizz"));
    Assert.That(cf.MetaData.PropertyDescriptors, Has.Length.EqualTo(1));
    var dataPoints = cf.DataPoints.ToArray();
    Assert.Multiple(() =>
    {
      Assert.That(dataPoints.Count, Is.EqualTo(2));
      Assert.That(dataPoints[0].At, Is.EqualTo(TimeSpan.FromSeconds(1)));
      Assert.That(dataPoints[1].At, Is.EqualTo(TimeSpan.FromSeconds(2)));
    });
  }

  [Test]
  [Category("bug")]
  public void it_should_be_able_to_read_incomplete_files()
  {
    var bugIncompleteFile = IStoreFile.CreateFile(_examples, "bug_incomplete_file.metrics");
    var cf = ColdMetricsDb.LoadCollectionFromStore(bugIncompleteFile);
    Assert.That(cf.MetaData.Urn.Value, Is.EqualTo("system:metrics:electrical:heat_service_state"));
    Assert.That(cf.MetaData.PropertyDescriptors, Has.Length.EqualTo(15));
    var actualDataPoints = cf.DataPoints.ToArray();
    Assert.That(actualDataPoints, Has.Length.EqualTo(7972));
  }
}
