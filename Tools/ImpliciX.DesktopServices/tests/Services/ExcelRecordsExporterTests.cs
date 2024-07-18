using System.Reflection;
using ClosedXML.Excel;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.DesktopServices.Services;
using NFluent;
using DataPointValue = ImpliciX.Data.Records.ColdRecords.DataPointValue;

// ReSharper disable InconsistentNaming

namespace ImpliciX.DesktopServices.Tests.Services;

public class ExcelRecordsExporterTests
{
  private static readonly string
    StorageFolderPath = Path.Combine(Path.GetTempPath(), nameof(ExcelRecordsExporterTests));

  private static readonly DateTime _day = new(2023, 10, 26);
  private static readonly DateTime _dt0 = _day.Add(new TimeSpan(11, 57, 44));
  private static readonly DateTime _dt1 = _day.Add(new TimeSpan(18, 42, 25));
  private static readonly DateTime _dt2 = _day.Add(new TimeSpan(21, 0, 0));
  private static readonly DateTime _dt3 = _day.Add(new TimeSpan(22, 0, 0));
  private static readonly TimeSpan _ts0 = CreateTime(_dt0);
  private static readonly TimeSpan _ts1 = CreateTime(_dt1);
  private static readonly TimeSpan _ts2 = CreateTime(_dt2);
  private static readonly TimeSpan _ts3 = CreateTime(_dt3);

  private static TimeSpan CreateTime(DateTime dt) => new(dt.Ticks);

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(StorageFolderPath))
      Directory.Delete(StorageFolderPath, true);
  }

  [Test]
  public void GivenOneRecordFileExists_WhenIExecute_ThenIConvertDataPointToOneWorksheetSuccessfully()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "records.xlsx");
    new ExcelRecordsExporter(workingDirPath, outputFileFullPath).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(1);
    CheckWorksheetInfoExpectedFor_ColdFile_1(workbook.Worksheets.ElementAt(0));
  }

  [Test]
  public void GivenMultipleRecordFiles_For_TheSame_Record()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);
    CreateColdFile_1_bis(workingDirPath);

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "records.xlsx");
    new ExcelRecordsExporter(workingDirPath, outputFileFullPath).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(1);
    var sheet = workbook.Worksheets.ElementAt(0);
    Check.That(sheet.Name).IsEqualTo(SomeLongUrn[^31..].Replace(':', '\ua789'));
    Check.That(LoadSheet(sheet)).IsEqualTo(new[]
    {
      new XLCellValue[] {"Identifier", "At", "fizz", "buzz", "fizzbuzz", "fizzbuzzqix"},
      new XLCellValue[] {"1", ToXlDt(_dt1), 1f, "Once upon a time, there was a buzz in the air. It was a nice buzz."},
      new XLCellValue[] {"2", ToXlDt(_dt2), 3f, "When suddenly, a buzz came in.", "EnumValue"},
      new XLCellValue[] {"3", ToXlDt(_dt3), 4f, "HelloWorld", "OtherEnumValue", "This is great!"}
    });
  }

  [Test]
  public void GivenMultipleRecordFileExists_WhenIExecute_ThenIConvertDataPointToTwoWorksheetsSuccessfully()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);
    CreateColdFile_2(workingDirPath);

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "records.xlsx");
    new ExcelRecordsExporter(workingDirPath, outputFileFullPath).Execute();

    // Then
    var workbook = new XLWorkbook(outputFileFullPath);
    Check.That(workbook.Worksheets).HasSize(2);
    CheckWorksheetInfoExpectedFor_ColdFile_1(workbook.Worksheets.ElementAt(0));
    CheckWorksheetInfoExpectedFor_ColdFile_2(workbook.Worksheets.ElementAt(1));
  }

  #region Helpers

  private const string SomeLongUrn = "foo:bar:qix:bar:foo:bar:qix:baro";
  private static void CreateColdFile_1(string destinationDirPath)
  {
    using var coldMetricsFile = ColdRecordsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m1{ColdRecordsDb.FileExtension}"),
      SomeLongUrn);
    coldMetricsFile.WriteDataPoint(new RecordsDataPoint(1, _ts1, new[]
    {
      new DataPointValue($"fizz", FieldType.Float, 1f),
      new DataPointValue($"buzz", FieldType.String,
        "Once upon a time, there was a buzz in the air. It was a nice buzz."),
    }));

    coldMetricsFile.WriteDataPoint(new RecordsDataPoint(2, _ts2, new[]
    {
      new DataPointValue($"fizz", FieldType.Float, 3f),
      new DataPointValue($"buzz", FieldType.String, "When suddenly, a buzz came in."),
      new DataPointValue($"fizzbuzz", FieldType.Enum, "EnumValue")
    }));
  }

  private static void CreateColdFile_1_bis(string destinationDirPath)
  {
    using var coldMetricsFile = ColdRecordsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m1_bis{ColdRecordsDb.FileExtension}"),
      SomeLongUrn);

    coldMetricsFile.WriteDataPoint(new RecordsDataPoint(3, _ts3, new[]
    {
      new DataPointValue($"fizz", FieldType.Float, 4f),
      new DataPointValue($"buzz", FieldType.String, "HelloWorld"),
      new DataPointValue($"fizzbuzz", FieldType.Enum, "OtherEnumValue"),
      new DataPointValue($"fizzbuzzqix", FieldType.String, "This is great!"),
    }));
  }

  private void CheckWorksheetInfoExpectedFor_ColdFile_1(IXLWorksheet sheet)
  {
    Check.That(sheet.Name).IsEqualTo(SomeLongUrn[^31..].Replace(':', '\ua789'));
    Check.That(LoadSheet(sheet)).IsEqualTo(new[]
    {
      new XLCellValue[] {"Identifier", "At", "fizz", "buzz", "fizzbuzz"},
      new XLCellValue[] {"1", ToXlDt(_dt1), 1f, "Once upon a time, there was a buzz in the air. It was a nice buzz."},
      new XLCellValue[] {"2", ToXlDt(_dt2), 3f, "When suddenly, a buzz came in.", "EnumValue"}
    });
  }

  private static void CreateColdFile_2(string destinationDirPath)
  {
    using var coldMetricsFile = ColdRecordsDb.NewCollection(
      Path.Combine(destinationDirPath, $"m2{ColdRecordsDb.FileExtension}"),
      "just:me");
    coldMetricsFile.WriteDataPoint(new RecordsDataPoint(1, _ts3, new[]
    {
      new DataPointValue("me", FieldType.Float, 66f),
      new DataPointValue("and:you", FieldType.Float, 67f),
    }));
  }

  private void CheckWorksheetInfoExpectedFor_ColdFile_2(IXLWorksheet sheet)
  {
    Check.That(sheet.Name).IsEqualTo("just\ua789me");
    var loadSheet = LoadSheet(sheet);
    Check.That(loadSheet).IsEqualTo(new[]
    {
      new XLCellValue[] {"Identifier", "At", "me", "and:you"},
      new XLCellValue[] {"1", ToXlDt(_dt3), 66f, 67f},
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
