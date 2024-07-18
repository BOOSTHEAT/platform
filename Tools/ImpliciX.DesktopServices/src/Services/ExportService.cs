using System;

namespace ImpliciX.DesktopServices.Services;

internal class ExportService : IExport
{
  private readonly Action<string> _logger;

  public ExportService(Action<string> logger) => _logger = logger;

  public IAction MetricsToExcel(string sourceFolder, string outputFilePath) =>
    new ExcelMetricsExporter(sourceFolder, outputFilePath, _logger);

  public IAction MetricsToSqlite(string sourceFolder, string outputFilePath) =>
    new SqliteMetricsExporter(sourceFolder, outputFilePath, _logger);

  public IAction RecordsToExcel(string sourceFolder, string outputFilePath) =>
    new ExcelRecordsExporter(sourceFolder, outputFilePath, _logger);
}
