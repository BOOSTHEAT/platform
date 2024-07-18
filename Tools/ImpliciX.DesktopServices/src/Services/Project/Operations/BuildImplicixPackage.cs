using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using ImpliciX.Language.Core;
using JetBrains.Annotations;
using static ImpliciX.DesktopServices.Services.Project.ProjectHelper;

// ReSharper disable once CheckNamespace
namespace ImpliciX.DesktopServices.Services.Project;

internal class BuildImpliciXPackage : IProjectOperation<LinkerInput, string>
{
  private readonly IDockerService _docker;
  private readonly IFileSystemService _filService;
  private readonly IProjectOperation<LinkerInput, string> _linkerBuilder;
  private readonly IProjectHelper _projectHelper;

  public BuildImpliciXPackage(
    IDockerService docker,
    IFileSystemService filService,
    [NotNull] IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder,
    SystemInfo systemInfo,
    bool buildGui
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _filService = filService ?? throw new ArgumentNullException(nameof(filService));
    _projectHelper = projectHelper ?? throw new ArgumentNullException(nameof(projectHelper));
    _linkerBuilder = linkerBuilder ?? throw new ArgumentNullException(nameof(linkerBuilder));
    TmpDirectory = projectHelper.CreateTempDirectory();

    BuildGuiOp = buildGui
      ? new BuildGui(
        _docker,
        filService,
        projectHelper,
        _linkerBuilder,
        systemInfo,
        TmpDirectory
      )
      : Option<BuildGui>.None();

    BuildImpliciXAppOp = new BuildImpliciXApp(
      _docker,
      filService,
      projectHelper,
      _linkerBuilder,
      systemInfo,
      TmpDirectory
    );
    ContainerTmpDir = "/build_tmp";
  }

  public string TmpDirectory { get; }

  private Option<BuildGui> BuildGuiOp { get; }
  public string ContainerTmpDir { get; }

  private BuildImpliciXApp BuildImpliciXAppOp { get; }

  public async Task<string> Execute(
    LinkerInput input
  )
  {
    var linkerPath = await _linkerBuilder.Execute(input);
    var appPath = await BuildImpliciXAppOp.Execute(input);

    var outputPath = Path.Combine(
      ContainerTmpDir,
      "publish",
      $"{input.AppName}.{input.Version}.zip"
    );
    var hostOutputPath = Path.Combine(
      TmpDirectory,
      "publish",
      $"{input.AppName}.{input.Version}.zip"
    );

    var commands = RunLinker.Concat(
      new[]
      {
        "pack",
        "-n", input.AppName,
        "-v", input.Version,
        "-o", outputPath.ToLinuxPath(),
        "-p", $"device:app,{input.Version},{Path.Combine(ContainerTmpDir, appPath).ToLinuxPath()}"
      }
    ).ToList();

    if (BuildGuiOp.IsSome)
    {
      var guiPath = await BuildGuiOp.GetValue().Execute(input);
      commands.AddRange(
        new[] { "-p", $"device:gui,{input.Version},{Path.Combine(ContainerTmpDir, guiPath).ToLinuxPath()}" }
      );
    }

    await _docker.Batch(
      new CreateContainerParameters
      {
        Name = nameof(BuildImpliciXPackage),
        Image = DotnetSdkImageName,
        Env = new List<string>(),
        HostConfig = new HostConfig
        {
          Binds = new[]
          {
            $"{TmpDirectory}:{ContainerTmpDir}",
            $"{linkerPath}:/linker"
          }.Concat(input.Binds.ToArray()).ToList()
        },
        Entrypoint = EntryPointBuilderFactory.Create()
          .SetCommand(
            string.Join(
              ' ',
              commands
            )
          )
          .Build()
      }
    );

    await _projectHelper.Until(() => _filService.FileExists(hostOutputPath));

    return hostOutputPath;
  }
}
