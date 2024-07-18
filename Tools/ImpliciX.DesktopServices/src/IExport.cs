namespace ImpliciX.DesktopServices;

public interface IExport
{
  IAction MetricsToExcel(string sourceFolder, string outputFilePath);
  IAction MetricsToSqlite(string sourceFolder, string outputFilePath);
  IAction RecordsToExcel(string sourceFolder, string outputFilePath);
}
