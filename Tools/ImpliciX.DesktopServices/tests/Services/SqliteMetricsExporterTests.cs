using System.Data.SQLite;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.DesktopServices.Services;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services;

public class SqliteMetricsExporterTests
{
  private static readonly string
    StorageFolderPath = Path.Combine(Path.GetTempPath(), nameof(SqliteMetricsExporterTests));

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
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.sqlite");
    new SqliteMetricsExporter(workingDirPath, outputFileFullPath, TestContext.Out.WriteLine).Execute();

    // Then
    using var sqlite = OpenConnection(outputFileFullPath);
    CheckExpectedDataInTableFor_ColdFile_1(sqlite);
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
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.sqlite");
    new SqliteMetricsExporter(workingDirPath, outputFileFullPath, TestContext.Out.WriteLine).Execute();

    // Then
    using var sqlite = OpenConnection(outputFileFullPath);
    CheckExpectedDataInTableFor_ColdFile_1(sqlite);
    CheckExpectedDataInTableFor_ColdFile_2(sqlite);
    CheckExpectedDataInTableFor_ColdFile_3(sqlite);
  }


  [Test]
  public void GivenMultipleMetricsFiles_For_TheSame_Metric()
  {
    // Given
    var workingDirPath = CreateTempDir();
    CreateColdFile_1(workingDirPath);
    CreateColdFile_1_bis(workingDirPath);

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.sqlite");
    new SqliteMetricsExporter(workingDirPath, outputFileFullPath, TestContext.Out.WriteLine).Execute();

    // Then
    using var sqlite = OpenConnection(outputFileFullPath);
    CheckTableContent(sqlite, SomeLongUrn,
      new[] {"at", "begin", "end", "fizz", "buzz", "fizzbuzz"},
      (new[] {_dt1, _dt0, _dt1}, new[] {1.0, 2.0}),
      (new[] {_dt2, _dt1, _dt2}, new[] {3.0, 4.0, 5.0}),
      (new[] {_dt4, _dt3, _dt4}, new[] {1.0, 2.0})
    );
  }


  [Test]
  public void ExportManyFiles()
  {
    var inputDirPath = Path.Combine("Resources", "metricsCold");
    var workingDirPath = CreateTempDir();
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.sqlite");
    Assert.DoesNotThrow(
      () => new SqliteMetricsExporter(inputDirPath, outputFileFullPath, TestContext.Out.WriteLine).Execute()
    );
    using var sqlite = OpenConnection(outputFileFullPath);
    using var cmd = sqlite.CreateCommand();
    cmd.CommandText =
      $"SELECT name FROM sqlite_schema WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY 1";
    using var actual = cmd.ExecuteReader();
    var allTables = GetAllValues(actual)
      .SelectMany(row => row.Select(x => x.Value.ToString()));
    Check.That(allTables).IsEqualTo(new[]
    {
      "monitoring:dashboard:drying_efficiency",
      "monitoring:dashboard:pellets",
      "monitoring:dashboard:production_state",
      "monitoring:value:kpi_crusher_failed_count",
      "monitoring:value:kpi_crusher_failed_duration",
      "monitoring:value:kpi_crusher_run_duration",
      "monitoring:value:kpi_dryer_failed_count",
      "monitoring:value:kpi_dryer_failed_duration",
      "monitoring:value:kpi_dryer_run_duration",
    });
  }


  [Test]
  [Ignore("Slow test")]
  public void CanExportManyPoints()
  {
    const int numberOfPoints = 1100000;
    // Given
    var workingDirPath = CreateTempDir();
    {
      using var coldMetricsFile =
        ColdMetricsDb.NewCollection(Path.Combine(workingDirPath, $"verylong{ColdMetricsDb.FileExtension}"), "just:me");
      for (int i = 1; i <= numberOfPoints; i++)
      {
        coldMetricsFile.WriteDataPoint(new MetricsDataPoint(TimeSpan.FromMilliseconds(i), new[]
        {
          new DataPointValue("just:me", i),
        }, TimeSpan.FromMilliseconds(i - 1), TimeSpan.FromMilliseconds(i)));
      }
    }

    // When
    var outputFileFullPath = Path.Combine(workingDirPath, "metrics.sqlite");
    new SqliteMetricsExporter(
      workingDirPath,
      outputFileFullPath,
      TestContext.Out.WriteLine
    ).Execute();

    // Then
    using var sqlite = OpenConnection(outputFileFullPath);
    using var actual = SelectAllFromTable(sqlite, "just:me");
    var allValues = GetAllValues(actual);
    Assert.That(allValues.Count(), Is.EqualTo(numberOfPoints));
  }

  [Test]
  public void NoExceptionButErrorResultOnMajorIssue()
  {
    var workingDirPath = CreateTempDir();
    var exporter = new SqliteMetricsExporter(workingDirPath, "///", TestContext.Out.WriteLine);
    var result = exporter.Execute();
    Assert.True(result.IsError);
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

  private void CheckExpectedDataInTableFor_ColdFile_1(SQLiteConnection sqlite)
  {
    CheckTableContent(sqlite, SomeLongUrn,
      new[] {"at", "begin", "end", "fizz", "buzz", "fizzbuzz"},
      (new[] {_dt1, _dt0, _dt1}, new[] {1.0, 2.0}),
      (new[] {_dt2, _dt1, _dt2}, new[] {3.0, 4.0, 5.0})
    );
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

  private void CheckExpectedDataInTableFor_ColdFile_2(SQLiteConnection sqlite)
  {
    CheckTableContent(sqlite, "just:me",
      new[] {"at", "begin", "end", "just:me", "and:you"},
      (new[] {_dt3, _dt1, _dt2}, new[] {66.0, 67.0})
    );
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

  private void CheckExpectedDataInTableFor_ColdFile_3(SQLiteConnection sqlite)
  {
    CheckTableContent(sqlite, "power:sum",
      new[] {"at", "begin", "end", "fizz", "buzz", "fizzbuzz"},
      (new[] {_dt3, _dt1, _dt2}, new[] {33.0, 34.0, 35.0})
    );
  }

  private static SQLiteConnection OpenConnection(string sqliteFilePath)
  {
    var connection = new SQLiteConnection();
    connection.ConnectionString = $"Data Source=\"{sqliteFilePath}\"";
    connection.Open();
    return connection;
  }

  private static void CheckTableContent(
    SQLiteConnection sqlite, string tableName,
    string[] expectedColumnNames,
    params (DateTime[] Dates, double[] Values)[] expectedRows)
  {
    using var actual = SelectAllFromTable(sqlite, tableName);

    Check.That(actual.FieldCount).IsEqualTo(expectedColumnNames.Length);
    Check.That(Enumerable.Range(0, actual.FieldCount).Select(actual.GetName)).IsEqualTo(expectedColumnNames);

    var allValues = GetAllValues(actual);
    Check.That(allValues.Select(row => row
        .Select(x => x.Value)
        .TakeWhile(v => v is not DBNull)
      ))
      .IsEqualTo(expectedRows.Select(expectedRow => expectedRow
        .Dates.Select(dt => (object)dt.Ticks)
        .Concat(expectedRow.Values.Cast<object>())
      ));
  }

  private static SQLiteDataReader SelectAllFromTable(SQLiteConnection sqlite, string tableName)
  {
    using var cmd = sqlite.CreateCommand();
    cmd.CommandText = $"SELECT * FROM `{tableName}`";
    return cmd.ExecuteReader();
  }

  private static IEnumerable<(string Name, object Value)[]> GetAllValues(SQLiteDataReader reader)
  {
    while (reader.Read())
      yield return Enumerable
        .Range(0, reader.FieldCount)
        .Select(i => (reader.GetName(i), reader.GetValue(i)))
        .ToArray();
  }

  private static string CreateTempDir()
  {
    var tmpDirPath = Path.Combine(StorageFolderPath, Path.GetFileNameWithoutExtension(Path.GetTempFileName()));
    Directory.CreateDirectory(tmpDirPath);
    return tmpDirPath;
  }

  #endregion
}
