using System.Reflection;
using Docker.DotNet.Models;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services.Project;
using ImpliciX.Language;
using ImpliciX.Language.Control;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using Moq;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Projects;

public class ProjectManagerTests
{
  private readonly Mock<IApplicationDefinitionFactory> _appDefFactory;
  private readonly Mock<IConsoleService> _console;
  private readonly Mock<IDeviceDefinitionFactory> _deviceDefFactory;
  private readonly Mock<IDockerService> _docker;
  private readonly Mock<IFileSystemService> _fileService;
  private readonly Mock<IProjectOperation<LinkerInput, string>> _linkerBuilder;
  private readonly Mock<IProjectHelper> _projectHelper;
  private readonly SystemInfo _systemInfo;

  public ProjectManagerTests()
  {
    _systemInfo = new SystemInfo(
      OS.linux,
      Architecture.x64,
      "x64"
    );

    _docker = new Mock<IDockerService>();
    _console = new Mock<IConsoleService>();

    _fileService = new Mock<IFileSystemService>();
    SetupFileService();

    _projectHelper = new Mock<IProjectHelper>();
    _projectHelper.Setup(o => o.CreateTempDirectory()).Returns("DesignerAppBuilder.Test");
    _projectHelper.Setup(
      o => o.FindGitDirectory(
        It.IsAny<string>(),
        It.IsAny<int>()
      )
    ).Returns("myGitFolder");
    _projectHelper.Setup(
      o => o.CopyAndPrepareProjectDirectory(
        It.IsAny<string>(),
        It.IsAny<string>()
      )
    );

    _linkerBuilder = new Mock<IProjectOperation<LinkerInput, string>>();
    _linkerBuilder.Setup(o => o.Execute(It.IsAny<LinkerInput>()));

    _deviceDefFactory = new Mock<IDeviceDefinitionFactory>();
    _deviceDefFactory
      .Setup(o => o.Create(It.IsAny<string>(), It.IsAny<ApplicationDefinition>()))
      .Returns<string, ApplicationDefinition>((path, appDef) =>
      {
        var device = new Mock<IDeviceDefinition>();
        device.Setup(o => o.Path).Returns(path);
        device.Setup(o => o.Name).Returns(appDef.AppName);
        device.Setup(o => o.EntryPoint).Returns("EntryPoint");
        device.Setup(o => o.Version).Returns("1.2.3");
        device.Setup(o => o.UserInterface).Returns(DeviceDefinition.M<UserInterfaceModuleDefinition>(appDef));
        return device.Object;
      });

    _appDefFactory = new Mock<IApplicationDefinitionFactory>();
  }

  [TestCase(nameof(DllProjectManager))]
  [TestCase(nameof(NupkgProjectManager))]
  [TestCase(nameof(CsProjectManager))]
  public void GivenApplicationInstanceIsNotCreated_WhenICreatePackage_ThenIGetError(
    string sutIdName
  )
  {
    var sut = CreateSut(sutIdName);
    Check.ThatCode(async () => await sut.CreatePackage(_systemInfo))
      .Throws<InvalidOperationException>();
  }

  [TestCase(nameof(DllProjectManager))]
  [TestCase(nameof(NupkgProjectManager))]
  [TestCase(nameof(CsProjectManager))]
  public async Task DeviceDefinitionMatchesTheApplicationDefinition(
    string sutIdName
  )
  {
    var mainApp = new FakeApplicationDefinition(new object[] { });
    SetupApplicationDefinitionFactory(mainApp);
    SetupDocker(received => { });
    var sut = CreateSut(sutIdName);

    var dd = await sut.Make();

    Assert.That(dd.Path, Is.EqualTo($"mySourcePathFor{sutIdName}"));
    Assert.That(dd.Name, Is.EqualTo(mainApp.AppName));
  }

  [TestCase(nameof(DllProjectManager))]
  [TestCase(nameof(NupkgProjectManager))]
  [TestCase(nameof(CsProjectManager))]
  public async Task GivenProjectWithGui_WhenICreatePackage_ThenIBuildGui(
    string sutIdName
  )
  {
    var mainApp = new FakeApplicationDefinition(
      new object[]
      {
        new MetricsModuleDefinition(),
        new ControlModuleDefinition(),
        new UserInterfaceModuleDefinition()
      }
    );

    SetupApplicationDefinitionFactory(mainApp);

    CreateContainerParameters? containerParameters = null;
    SetupDocker(received => containerParameters = received);

    var sut = CreateSut(sutIdName);
    await sut.Make();
    await sut.CreatePackage(_systemInfo);

    Check.That(containerParameters).IsNotNull();
    Check.That(containerParameters!.Entrypoint).Not.IsEmpty();
    var entryPoints = string.Join(
      ", ",
      containerParameters!.Entrypoint
    );
    Check.That(entryPoints).Contains("device:app");
    Check.That(entryPoints).Contains("gui");
  }

