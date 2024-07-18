using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using ImpliciX.Language.Core;
using JetBrains.Annotations;
using static ImpliciX.DesktopServices.Services.Project.ProjectHelper;

// ReSharper disable once CheckNamespace
namespace ImpliciX.DesktopServices.Services.Project;

internal class RunGui : IProjectOperation<LinkerInput, Unit>
{
  private const string WindowsHost = "host.docker.internal";
  private readonly IDockerService _docker;

  public RunGui(
    [NotNull] IDockerService docker,
    [NotNull] IProjectHelper projectHelper,
    [NotNull] IProjectOperation<LinkerInput, string> linkerBuilder
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    if (projectHelper == null) throw new ArgumentNullException(nameof(projectHelper));

    TmpDirectory = projectHelper.CreateTempDirectory();
    ContainerTmpDir = "/host_tmp";
    BuildGuiOperation =
      new BuildGui(
        docker,
        new FileSystemService(),
        projectHelper,
        linkerBuilder,
        new SystemInfo(
          OS.linux,
          Architecture.x64,
          "generic"
        ), TmpDirectory
      );
  }

  public string TmpDirectory { get; }
  public string ContainerTmpDir { get; }

  private BuildGui BuildGuiOperation { get; }

  public async Task<Unit> Execute(
    LinkerInput input
  )
  {
    var guiExe = await BuildGuiOperation.Execute(input);
    await LaunchGui(
      guiExe,
      input.Binds.ToArray()
    );
    return default;
  }

  private async Task LaunchGui(
    string guiExe,
    string[] binds
  )
  {
    Action<CreateContainerParameters> addOsSpecificTo = Environment.OSVersion.Platform switch
    {
      PlatformID.Unix => settings =>
      {
        settings.Env.Add($"DISPLAY={Environment.GetEnvironmentVariable("DISPLAY")}");
        settings.Env.Add("BACKEND=127.0.0.1");
        settings.HostConfig.Binds.Add($"{Environment.GetEnvironmentVariable("HOME")}/.Xauthority:/root/.Xauthority:rw");
        settings.HostConfig.NetworkMode = "host";
      },
      PlatformID.Win32NT => settings =>
      {
        settings.Env.Add($"DISPLAY={WindowsHost}:0");
        settings.Env.Add($"BACKEND={WindowsHost}");
      },
      _ => throw new NotSupportedException($"Running GUI is not supported on {Environment.OSVersion.Platform}")
    };

    await _docker.Pull(QtImageName);
    var settings = new CreateContainerParameters
    {
      Name = nameof(LaunchGui),
      Image = QtImageName,
      Env = new List<string>
      {
        "QT_QUICK_BACKEND=software",
        "QT_IM_MODULE=qtvirtualkeyboard",
        $"GUI_EXE={guiExe.ToLinuxPath()}"
      },
      HostConfig = new HostConfig
      {
        Binds = new[]
        {
          $"{TmpDirectory}:{ContainerTmpDir}"
        }.Concat(binds).ToList()
      },
      WorkingDir = ContainerTmpDir,
      Entrypoint = new[]
      {
        "/bin/sh", "-c", $"${{GUI_EXE}} backend=${{BACKEND}}:{RemoteDevice.WebSocketLocalPort} loglevel=VERBOSE"
      }
    };

    addOsSpecificTo(settings);
    await _docker.Batch(settings);
  }
}
