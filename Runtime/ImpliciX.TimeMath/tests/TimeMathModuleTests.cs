using System;
using System.Collections.Generic;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.SharedKernel.IO;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Tools;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests;

public class TimeMathModuleTests
{
  private const string ModuleName = "TimeMath";

  private IProvideDependency InitProvideDependency(
    TimeMathSettings timeMathSettings,
    IFileSystemService fileSystemService,
    IReadTimeSeries readTimeSeries,
    IWriteTimeSeries writeTimeSeries
  )
  {
    throw new InvalidOperationException("Storage setting section must be set for module TimeMath");

    var provider = new Mock<IProvideDependency>();
    return provider.Object;
  }

  [Test]
  public void GivenTimeMathSettingsStorageIsNotDefine_WhenICreate_ThenIGetAnException()
  {
    var options = new ApplicationOptions(
      new Dictionary<string, string>
      {
        {"LOCAL_STORAGE", Environment.GetEnvironmentVariable("HOME") + "/ImpliciX/storage/timeMath"}
      },
      new EnvironmentService()
    );

    var readTimeSeries = Mock.Of<IReadTimeSeries>();
    var writeTimeSeries = Mock.Of<IWriteTimeSeries>();

    var metrics = Array.Empty<Metric<MetricUrn>>();
    var timeMathModule = new TimeMathModule(ModuleName, CreateMetricInfos.Execute(metrics, Array.Empty<ISubSystemDefinition>()), options);

    var ex = Check.ThatCode(() =>
        timeMathModule.InitializeResources(
          InitProvideDependency(new TimeMathSettings(), Mock.Of<IFileSystemService>(), readTimeSeries, writeTimeSeries))
      )
      .Throws<InvalidOperationException>().Value;

    Check.That(ex.Message).Contains("Storage setting section must be set for module TimeMath");
  }

  [Test]
  public void GivenTimeMathSettingsStorageIsDefine_WhenICreate_ThenIGetATimeMathModule()
  {
    // given
    var timeMathModuleFactory = Mock.Of<IModuleFactory>();
    var factories = Mock.Of<ModuleFactories>(
      moduleFactories => moduleFactories[ModuleName]
                         ==
                         timeMathModuleFactory
    );

    IReadOnlyDictionary<string, string> optionsDictionary = new Dictionary<string, string>();
    var environmentService = Mock.Of<IEnvironmentService>();
    var environmentServiceMock = Mock.Get(environmentService);
    //IMPLICIX_ENVIRONMENT=dev
    environmentServiceMock.Setup(
      service =>
        service.GetEnvironmentVariable("IMPLICIX_ENVIRONMENT")
    ).Returns(
      "dev"
    );

    //IMPLICIX_LOCAL_STORAGE=/tmp/refApp
    environmentServiceMock.Setup(
      service =>
        service.GetEnvironmentVariable("IMPLICIX_LOCAL_STORAGE")
    ).Returns(
      "/tmp/UnitTest" + nameof(TimeMathModuleTests)
    );

    var timeMathModuleDefinition = new TimeMathModuleDefinition();
    timeMathModuleDefinition.Metrics = new IMetricDefinition[]
    {
    };

    var applicationDefinition = new ApplicationDefinition();
    applicationDefinition.ModuleDefinitions = new[]
    {
      timeMathModuleDefinition
    };

    var applicationRuntimeDefinition = new ApplicationRuntimeDefinition(
      applicationDefinition,
      new ApplicationOptions(
        optionsDictionary,
        environmentService
      ),
      new string[] { }
    );

    Mock.Get(timeMathModuleFactory).Setup(
        factory => factory.Create(ModuleName, applicationRuntimeDefinition)
      )
      .Returns(
        (string name, ApplicationRuntimeDefinition runtimeDefinition) =>
          TimeMathModule.Create(name, runtimeDefinition)
      );

    // when
    var moduleFactory = factories[ModuleName];
    var timeMathModule = moduleFactory.Create(ModuleName, applicationRuntimeDefinition);
    // then
    Check.That(moduleFactory).Not.IsNull();
    Check.That(timeMathModule).Not.IsNull();
    Check.That(timeMathModule.Id).Not.IsNull();
  }
}

public interface IModuleFactory
{
  public IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef);
}

public class ModuleFactories
{
  public virtual IModuleFactory this[string key] => throw new NotImplementedException();
}