using System;
using System.Data;
using System.Linq;
using ClosedXML.Excel;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Language.Core;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services;

internal sealed class ExcelRecordsExporter : IAction
{
  private readonly ColdReader<RecordsDataPoint> _coldReader;
  private readonly Action<string> _log;
  [NotNull] internal readonly string OutputFilePath;
  [NotNull] internal readonly string WorkingDirPath;

  public ExcelRecordsExporter([NotNull] string workingDirPath, [NotNull] string outputFilePath,
    Action<string> log = null)
  {
    WorkingDirPath = workingDirPath ?? throw new ArgumentNullException(nameof(workingDirPath));
    OutputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
    _coldReader = ColdRecordsDb.CreateReader(workingDirPath);
    _log = log ?? (_ => { });
  }

  public Result<Unit> Execute()
  {
    try
    {
      using var workbook = new XLWorkbook();
      foreach (var (baseUrn, index) in _coldReader.Urns.Select((urn, index) => (urn, index)))
      {
        _log($"Exporting {baseUrn} ({index + 1}/{_coldReader.Urns.Length})");
        var sheetName = baseUrn.Length <= 31 ? baseUrn : baseUrn[^31..];
        var worksheetName = sheetName.Replace(':', '\ua789');
        var sheet = workbook.AddWorksheet(worksheetName);
        _log("--- Inserting Identifier in sheet");
        InsertIdInSheet(sheet, baseUrn, 1);
        _log("--- Inserting times in sheet");
        InsertTimesInSheet(sheet, baseUrn, 2);
        _log("--- Inserting values in sheet");
        InsertValuesInSheet(sheet, baseUrn);
      }

      _log("--- Start Saving workbook");
      workbook.SaveAs(OutputFilePath);
      return default(Unit);
    }
    catch (Exception e)
    {
      return Result<Unit>.Create(new Error("Excel export failed", e.Message));
    }
  }

  private void InsertIdInSheet(IXLWorksheet sheet, string baseUrn, int column)
  {
    var idTable = new DataTable();
    idTable.Columns.Add("Identifier", typeof(string));
    var dataPoints = _coldReader.ReadDataPoints(baseUrn);
    foreach (var dp in dataPoints)
    {
      var idRow = idTable.Rows.Add();
      idRow.SetField(0, dp.Id.ToString());
    }

    sheet.Columns(column, column).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    sheet.Cell(1, column).InsertTable(idTable.AsEnumerable());
    sheet.Columns(column, column).Width = 20.4;
  }

  private void InsertTimesInSheet(IXLWorksheet sheet, String baseUrn, int column)
  {
    var timeTable = new DataTable();
    timeTable.Columns.Add("At", typeof(DateTime));
    var dataPoints = _coldReader.ReadDataPoints(baseUrn);
    foreach (var dp in dataPoints)
    {
      var timeRow = timeTable.Rows.Add();
      timeRow.SetField(0, ToDateTime(dp.At));
    }

    sheet.Columns(column, column).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm:ss";
    sheet.Cell(1, column).InsertTable(timeTable.AsEnumerable());
    sheet.Columns(column, column).Width = 20.4;
  }

  private void InsertValuesInSheet(IXLWorksheet sheet, string baseUrn)
  {
    DataColumn GetDataColumn(PropertyDescriptor descriptor) =>
      (FieldType)descriptor.Type switch
      {
        FieldType.Enum => new DataColumn(descriptor.Urn, typeof(string)),
        FieldType.String => new DataColumn(descriptor.Urn, typeof(string)),
        FieldType.Float => new DataColumn(descriptor.Urn, typeof(float)),
        _ => throw new ArgumentOutOfRangeException()
      };

    var valueTable = new DataTable();
    var descriptors = _coldReader.GetPropertiesDescriptors(baseUrn);
    var properties = descriptors!
      .Select(u => u.Urn.Value)
      .ToArray();

    var columns = descriptors.Select(GetDataColumn).ToArray();
    valueTable.Columns.AddRange(columns);
    var propertyIndex = properties.Select((property, index) => (property, index))
      .ToDictionary(x => x.property, x => x.index);
    var dataPoints = _coldReader.ReadDataPoints(baseUrn);
    foreach (var dp in dataPoints)
    {
      var valueRow = valueTable.Rows.Add();
      foreach (var dpv in dp.Values)
        valueRow.SetField(propertyIndex[dpv.Urn], dpv.Value);
    }

    sheet.Cell(1, 3).InsertTable(valueTable.AsEnumerable());
    sheet.Columns(3, 1 + valueTable.Columns.Count).AdjustToContents();
  }

  private DateTime ToDateTime(TimeSpan implicixDateTime) => new(implicixDateTime.Ticks);
}
