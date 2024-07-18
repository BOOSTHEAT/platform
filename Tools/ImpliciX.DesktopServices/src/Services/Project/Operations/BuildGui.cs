using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using JetBrains.Annotations;
using static ImpliciX.DesktopServices.Services.Project.ProjectHelper;

// ReSharper disable once CheckNamespace
namespace ImpliciX.DesktopServices.Services.Project;

internal class BuildGui : IProjectOperation<LinkerInput, string>
{
  private readonly IDockerService _docker;
  private readonly IFileSystemService _fileService;
  private readonly IProjectOperation<LinkerInput, string> _linkerBuilder;
  private readonly SystemInfo _systemInfo;

  public BuildGui(
    IDockerService docker,
    IFileSystemService fileService,
    IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder,
    [NotNull] SystemInfo systemInfo,
    [CanBeNull] string tmpDirectory = null
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _systemInfo = systemInfo ?? throw new ArgumentNullException(nameof(systemInfo));
    _linkerBuilder = linkerBuilder ?? throw new ArgumentNullException(nameof(linkerBuilder));
    TmpDirectory = tmpDirectory ??
                   projectHelper?.CreateTempDirectory() ?? throw new ArgumentNullException(nameof(projectHelper));
    ContainerTmpDir = "/host_tmp";
  }

  private string TmpDirectory { get; }
  private string ContainerTmpDir { get; }

  public async Task<string> Execute(
    LinkerInput input
  )
  {
    var folderBinds = input.Binds.ToArray();
    var linkerPath = await _linkerBuilder.Execute(input);
    await GenerateQmlSourceCode(
      input,
      linkerPath
    );
    GenerateVersionJsFile(
      TmpDirectory,
      input.Version
    );
    CreateQtBuildDirectory(TmpDirectory);
    await CompileGui(folderBinds);
    return Path.Combine(
      QtBuildFolderName,
      ImpliciXGuiExeName
    );
  }

  private async Task CompileGui(
    string[] binds
  )
  {
    var qtImage = _systemInfo.Architecture == Architecture.arm
      ? QtImageNameArm32
      : QtImageName;

    await _docker.Pull(qtImage);

    var settings = new CreateContainerParameters
    {
      Name = nameof(CompileGui),
      Image = qtImage,
      Env = new List<string>
      {
        "QT_QUICK_BACKEND=software",
        $"QT_BUILD_FOLDER_NAME={QtBuildFolderName}"
      },
      HostConfig = new HostConfig
      {
        Binds = new[]
        {
          $"{TmpDirectory}:{ContainerTmpDir}"
        }.Concat(binds).ToList()
      },
      Entrypoint = EntryPointBuilderFactory.Create()
        .SetCommand($"cd {ContainerTmpDir} && qmake -o {QtBuildFolderName}/Makefile && make -C {QtBuildFolderName}")
        .Build()
    };

    await _docker.Batch(settings);
    await _docker.Wait(settings.Name);
  }

  private async Task GenerateQmlSourceCode(
    LinkerInput input,
    string linkerPath
  )
  {
    await _docker.Pull(DotnetSdkImageName);

    var commands = RunLinker.Concat(
      new[]
      {
        "qml",
        "-v", input.Version,
        "-s", NugetLocalFeedUrl,
        "-e", input.AppEntryPoint,
        "-o", ContainerTmpDir
      }
    ).Concat(input.LinkerOptions).ToList();

    await _docker.Batch(
      new CreateContainerParameters
      {
        Name = nameof(GenerateQmlSourceCode),
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

    await _docker.Wait(nameof(GenerateQmlSourceCode));

    if (!_fileService.FileExists(
          Path.Combine(
            TmpDirectory,
            "main.qrc"
          )
        ))
      throw new ApplicationException("Failed to generate QML");
  }

  private void GenerateVersionJsFile(
    string directory,
    string version
  )
  {
    var versionJs = Path.Combine(
      directory,
      "version.js"
    );
    _fileService.WriteAllText(
      versionJs,
      $"var version = '{version}';"
    );
  }

  private string CreateQtBuildDirectory(
    string directory
  )
  {
    var path = Path.Combine(
      directory,
      QtBuildFolderName
    );
    _fileService.CreateDirectory(path);
    return path;
  }
}
