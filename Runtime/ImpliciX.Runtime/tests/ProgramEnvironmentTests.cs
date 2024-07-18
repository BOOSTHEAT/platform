using System;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.SharedKernel.Modules;
using Microsoft.Extensions.Configuration;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Runtime.Tests;

public class ProgramEnvironmentTests
{
  [Test]
  public void CreateMinimalist()
  {
    var (env, _) = CreateFor("settings/minimalist.json", "single_setup");
    Check.That(env.Modules).IsEmpty();
  }
  
  [Test]
  public void CreateWithDumbModules()
  {
    var (env, actualRtDef) = CreateFor("settings/dumb_modules.json", "single_setup");
    Check.That(env.Modules.Select(x => x.Id)).IsEquivalentTo("TheFooFactory:foo","TheBarFactory:bar");
    Check.That(actualRtDef.Options.LocalStoragePath).IsEqualTo("/");
  }
  
  [Test]
  public void CreateWithLocalOptions()
  {
    var (env, actualRtDef) = CreateFor("settings/local_options.json","single_setup");
    Check.That(env.Modules.Select(x => x.Id)).IsEquivalentTo("TheFooFactory:foo","TheBarFactory:bar");
    Check.That(actualRtDef.Options.LocalStoragePath).IsEqualTo("/tmp");
  }
  
  [Test]
  public void CreateWithMultipleSetups()
  {
    var (env, actualRtDef) = CreateFor("settings/multiple_setups.json","poum");
    Check.That(env.Modules.Select(x => x.Id)).IsEquivalentTo("TheFooFactory:foo","TheBarFactory:bar");
    Check.That(actualRtDef.Setups).IsEquivalentTo("pim", "pam", "poum");
  }
  
  [Test]
  public void ErrorModuleWithoutFactory()
  {
    Check.ThatCode(() => CreateFor("settings/module_without_factory.json", "single_setup"))
      .ThrowsAny().WithMessage("No factory defined for module bar");
  }

  private (ProgramEnvironment sut, ApplicationRuntimeDefinition actualRtDef) CreateFor(string settingsFile, string setup)
  {
    Environment.SetEnvironmentVariable("IMPLICIX_LOCAL_STORAGE", "/");
    var builder = new ConfigurationBuilder();
    builder.AddJsonFile(settingsFile);
    var configuration = builder.Build();
    var model = new ApplicationDefinition
    {
      DataModelDefinition = new DataModelDefinition()
    };
    ApplicationRuntimeDefinition actualRtDef = null;
    var env = ProgramEnvironment.CreateInstance(configuration, model, setup,
      (factoryName, moduleName, rtDef) =>
      {
        actualRtDef = rtDef;
        return CreateModuleInstance($"{factoryName}:{moduleName}");
      });
    return (env, actualRtDef);
  }

  private IImpliciXModule CreateModuleInstance(string id)
  {
    var mock = new Mock<IImpliciXModule>();
    mock.Setup(x => x.Id).Returns(id);
    return mock.Object;
  }
}