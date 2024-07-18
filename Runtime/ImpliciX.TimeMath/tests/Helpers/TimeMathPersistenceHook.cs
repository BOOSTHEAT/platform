using System;
using System.IO;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.TimeMath.Access;
using ImpliciX.TimeMath.Tests.Helpers;
using TechTalk.SpecFlow;

namespace ImpliciX.TimeMath.Tests;

[Binding]
public sealed class TimeMathPersistenceHook
{
  private readonly FeatureContext _featureContext;
  private readonly ScenarioContext _scenarioContext;
  private static readonly string GlobalFolderPath = Path.Combine(
    Path.GetTempPath(),
    "TimeMathTests"
    );
  private string FolderPath => Path.Combine(
    GlobalFolderPath,
    WindowedOrNot,
    _featureContext.FeatureInfo.Title,
    _scenarioContext.ScenarioInfo.Title
    );

  private string WindowedOrNot => _featureContext.FeatureInfo.FolderPath.Split(Path.DirectorySeparatorChar)[1];

  public TimeMathPersistenceHook(
    FeatureContext featureContext,
    ScenarioContext scenarioContext
  )
  {
    _featureContext = featureContext;
    _scenarioContext = scenarioContext;
  }
  // For additional details on SpecFlow hooks see http://go.specflow.org/doc-hooks

  [BeforeScenario]
  public void BeforeScenario()
  {
    Console.WriteLine("Starting " + _featureContext.FeatureInfo.Title);
    Console.WriteLine("tags " + _featureContext.FeatureInfo.Tags);
    Console.WriteLine("path " + _featureContext.FeatureInfo.FolderPath);
    var path = _featureContext.FeatureInfo.FolderPath;

    if (path.Contains("TimeSeries"))
    {
      var db = new TimeSeriesDb(FolderPath, "test");
      _scenarioContext.Set(db);
      _scenarioContext["timeMathWriter"] = new TimeBasedTimeMathWriter(db);
      _scenarioContext["timeMathReader"] = new TimeBasedTimeMathReader(db);
    }
    else
    {
      var fakeTimeMathPersistence = new FakeTimeMathPersistence();
      _scenarioContext["timeMathWriter"] = fakeTimeMathPersistence;
      _scenarioContext["timeMathReader"] = fakeTimeMathPersistence;
    }
  }

  [AfterScenario]
  public void CleanupScenario()
  {
    if(_scenarioContext.TryGetValue<TimeSeriesDb>(out var db))
      db.Dispose();
  }
  
  [BeforeTestRun]
  public static void GlobalInit()
  {
    if(Directory.Exists(GlobalFolderPath))
      Directory.Delete(GlobalFolderPath, true);
    Directory.CreateDirectory(GlobalFolderPath);
  }
  
  [AfterTestRun]
  public static void GlobalCleanup()
  {
    Directory.Delete(GlobalFolderPath, true);
  }
}
