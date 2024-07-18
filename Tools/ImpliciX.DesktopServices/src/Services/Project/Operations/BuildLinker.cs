using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using JetBrains.Annotations;
using static ImpliciX.DesktopServices.Services.Project.ProjectHelper;

namespace ImpliciX.DesktopServices.Services.Project;

internal sealed class BuildLinker : IProjectOperation<LinkerInput, string>
{
  [NotNull] private readonly IDockerService _docker;

  public BuildLinker(
    [NotNull] IDockerService docker
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
  }

  public async Task<string> Execute(
    LinkerInput input
  )
  {
    var linkerWrapperTemp = Path.Combine(
      Path.GetTempPath(),
      Path.GetRandomFileName()
    );
    await RunLinkerBuildAsync(linkerWrapperTemp);
    return GetLinkerPath(linkerWrapperTemp);
  }

  private string GetLinkerPath(
    string linkerWrapperTemp
  )
  {
    return Path.Combine(
      linkerWrapperTemp,
      "publish"
    );
  }

  private async Task RunLinkerBuildAsync(
    string linkerWrapperTemp
  )
  {
    Directory.CreateDirectory(linkerWrapperTemp);
    GenerateLinkerWrapperProj(linkerWrapperTemp);

    await _docker.Pull(DotnetSdkImageName);

    await _docker.Batch(
      new CreateContainerParameters
      {
        Name = nameof(RunLinkerBuildAsync),
        Image = DotnetSdkImageName,
        HostConfig = new HostConfig
        {
          Binds = new[]
          {
            $"{linkerWrapperTemp}:/linker",
            $"{GetNugetLocalPackagesCachePath()}:{NugetLocalFeedUrl}"
          }.ToList()
        },
        Entrypoint = EntryPointBuilderFactory.Create()
          .LinkUserNugetPackages()
          .SetCommand("dotnet publish /linker -r linux-x64 --self-contained true -o /linker/publish")
          .Build()
      }
    );

    await _docker.Wait(nameof(RunLinkerBuildAsync));

    if (!Directory.Exists(
          Path.Combine(
            linkerWrapperTemp,
            "bin"
          )
        ))
      throw new ApplicationException("Failed to generate Linker");
  }
}