  [TestCase(nameof(DllProjectManager))]
  [TestCase(nameof(NupkgProjectManager))]
  [TestCase(nameof(CsProjectManager))]
  public async Task GivenProjectWithoutGui_WhenICreatePackage_ThenIDoNotTryToBuildGui(
    string sutIdName
  )
  {
    var mainApp = new FakeApplicationDefinition(
      new object[]
      {
        new MetricsModuleDefinition(),
        new ControlModuleDefinition()
      }
    );

    SetupApplicationDefinitionFactory(mainApp);

    CreateContainerParameters? containerParameters = null;
    SetupDocker(received => containerParameters = received);

    var sut = CreateSut(sutIdName);
    await sut.Make();
    await sut.CreatePackage(_systemInfo);

    Check.That(containerParameters).IsNotNull();
    Check.That(containerParameters!.Entrypoint).Not.IsEmpty();
    var entryPoints = string.Join(
      ", ",
      containerParameters!.Entrypoint
    );
    Check.That(entryPoints).Contains("device:app");
    Check.That(entryPoints).Not.Contains("gui");
  }


  #region Helpers

  private IManageProject CreateSut(
    string manageProjectType
  )
  {
    return manageProjectType switch
    {
      nameof(CsProjectManager) => CreateCsProjectManager(),
      nameof(DllProjectManager) => CreateDllProjectManager(),
      nameof(NupkgProjectManager) => CreateNupkgProjectManager(),
      _ => throw new ArgumentOutOfRangeException(
        nameof(manageProjectType),
        manageProjectType,
        null
      )
    };
  }

  private IManageProject CreateCsProjectManager()
  {
    return new CsProjectManager(
      "mySourcePathForCsProjectManager",
      _ => { },
      _docker.Object,
      _console.Object,
      _fileService.Object,
      _projectHelper.Object,
      _linkerBuilder.Object,
      _deviceDefFactory.Object,
      _appDefFactory.Object,
      DllProjectManager_AddAssembliesFromSource
    );
  }

  private IManageProject CreateDllProjectManager()
  {
    return new DllProjectManager(
      "mySourcePathForDllProjectManager",
      _ => { },
      _docker.Object,
      _fileService.Object,
      _projectHelper.Object,
      _linkerBuilder.Object,
      _deviceDefFactory.Object,
      _appDefFactory.Object,
      DllProjectManager_AddAssembliesFromSource
    );
  }

  private IManageProject CreateNupkgProjectManager()
  {
    return new NupkgProjectManager(
      "mySourcePathForNupkgProjectManager",
      _ => { },
      _docker.Object,
      _console.Object,
      _fileService.Object,
      _projectHelper.Object,
      _linkerBuilder.Object,
      _deviceDefFactory.Object,
      NupkgProjectManager_CreateAppDef(_appDefFactory.Object)
    );
  }

  private static void DllProjectManager_AddAssembliesFromSource(
    string sourcePath,
    IDictionary<string, Assembly> assemblies
  )
  {
    assemblies.Add(
      "key",
      new FakeAssembly(
        "myFakeAssembly",
        new[] {typeof(FakeAssembly)}
      )
    );
  }

  private Func<string, (string, ApplicationDefinition)> NupkgProjectManager_CreateAppDef(
    IApplicationDefinitionFactory appFactory
  )
  {
    return sourcePath =>
      ("nupkgId", appFactory.CreateEntryPointFrom(
        null,
        sourcePath
      ));
  }

  private class FakeAssembly : Assembly
  {
    private readonly string _name;
    private readonly Type[] _types;

    public FakeAssembly(
      string name,
      Type[] types
    )
    {
      _name = name;
      _types = types;
    }

    public override Type[] GetTypes()
    {
      return _types;
    }

    public override AssemblyName GetName()
    {
      return new AssemblyName(_name);
    }

    public override AssemblyName[] GetReferencedAssemblies()
    {
      return new[] {GetName()};
    }
  }

  private class FakeApplicationDefinition : ApplicationDefinition
  {
    public FakeApplicationDefinition(
      object[] moduleDefinitions
    )
    {
      AppName = "DeviceName";
      ModuleDefinitions = moduleDefinitions;
    }
  }

  private void SetupFileService()
  {
    _fileService.Setup(o => o.FileExists(It.IsAny<string?>()))
      .Returns<string?>(
        path => path is not null &&
                (path.Contains("build_config.txt") || path.Contains("main.qrc") || path.Contains("app.zip"))
      );

    _fileService.Setup(o => o.CreateDirectory(It.IsAny<string>())).Returns<string>(
      path =>
      {
        var info = new Mock<IDirectoryInfoWrapper>();
        info.Setup(o => o.FullName).Returns("FullName");
        info.Setup(o => o.EnumerateFiles()).Returns(new[] {new FileInfo(path)});
        return info.Object;
      }
    );

    _fileService.Setup(
      o => o.DirectoryGetFiles(
        It.IsAny<string>(),
        It.IsAny<string>()
      )
    ).Returns(new[] {"myFile1", "myFile2"});
  }

  private void SetupDocker(
    Action<CreateContainerParameters> onContainerParametersReceived
  )
  {
    _docker.Setup(o => o.Batch(It.IsAny<CreateContainerParameters>()))
      .Returns<CreateContainerParameters>(
        parameters =>
        {
          onContainerParametersReceived(parameters);
          return Task.CompletedTask;
        }
      );

    _docker.Setup(
      o => o.Execute(
        It.IsAny<string>(),
        It.IsAny<string[]>()
      )
    ).Returns(Task.CompletedTask);
  }

  private void SetupApplicationDefinitionFactory(
    ApplicationDefinition appDef
  )
  {
    _appDefFactory.Setup(
      o => o.CreateEntryPointFrom(
        It.IsAny<Type[]>(),
        It.IsAny<string>()
      )
    ).Returns(appDef);
  }

  #endregion
}
