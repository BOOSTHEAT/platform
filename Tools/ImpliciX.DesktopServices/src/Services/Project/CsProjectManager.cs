using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Helpers;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services.Project;

internal class CsProjectManager : IManageProject
{
  private readonly Action<IDeviceDefinition> _onMake;

  public CsProjectManager([NotNull] string sourcePath, [NotNull] Action<IDeviceDefinition> onMake,
    [NotNull] IDockerService docker, [NotNull] IConsoleService console, [NotNull] IFileSystemService fileService, [NotNull] IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder,
    [NotNull] IDeviceDefinitionFactory deviceDefFactory, [NotNull] IApplicationDefinitionFactory appDefFactory,
    Action<string, IDictionary<string, Assembly>> dllProjectManager_AddAssembliesFromSource = null)
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _console = console ?? throw new ArgumentNullException(nameof(console));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _deviceDefFactory = deviceDefFactory ?? throw new ArgumentNullException(nameof(deviceDefFactory));
    _appDefFactory = appDefFactory ?? throw new ArgumentNullException(nameof(appDefFactory));
    _dllProjectManagerAddAssembliesFromSource = dllProjectManager_AddAssembliesFromSource;
    _onMake = onMake ?? throw new ArgumentNullException(nameof(onMake));
    Path = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
    _projectHelper = projectHelper ?? throw new ArgumentNullException(nameof(projectHelper));
    _linkerBuilder = linkerBuilder;

    _device = null;
  }

  public string Path { get; }


  [CanBeNull] private IDeviceDefinition _device;
  [NotNull] private readonly IDockerService _docker;
  [NotNull] private readonly IConsoleService _console;
  [NotNull] private readonly IFileSystemService _fileService;
  [NotNull] private readonly IDeviceDefinitionFactory _deviceDefFactory;
  [NotNull] private readonly IApplicationDefinitionFactory _appDefFactory;
  private readonly Action<string, IDictionary<string, Assembly>> _dllProjectManagerAddAssembliesFromSource;
  [NotNull] private readonly IProjectHelper _projectHelper;
  private readonly IProjectOperation<LinkerInput, string> _linkerBuilder;

  public async Task<IDeviceDefinition> Make()
  {
    try
    {
      var builder = new BuildCsProj(_docker, _fileService, _projectHelper);
      var deviceFilename = await builder.Execute(Path);
      var dllProj = new DllProjectManager(deviceFilename, _onMake, _docker, _fileService, _projectHelper, _linkerBuilder, _deviceDefFactory, _appDefFactory,
        _dllProjectManagerAddAssembliesFromSource, Path);

      var dd = await dllProj.Make();
      _device = dd;
      return dd;
    }
    catch (Exception e)
    {
      _console.WriteError(e);
      throw;
    }
  }

  public bool CanMakeMultipleTimes => true;

  public async Task<FileInfo> CreatePackage(SystemInfo systemInfo)
  {
    if (_device is null)
      throw new InvalidOperationException($"Device definition is null : {nameof(Make)} must be call before {nameof(CreatePackage)}");

    var (linkerOptions, binds) = MapAppPath();
    var buildGui = _device.UserInterface != null;
    var builder = new BuildImpliciXPackage(_docker, _fileService, _projectHelper, _linkerBuilder, systemInfo, buildGui);
    var packageFilename = await builder.Execute(new LinkerInput(
      _device.Name,
      _device.EntryPoint,
      _device.Version,
      linkerOptions,
      binds
    ));

    return new FileInfo(packageFilename);
  }

  public async Task RunGui()
  {
    var (linkerOptions, binds) = MapAppPath();
    var runner = new RunGui(_docker, _projectHelper, _linkerBuilder);
    await runner.Execute(new LinkerInput(
      _device.Name,
      _device.EntryPoint,
      _device.Version,
      linkerOptions,
      binds)
    );
  }

  private (string[] linkerOptions, string[] binds) MapAppPath()
  {
    var gitDirectory = _projectHelper.FindGitDirectory(Path);
    var relativePath = System.IO.Path.GetRelativePath(gitDirectory, Path);
    var project_tmp_folder = _projectHelper.CreateTempDirectory();
    _projectHelper.CopyAndPrepareProjectDirectory(gitDirectory, project_tmp_folder);
    var linkerOptions = new[] {"-p", $"/app/{relativePath}".ToLinuxPath()};
    var binds = new[] {$"{project_tmp_folder}:/app"};
    return (linkerOptions, binds);
  }
}