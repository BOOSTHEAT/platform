using ImpliciX.DesktopServices.Services;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services.TargetSystemTests;

public class LoopbackTargetSystemTests
{
  private string _destinationFolder = String.Empty;
  private LoopbackTargetSystem _lts = new("");

  [SetUp]
  public void Init()
  {
    _destinationFolder = Path.Combine(Path.GetTempPath(), "LoopbackTargetSystemTests", Path.GetRandomFileName());
    Directory.CreateDirectory(_destinationFolder);
    Directory.CreateDirectory(((LoopbackTargetSystem.ColdStorageDownloadCapability)_lts.MetricsColdStorageDownload)
      .ColdFinishedFolder);
  }

  [TearDown]
  public void Cleanup()
  {
    Directory.Delete(_destinationFolder, true);
    Directory.Delete(
      ((LoopbackTargetSystem.ColdStorageDownloadCapability)_lts.MetricsColdStorageDownload).ColdFinishedFolder, true);
  }

  [Test]
  public void MetricsColdStorageDownload()
  {
    var sut = _lts.MetricsColdStorageDownload;
    var finishFolder = ((LoopbackTargetSystem.ColdStorageDownloadCapability)_lts.MetricsColdStorageDownload)
      .ColdFinishedFolder;
    Assert.That(sut.IsAvailable, Is.True);
    File.WriteAllText(Path.Combine(finishFolder, "file1.metrics.zip"), "A");
    File.WriteAllText(Path.Combine(finishFolder, "file2.metrics.zip"), "B");
    File.WriteAllText(Path.Combine(finishFolder, "whatever"), "");
    var download = sut.Execute().AndSaveManyTo(_destinationFolder).ToBlockingEnumerable().ToArray();
    Check.That(download).IsEqualTo(new[]
    {
      (1, 2, "file1.metrics.zip", "559aead08264d5795d3909718cdd05abd49572e84fe55590eef31a88a08fdffd"),
      (2, 2, "file2.metrics.zip", "df7e70e5021544f4834bbee64a9e3789febc4be81470df629cad6ddb03320a5c")
    });
    AssertFileExistsAndContains(Path.Combine(_destinationFolder, "file1.metrics.zip"), "A");
    AssertFileExistsAndContains(Path.Combine(_destinationFolder, "file2.metrics.zip"), "B");
  }

  private void AssertFileExistsAndContains(string path, string content)
  {
    Assert.That(path, Does.Exist);
    Assert.That(File.ReadAllText(path), Is.EqualTo(content));
  }
}
