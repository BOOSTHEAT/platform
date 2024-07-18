using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using ImpliciX.ToQml.Tests.Helpers;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.QmlTests;

[SetUpFixture]
public class Harness
{
  private DirectoryInfo _stdResourcesPath;
  private static IContainer _container;

  [OneTimeSetUp]
  public async Task Init()
  {
    _stdResourcesPath = new DirectoryInfo(Path.Combine(Path.GetTempPath(),Path.GetRandomFileName()));
    _stdResourcesPath.Create();
    var standardResources = ResourceManager.Load(_stdResourcesPath, ResourceManager.StandardResources,new NullCopyrightManager());
    _container = new ContainerBuilder()
      .WithName("qmltests")
      .WithImagePullPolicy(ilr => true)
      .WithImage("implicixpublic.azurecr.io/implicix-qt5:latest")
      .WithEnvironment("XDG_RUNTIME_DIR", "/tmp/runtime")
      .WithBindMount(Path.Combine(Environment.CurrentDirectory,"QmlTests","Root"),"/tests")
      .WithBindMount(_stdResourcesPath.FullName,"/stdlib")
      .WithEnvironment("QT_QPA_PLATFORM", "offscreen")
      .WithEntrypoint("yes")
      .Build();
    await _container.StartAsync();
  }

  [OneTimeTearDown]
  public void Clean()
  {
    _stdResourcesPath.Delete(true);
  }
  
  public static async Task RunTest(string filename)
  {
    var result = await _container.ExecAsync(new List<string>
      { "qmltestrunner", "-input", $"/tests/{filename}", "-import", "/stdlib", "-import", "/tests" });
    TestContext.Out.Write($"ExitCode: {result.ExitCode}\n");
    TestContext.Out.Write(result.Stdout);
    TestContext.Out.Write(result.Stderr);

    if (result.ExitCode != 0)
      Assert.Fail($"Tests {filename} Failed");
  }
}