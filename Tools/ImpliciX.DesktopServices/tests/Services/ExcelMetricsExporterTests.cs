using System.Reflection;
using ClosedXML.Excel;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.DesktopServices.Services;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services;

public class ExcelMetricsExporterTests
{
  private static readonly string
    StorageFolderPath = Path.Combine(Path.GetTempPath(), nameof(ExcelMetricsExporterTests));

  private static readonly DateTime _day = new(2023, 10, 26);
  private static readonly DateTime _dt0 = _day.Add(new TimeSpan(11, 57, 44));
  private static readonly DateTime _dt1 = _day.Add(new TimeSpan(18, 42, 25));
  private static readonly DateTime _dt2 = _day.Add(new TimeSpan(21, 0, 0));
  private static readonly DateTime _dt3 = _day.Add(new TimeSpan(22, 0, 0));
  private static readonly DateTime _dt4 = _day.Add(new TimeSpan(25, 0, 0));
  private static readonly TimeSpan _ts0 = CreateMetricDateTime(_dt0);
  private static readonly TimeSpan _ts1 = CreateMetricDateTime(_dt1);
  private static readonly TimeSpan _ts2 = CreateMetricDateTime(_dt2);
  private static readonly TimeSpan _ts3 = CreateMetricDateTime(_dt3);
  private static readonly TimeSpan _ts4 = CreateMetricDateTime(_dt4);

