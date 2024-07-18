using System;
using Microsoft.Extensions.Configuration;
using TechTalk.SpecFlow;

namespace Configuration.Specs.Steps;

[Binding]
public sealed class DefinitionSteps
{
  private readonly ScenarioContext _scenarioContext;

  public DefinitionSteps(ScenarioContext scenarioContext)
  {
    _scenarioContext = scenarioContext;
  }

  [Given(@"I have a simple ""(.*)"" profil")]
  public void GivenIHaveASimpleProfil(string profil)
  {
    Environment.SetEnvironmentVariable("IMPLICIX_LOCAL_STORAGE", "/");
    var builder = new ConfigurationBuilder();
    builder.AddJsonFile("settings/simpleApplicationSetting.json", false, false);
    var configuration = builder.Build();
    _scenarioContext.Add("configuration", configuration);

    //var model = new ApplicationDefinition();
    //var setup=configuration["Setups"];
    //var setup=configuration.Get["Setups"];
    //Setups:dev:Log
    /*
    {
        DataModelDefinition = new DataModelDefinition()
    };
    */
    _scenarioContext.Pending();
  }

  [Given(@"I add ""(.*)"" to the ""(.*)"" profil")]
  public void GivenIAddToTheProfil(string timeMath, string profil)
  {
    _scenarioContext.Pending();
  }

  [When(@"I start the ""(.*)"" application in ""(.*)"" mode")]
  public void WhenIStartTheApplicationInMode(string application, string mode)
  {
    _scenarioContext.Pending();
  }

  [Then(@"the application does not start")]
  public void ThenTheApplicationDoesNotStart()
  {
    _scenarioContext.Pending();
  }

  [When(@"the service is initialize")]
  public void WhenTheServiceIsInitialize()
  {
  }
}