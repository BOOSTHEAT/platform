using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using JetBrains.Annotations;
using static ImpliciX.DesktopServices.Services.Project.ProjectHelper;

// ReSharper disable once CheckNamespace
namespace ImpliciX.DesktopServices.Services.Project;

internal class BuildImpliciXApp : IProjectOperation<LinkerInput, string>
{
  private readonly IDockerService _docker;
  private readonly IFileSystemService _fileService;
  private readonly IProjectOperation<LinkerInput, string> _linkerBuilder;
  private readonly IProjectHelper _projectHelper;
  private readonly SystemInfo _systemInfo;

  public BuildImpliciXApp(
    [NotNull] IDockerService docker,
    [NotNull] IFileSystemService fileService,
    [NotNull] IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder,
    [NotNull] SystemInfo systemInfo,
    string tmpDirectory = null
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _projectHelper = projectHelper ?? throw new ArgumentNullException(nameof(projectHelper));
    _systemInfo = systemInfo ?? throw new ArgumentNullException(nameof(systemInfo));
    _linkerBuilder = linkerBuilder ?? throw new ArgumentNullException(nameof(linkerBuilder));
    TmpDirectory = tmpDirectory ?? _projectHelper.CreateTempDirectory();
    ContainerTmpDir = "/build_tmp";
  }

  private string ContainerTmpDir { get; }
  private string TmpDirectory { get; }

  public async Task<string> Execute(
    LinkerInput input
  )
  {
    var linkerPath = await _linkerBuilder.Execute(input);
    var outputPath = Path.Combine(
      ContainerTmpDir,
      "app.zip"
    );
    var hostOutputPath = Path.Combine(
      TmpDirectory,
      "app.zip"
    );

    var commands = RunLinker.Concat(
      new[]
      {
        "build",
        "-n", "Implicix.Runtime",
        "-t", $"{_systemInfo.Os}-{_systemInfo.Architecture}".ToLower(),
        "-v", input.Version,
        "-s", NugetLocalFeedUrl,
        "-e", input.AppEntryPoint,
        "-o", outputPath.ToLinuxPath()
      }
    ).Concat(input.LinkerOptions).ToList();

    await _docker.Batch(
      new CreateContainerParameters
      {
        Name = nameof(BuildImpliciXApp),
        Image = DotnetSdkImageName,
        HostConfig = new HostConfig
        {
          Binds = new[]
          {
            $"{TmpDirectory}:{ContainerTmpDir}",
            $"{linkerPath}:/linker",
            $"{GetNugetLocalPackagesCachePath()}:{NugetLocalFeedUrl}"
          }.Concat(input.Binds.ToArray()).ToList()
        },
        Entrypoint = EntryPointBuilderFactory.Create()
          .LinkUserNugetPackages()
          .SetCommand(
            string.Join(
              ' ',
              commands
            )
          )
          .Build()
      }
    );

    await _projectHelper.Until(() => _fileService.FileExists(hostOutputPath));
    await _docker.Wait(nameof(BuildImpliciXApp));
    if (!_fileService.FileExists(hostOutputPath))
      throw new ApplicationException("Failed to build Implicix app");

    return outputPath;
  }
}
