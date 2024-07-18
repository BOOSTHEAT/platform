using System;
using System.Data;
using System.Linq;
using ClosedXML.Excel;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Core;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services;

internal sealed class ExcelMetricsExporter : IAction
{
  private const int MaxOutputLength = 1048575;
  [NotNull] private readonly ColdReader<MetricsDataPoint> _coldReader;
  private readonly Action<string> _log;
  [NotNull] internal readonly string OutputFilePath;
  [NotNull] internal readonly string WorkingDirPath;

  public ExcelMetricsExporter([NotNull] string workingDirPath, [NotNull] string outputFilePath,
    Action<string> log = null)
  {
    WorkingDirPath = workingDirPath ?? throw new ArgumentNullException(nameof(workingDirPath));
    OutputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
    _coldReader = ColdMetricsDb.CreateReader(workingDirPath);
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
        _log("--- Inserting times in sheet");
        InsertTimesInSheet(sheet, baseUrn);
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

  private void InsertTimesInSheet(IXLWorksheet sheet, string baseUrn)
  {
    var timeTable = new DataTable();
    timeTable.Columns.Add("At", typeof(DateTime));
    timeTable.Columns.Add("Start", typeof(DateTime));
    timeTable.Columns.Add("End", typeof(DateTime));
    var dataPoints = _coldReader.ReadDataPoints(baseUrn).ToArray();
    if (dataPoints.Length > MaxOutputLength)
      _log($"--- Incoming data has {dataPoints.Length} lines. Output is truncated to {MaxOutputLength} lines");
    foreach (var dp in dataPoints.Take(MaxOutputLength))
    {
      var timeRow = timeTable.Rows.Add();
      timeRow.SetField(0, ToDateTime(dp.At));
      timeRow.SetField(1, ToDateTime(dp.SampleStartTime));
      timeRow.SetField(2, ToDateTime(dp.SampleEndTime));
    }

    sheet.Columns(1, 3).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm:ss";
    sheet.Cell(1, 1).InsertTable(timeTable.AsEnumerable());
    sheet.Columns(1, 3).Width = 20.4;
  }

  private void InsertValuesInSheet(IXLWorksheet sheet, string baseUrn)
  {
    string GetFieldName(string propertyName) => propertyName.Length == baseUrn.Length
      ? propertyName
      : propertyName.Substring(baseUrn.Length + 1);

    var valueTable = new DataTable();
    var properties = _coldReader.GetProperties(baseUrn);
    var shiftForBlankRoot = properties.Contains(baseUrn) ? 0 : 1;
    if (shiftForBlankRoot == 1)
      valueTable.Columns.Add(baseUrn);
    valueTable.Columns.AddRange(properties.Select(p => new DataColumn(GetFieldName(p), typeof(float))).ToArray());
    var propertyIndex = properties.Select((property, index) => (property, index))
      .ToDictionary(x => x.property, x => x.index + shiftForBlankRoot);
    var dataPoints = _coldReader.ReadDataPoints(baseUrn).Take(MaxOutputLength);
    foreach (var dp in dataPoints)
    {
      var valueRow = valueTable.Rows.Add();
      foreach (var dpv in dp.Values)
        valueRow.SetField(propertyIndex[dpv.Urn], dpv.Value);
    }

    sheet.Cell(1, 4).InsertTable(valueTable.AsEnumerable());
    sheet.Columns(4, 3 + valueTable.Columns.Count).AdjustToContents();
  }

  private DateTime ToDateTime(TimeSpan implicixDateTime) => new(implicixDateTime.Ticks);
}
