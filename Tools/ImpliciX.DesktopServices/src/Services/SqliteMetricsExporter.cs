using System;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Core;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services;

internal class SqliteMetricsExporter : IAction
{
  private const int InsertionChunkSize = 100000;
  [NotNull] private readonly ColdReader<MetricsDataPoint> _coldReader;
  private readonly Action<string> _log;
  [NotNull] internal readonly string OutputFilePath;
  [NotNull] internal readonly string WorkingDirPath;

  public SqliteMetricsExporter([NotNull] string workingDirPath, [NotNull] string outputFilePath,
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
      using var connection = new SQLiteConnection();
      connection.ConnectionString = $"Data Source=\"{OutputFilePath}\"";
      connection.Open();
      foreach (var (baseUrn, index) in _coldReader.Urns.Select((urn, index) => (urn, index)))
      {
        _log($"Exporting series {index + 1}/{_coldReader.Urns.Length} ({baseUrn})");
        ExportSingleMetric(connection, baseUrn);
      }

      return Result<Unit>.Create(new Unit());
    }
    catch (Exception e)
    {
      return Result<Unit>.Create(new Error("SQLite export failed", e.Message));
    }
  }

  private void ExportSingleMetric(SQLiteConnection connection, string baseUrn)
  {
    var columns = _coldReader.GetProperties(baseUrn);
    var tableName = baseUrn;

    Execute(connection, TableCreation(tableName, columns));

    var points = _coldReader.ReadDataPoints(baseUrn).ToArray();
    _log($"Found {points.Length} points in {baseUrn}");

    var chunks =
      from point in points
      let columnNames = ColumnsForValueInsertion(point, tableName)
      group point by columnNames
      into groupBySameColumns
      from chunk in groupBySameColumns.Chunk(InsertionChunkSize)
      select new {ColumnNames = groupBySameColumns.Key, Points = chunk};

    var written = 0;
    foreach (var chunk in chunks)
    {
      var cmdBuilder = new StringBuilder();
      cmdBuilder.Append($"INSERT INTO `{tableName}` (at, begin, end, {chunk.ColumnNames}) VALUES ");
      Execute(connection, BuildInsertCommand(chunk.Points, cmdBuilder));
      written += chunk.Points.Length;
      _log($"Progress {baseUrn} ({written}/{points.Length}) {100.0 * written / points.Length:F1}%");
    }
  }

  private static string BuildInsertCommand(MetricsDataPoint[] chunk, StringBuilder cmdBuilder) =>
    chunk
      .Select((point, index) => (point, index))
      .Aggregate(cmdBuilder, (builder, x) =>
      {
        var valueInsertionCommand = $"({AllValues(x.point)}){(x.index == chunk.Length - 1 ? "" : ",")}";
        cmdBuilder.Append(valueInsertionCommand);
        return builder;
      }).ToString();

  private static string AllValues(MetricsDataPoint point)
  {
    var values = point.Values.Select(v => v.Value);
    var formattedValues = string.Join(',', values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
    var allValues = $"{point.At.Ticks}, {point.SampleStartTime.Ticks}, {point.SampleEndTime.Ticks}, {formattedValues}";
    return allValues;
  }

  private static string ColumnsForValueInsertion(MetricsDataPoint point, string tableName)
  {
    var columnNames = point.Values.Select(v => v.Urn.Value);
    var sqlCompatibleColumnNames = columnNames.Select(c => $"`{ColumnName(tableName, c)}`").ToArray();
    var columnsForValueInsertion = string.Join(',', sqlCompatibleColumnNames);
    return columnsForValueInsertion;
  }

  private static void Execute(SQLiteConnection connection, string commandText)
  {
    using var cmd = connection.CreateCommand();
    cmd.CommandText = commandText;
    cmd.ExecuteNonQuery();
  }

  private static string TableCreation(string name, string[] columns)
  {
    var sqlCompatibleColumnNames = columns.Select(c => ColumnName(name, c)).ToArray();
    var columnsWithType = sqlCompatibleColumnNames.Select(f => $"`{f}` REAL");
    var tableCreationCommand =
      @$"
CREATE TABLE `{name}` (at INTEGER PRIMARY KEY, begin INTEGER, end INTEGER, {string.Join(',', columnsWithType)});
CREATE VIEW `{name}:Delays` AS SELECT at, (at-LAG(at) OVER (ORDER BY at))/10000 as ElapsedMs FROM `{name}`;
CREATE VIEW `{name}:Density` as select ElapsedMs, count(*) as Counted FROM `{name}:Delays` GROUP BY ElapsedMs;
CREATE VIEW `{name}:Distribution` as select ElapsedMs, sumed*1.0/maximum as Percentage  from (select ElapsedMs, Counted, (sum(Counted) OVER (order by ElapsedMs)) as sumed FROM `{name}:Density` as T) left join (select sum(Counted) as maximum FROM `{name}:Density`);
";
    return tableCreationCommand;
  }

  private static string ColumnName(string tableName, string fullName) =>
    fullName.Replace(tableName + ":", "");
}
