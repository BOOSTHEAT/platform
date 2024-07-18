using ImpliciX.Data.ColdMetrics;
using ImpliciX.DesktopServices.Services;

namespace ImpliciX.DesktopServices.Tests.Services;

public class ExportTests
{
  private string _metricsFilePath = null!;
  private string _storageFolderPath = null!;

  [SetUp]
  public void SetUp()
  {
    _storageFolderPath = Path.Combine(Path.GetTempPath(), nameof(ExportTests));
    Directory.CreateDirectory(_storageFolderPath);
    _metricsFilePath = Path.Combine(_storageFolderPath, $"foo{ColdMetricsDb.FileExtension}");
    ColdMetricsDb.NewCollection(_metricsFilePath, "foo").Dispose();
  }

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(_storageFolderPath))
      Directory.Delete(_storageFolderPath, true);
  }

  [Test]
  public void MetricsExportToExcelCanBeCalledFromConcierge()
  {
    var concierge = new BaseConcierge(null);
    dynamic exporter = concierge.Export.MetricsToExcel(_storageFolderPath, "fizz");
    Assert.That(exporter, Is.InstanceOf<ExcelMetricsExporter>());
    Assert.That(exporter.WorkingDirPath, Is.EqualTo(_storageFolderPath));
    Assert.That(exporter.OutputFilePath, Is.EqualTo("fizz"));
  }

  [Test]
  public void RecordsExportToExcelCanBeCalledFromConcierge()
  {
    var concierge = new BaseConcierge(null);
    dynamic exporter = concierge.Export.RecordsToExcel(_storageFolderPath, "fizz");
    Assert.That(exporter, Is.InstanceOf<ExcelRecordsExporter>());
    Assert.That(exporter.WorkingDirPath, Is.EqualTo(_storageFolderPath));
    Assert.That(exporter.OutputFilePath, Is.EqualTo("fizz"));
  }
}
