using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.Language;
using ImpliciX.Language.GUI;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services.Project;

internal class NupkgProjectManager : IManageProject
{
  private readonly IConsoleService _console;
  [NotNull] private readonly IDeviceDefinitionFactory _deviceDefFactory;
  private readonly IDockerService _docker;
  [NotNull] private readonly IFileSystemService _fileService;
  [NotNull] private readonly IProjectOperation<LinkerInput, string> _linkerBuilder;
  [NotNull] private readonly Func<string, (string, ApplicationDefinition)> _nupkgLoader;
  private readonly Action<IDeviceDefinition> _onMake;
  [NotNull] private readonly IProjectHelper _projectHelper;
  [CanBeNull] private ApplicationDefinition _appMain;
  private string _nupkgId;

  public NupkgProjectManager(
    [NotNull] string sourcePath,
    [NotNull] Action<IDeviceDefinition> onMake,
    [NotNull] IDockerService docker,
    [NotNull] IConsoleService console,
    [NotNull] IFileSystemService fileService,
    [NotNull] IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder,
    [NotNull] IDeviceDefinitionFactory deviceDefFactory,
    [CanBeNull] Func<string, (string, ApplicationDefinition)> nupkgLoader = null
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _console = console ?? throw new ArgumentNullException(nameof(console));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _projectHelper = projectHelper ?? throw new ArgumentNullException(nameof(projectHelper));
    _deviceDefFactory = deviceDefFactory ?? throw new ArgumentNullException(nameof(deviceDefFactory));
    Path = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
    _onMake = onMake ?? throw new ArgumentNullException(nameof(onMake));
    _linkerBuilder = linkerBuilder;
    _nupkgLoader = nupkgLoader ?? NupkgLoader.CreateApplication;
  }

  public string Path { get ; }

  public Task<IDeviceDefinition> Make()
  {
    (_nupkgId, _appMain) = _nupkgLoader(Path);
    var device = _deviceDefFactory.Create(Path, _appMain);
    _onMake(device);
    return Task.FromResult(device);
  }

  public bool CanMakeMultipleTimes => false;

  public async Task<FileInfo> CreatePackage(
    SystemInfo systemInfo
  )
  {
    if (_appMain is null)
      throw new InvalidOperationException(
        $"Application instance is null : {nameof(Make)} must be call before {nameof(CreatePackage)}"
      );

    var buildGui = _appMain.ModuleDefinitions.Any(m => m is UserInterfaceModuleDefinition);
    var builder = new BuildImpliciXPackage(
      _docker,
      _fileService,
      _projectHelper,
      _linkerBuilder,
      systemInfo,
      buildGui
    );
    var dd = _deviceDefFactory.Create(Path, _appMain);
    var localRepo = await CreateLocalRepoForNupkg(
      builder.TmpDirectory,
      builder.ContainerTmpDir
    );
    var packageFilename = await builder.Execute(
      new LinkerInput(
        dd.Name,
        dd.EntryPoint,
        dd.Version,
        new[] { "-s", localRepo.ToLinuxPath(), "-n", _nupkgId },
        new[] { $"{System.IO.Path.GetDirectoryName(Path)}:/app" }
      )
    );

    return new FileInfo(packageFilename);
  }

  public async Task RunGui()
  {
    var dd = _deviceDefFactory.Create(Path, _appMain);
    var runner = new RunGui(
      _docker,
      _projectHelper,
      _linkerBuilder
    );
    var localRepo = await CreateLocalRepoForNupkg(
      runner.TmpDirectory,
      runner.ContainerTmpDir
    );
    await runner.Execute(
      new LinkerInput(
        dd.Name,
        dd.EntryPoint,
        dd.Version,
        new[] { "-s", localRepo.ToLinuxPath(), "-n", _nupkgId },
        Array.Empty<string>()
      )
    );
  }

  private async Task<string> CreateLocalRepoForNupkg(
    string basePath,
    string containerBasePath
  )
  {
    _console.WriteLine("Creating local nupkg repository");
    const string repoName = "repo";
    var repoFolder = _fileService.CreateDirectory(
      System.IO.Path.Combine(
        basePath,
        repoName
      )
    );
    await _docker.Pull(ProjectHelper.DotnetSdkImageName);
    const string containerName = "implicix_create_repo";

    await _docker.Batch(
      new CreateContainerParameters
      {
        Name = containerName,
        Image = ProjectHelper.DotnetSdkImageName,
        HostConfig = new HostConfig
        {
          Binds = new[]
          {
            $"{repoFolder.FullName}:/repo",
            $"{Path}:/app.nupkg"
          }
        },
        Entrypoint = EntryPointBuilderFactory.Create()
          .SetCommand("/usr/bin/dotnet nuget push /app.nupkg -s /repo")
          .Build()
      }
    );

    await _docker.Wait(containerName);

    if (!repoFolder.EnumerateFiles().Any())
      throw new ApplicationException("Failed to create local repository for nupkg");

    return $"{containerBasePath}/{repoName}";
  }
}