  private static TimeSpan CreateMetricDateTime(DateTime dt) => new(dt.Ticks);

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(StorageFolderPath))
      Directory.Delete(StorageFolderPath, true);
  }

  [Test]
  public void GivenOneMetricsFileExists_WhenIExecute_ThenIConvertDataPointToOneWorksheetSuccessfully()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.xlsx");
    new ExcelMetricsExporter(workingDirPath, outputFileFullPath).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(1);
    CheckWorksheetInfoExpectedFor_ColdFile_1(workbook.Worksheets.ElementAt(0));
  }

  [Test]
  public void GivenMultipleMetricsFileExists_WhenIExecute_ThenIConvertDataPointToTwoWorksheetsSuccessfully()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);
    CreateColdFile_2(workingDirPath);
    CreateColdFile_3(workingDirPath);

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.xlsx");
    new ExcelMetricsExporter(workingDirPath, outputFileFullPath).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(3);
    CheckWorksheetInfoExpectedFor_ColdFile_1(workbook.Worksheets.ElementAt(0));
    CheckWorksheetInfoExpectedFor_ColdFile_2(workbook.Worksheets.ElementAt(1));
    CheckWorksheetInfoExpectedFor_ColdFile_3(workbook.Worksheets.ElementAt(2));
  }

  [Test]
  public void GivenMultipleMetricsFiles_For_TheSame_Metric()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);
    CreateColdFile_1_bis(workingDirPath);
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.xlsx");
    new ExcelMetricsExporter(workingDirPath, outputFileFullPath).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(1);
    var sheet = workbook.Worksheets.ElementAt(0);
    Check.That(sheet.Name).IsEqualTo(SomeLongUrn[^31..].Replace(':', '\ua789'));
    var loadSheet = LoadSheet(sheet);
    var expected = new[]
    {
      new XLCellValue[] {"At", "Start", "End", "foo:bar:qix:bar:foo:bar:qix:baro", "fizz", "buzz", "fizzbuzz"},
      new XLCellValue[] {ToXlDt(_dt1), ToXlDt(_dt0), ToXlDt(_dt1), Blank.Value, 1f, 2f},
      new XLCellValue[] {ToXlDt(_dt2), ToXlDt(_dt1), ToXlDt(_dt2), Blank.Value, 3f, 4f, 5f},
      new XLCellValue[] {ToXlDt(_dt4), ToXlDt(_dt3), ToXlDt(_dt4), Blank.Value, 1f, 2f},
    };
    Check.That(loadSheet).IsEqualTo(expected);
  }

  [Test]
  public void bug7138()
  {
    var workingDirPath = Path.Combine("Resources", "metricsCold");
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.xlsx");
    Assert.DoesNotThrow(() =>
    {
      new ExcelMetricsExporter(workingDirPath, outputFileFullPath).Execute();
    });
    var workbook = new XLWorkbook(outputFileFullPath);
    Assert.That(workbook.Worksheets.Count, Is.AtLeast(1));
  }

  [Test]
  [Ignore("Slow test")]
  public void TruncateExportWhenTooManyLines()
  {
    // Given
    var workingDirPath = CreateTempDir();
    {
      using var coldMetricsFile =
        ColdMetricsDb.NewCollection(Path.Combine(workingDirPath, $"verylong{ColdMetricsDb.FileExtension}"), "just:me");
      for (int i = 1; i < 1100000; i++)
      {
        coldMetricsFile.WriteDataPoint(new MetricsDataPoint(TimeSpan.FromTicks(i), new[]
        {
          new DataPointValue("just:me", i),
        }, TimeSpan.FromTicks(i - 1), TimeSpan.FromTicks(i)));
      }
    }

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.xlsx");
    new ExcelMetricsExporter(
      workingDirPath,
      outputFileFullPath,
      t => TestContext.Out.WriteLine(t)
    ).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(1);
    var sheet = LoadSheet(workbook.Worksheets.ElementAt(0)).ToArray();
    Assert.That(sheet.Length, Is.EqualTo(1048576));
  }


  #region Helpers

  private const string SomeLongUrn = "foo:bar:qix:bar:foo:bar:qix:baro";
  private static void CreateColdFile_1(string destinationDirPath)
  {
    using var coldMetricsFile = ColdMetricsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m1_bis{ColdMetricsDb.FileExtension}"),
      SomeLongUrn);
    coldMetricsFile.WriteDataPoint(new MetricsDataPoint(_ts1, new[]
    {
      new DataPointValue($"{SomeLongUrn}:fizz", 1f),
      new DataPointValue($"{SomeLongUrn}:buzz", 2f)
    }, _ts0, _ts1));

    coldMetricsFile.WriteDataPoint(new MetricsDataPoint(_ts2, new[]
    {
      new DataPointValue($"{SomeLongUrn}:fizz", 3f),
      new DataPointValue($"{SomeLongUrn}:buzz", 4f),
      new DataPointValue($"{SomeLongUrn}:fizzbuzz", 5f)
    }, _ts1, _ts2));
  }

  private static void CreateColdFile_1_bis(string destinationDirPath)
  {
    using var coldMetricsFile = ColdMetricsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m1{ColdMetricsDb.FileExtension}"),
      SomeLongUrn);
    coldMetricsFile.WriteDataPoint(new MetricsDataPoint(_ts4, new[]
    {
      new DataPointValue($"{SomeLongUrn}:fizz", 1f),
      new DataPointValue($"{SomeLongUrn}:buzz", 2f)
    }, _ts3, _ts4));
  }

  private void CheckWorksheetInfoExpectedFor_ColdFile_1(IXLWorksheet sheet)
  {
    Check.That(sheet.Name).IsEqualTo(SomeLongUrn[^31..].Replace(':', '\ua789'));
    Check.That(LoadSheet(sheet)).IsEqualTo(new[]
    {
      new XLCellValue[] {"At", "Start", "End", "foo:bar:qix:bar:foo:bar:qix:baro", "fizz", "buzz", "fizzbuzz"},
      new XLCellValue[] {ToXlDt(_dt1), ToXlDt(_dt0), ToXlDt(_dt1), Blank.Value, 1f, 2f},
      new XLCellValue[] {ToXlDt(_dt2), ToXlDt(_dt1), ToXlDt(_dt2), Blank.Value, 3f, 4f, 5f},
    });
  }

  private static void CreateColdFile_2(string destinationDirPath)
  {
    using var coldMetricsFile = ColdMetricsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m2{ColdMetricsDb.FileExtension}"),
      "just:me");
    coldMetricsFile.WriteDataPoint(new MetricsDataPoint(_ts3, new[]
    {
      new DataPointValue("just:me", 66f),
      new DataPointValue("just:me:and:you", 67f),
    }, _ts1, _ts2));
  }

  private void CheckWorksheetInfoExpectedFor_ColdFile_2(IXLWorksheet sheet)
  {
    Check.That(sheet.Name).IsEqualTo("just\ua789me");
    Check.That(LoadSheet(sheet)).IsEqualTo(new[]
    {
      new XLCellValue[] {"At", "Start", "End", "just:me", "and:you"},
      new XLCellValue[] {ToXlDt(_dt3), ToXlDt(_dt1), ToXlDt(_dt2), 66f, 67f},
    });
  }

  private static void CreateColdFile_3(string destinationDirPath)
  {
    using var coldMetricsFile = ColdMetricsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m3{ColdMetricsDb.FileExtension}"),
      "power:sum");
    coldMetricsFile.WriteDataPoint(new MetricsDataPoint(_ts3, new[]
    {
      new DataPointValue("power:sum:fizz", 33f),
      new DataPointValue("power:sum:buzz", 34f),
      new DataPointValue("power:sum:fizzbuzz", 35f)
    }, _ts1, _ts2));
  }

  private void CheckWorksheetInfoExpectedFor_ColdFile_3(IXLWorksheet sheet)
  {
    Check.That(sheet.Name).IsEqualTo("power\ua789sum");
    Check.That(LoadSheet(sheet)).IsEqualTo(new[]
    {
      new XLCellValue[] {"At", "Start", "End", "power:sum", "fizz", "buzz", "fizzbuzz"},
      new XLCellValue[] {ToXlDt(_dt3), ToXlDt(_dt1), ToXlDt(_dt2), Blank.Value, 33f, 34f, 35f},
    });
  }

  private static IEnumerable<IEnumerable<XLCellValue>> LoadSheet(IXLWorksheet sheet)
  {
    return sheet.Rows().Select(row => Enumerable.Range(1, row.Cells().Max(c => c.Address.ColumnNumber))
      .Select(column => row.Cells()
        .FirstOrDefault(c => c!.Address.ColumnNumber == column, null)?.Value ?? Blank.Value));
  }

  private static XLCellValue ToXlDt(DateTime dt) => XlCellValueFromSerialDateTime(double.Round(dt.ToOADate(), 10));

  private static XLCellValue XlCellValueFromSerialDateTime(double serial) =>
    (XLCellValue)typeof(XLCellValue)
      .GetMethod("FromSerialDateTime", BindingFlags.Static | BindingFlags.NonPublic)
      ?.Invoke(null, new object[] {serial})!;

  private static string CreateTempDir()
  {
    var tmpDirPath = Path.Combine(StorageFolderPath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
    Directory.CreateDirectory(tmpDirPath);
    return tmpDirPath;
  }

  #endregion
}
