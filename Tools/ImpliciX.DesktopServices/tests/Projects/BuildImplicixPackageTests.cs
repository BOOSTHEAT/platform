using Docker.DotNet.Models;
using ImpliciX.DesktopServices.Services.Project;
using Moq;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Projects;

internal class BuildImpliciXPackageTests
{
  private readonly Mock<IDockerService> _docker;
  private readonly Mock<IFileSystemService> _fileService;
  private readonly Mock<IProjectHelper> _projectHelper;

  public BuildImpliciXPackageTests()
  {
    _docker = new Mock<IDockerService>();

    _fileService = new Mock<IFileSystemService>();
    _fileService.Setup(o => o.FileExists(It.IsAny<string?>()))
      .Returns<string?>(
        path => path is not null &&
                (path.Contains("build_config.txt") || path.Contains("main.qrc") || path.Contains("app.zip"))
      );

    _projectHelper = new Mock<IProjectHelper>();
    _projectHelper.Setup(o => o.CreateTempDirectory()).Returns("DesignerAppBuilder.Test");
  }

  [Test]
  public async Task GivenIDoNotWantToBuildGui_WhenIExecute_ThenLinkerOptionDoNotContainsDeviceGui()
  {
    CreateContainerParameters? containerParameters = null;
    SetupDocker(received => containerParameters = received);

    var sut = CreateSut(false);
    await sut.Execute(
      new LinkerInput(
        "AppName",
        "AppEntryPoint",
        "1.2.3",
        Array.Empty<string>(),
        Array.Empty<string>()
      )
    );

    Check.That(containerParameters).IsNotNull();
    Check.That(containerParameters!.Entrypoint).Not.IsEmpty();
    var entryPoints = string.Join(
      ", ",
      containerParameters!.Entrypoint
    );
    Check.That(entryPoints).Not.Contains("gui");
  }

  [Test]
  public async Task GivenIWantToBuildGui_WhenIExecute_ThenLinkerOptionContainsDeviceGui()
  {
    CreateContainerParameters? containerParameters = null;
    SetupDocker(received => containerParameters = received);

    var sut = CreateSut();
    await sut.Execute(
      new LinkerInput(
        "AppName",
        "AppEntryPoint",
        "1.2.3",
        Array.Empty<string>(),
        Array.Empty<string>()
      )
    );

    Check.That(containerParameters).IsNotNull();
    Check.That(containerParameters!.Entrypoint).Not.IsEmpty();
    var entryPoints = string.Join(
      ", ",
      containerParameters!.Entrypoint
    );
    Check.That(entryPoints).Contains("gui");
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

  private BuildImpliciXPackage CreateSut(
    bool enableGuiOp = true
  )
  {
    var linkerBuilder = new Mock<IProjectOperation<LinkerInput, string>>();
    linkerBuilder.Setup(c => c.Execute(It.IsAny<LinkerInput>()));

    return new BuildImpliciXPackage(
      _docker.Object,
      _fileService.Object,
      _projectHelper.Object,
      linkerBuilder.Object,
      new SystemInfo(
        OS.linux,
        Architecture.x64,
        "x64"
      ), enableGuiOp
    );
  }
}
