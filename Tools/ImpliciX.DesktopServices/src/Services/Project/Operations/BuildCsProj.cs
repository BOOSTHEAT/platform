using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using JetBrains.Annotations;
using static ImpliciX.DesktopServices.Services.Project.ProjectHelper;

// ReSharper disable once CheckNamespace
namespace ImpliciX.DesktopServices.Services.Project;

internal sealed class BuildCsProj : IProjectOperation<string, string>
{
  [NotNull] private readonly IDockerService _docker;
  [NotNull] private readonly IFileSystemService _fileService;
  [NotNull] private readonly IProjectHelper _projectHelper;

  public BuildCsProj(
    [NotNull] IDockerService docker,
    [NotNull] IFileSystemService fileService,
    [NotNull] IProjectHelper projectHelper
  )
  {
    _docker = docker ?? throw new ArgumentNullException(nameof(docker));
    _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    _projectHelper = projectHelper ?? throw new ArgumentNullException(nameof(projectHelper));
  }

  public async Task<string> Execute(
    string csProjPath
  )
  {
    var tmpOutputDirectory = _projectHelper.CreateTempDirectory();
    await RunCsProjBuildAsync(
      csProjPath,
      tmpOutputDirectory
    );
    return GetGeneratedDllName(
      csProjPath,
      tmpOutputDirectory
    );
  }

  private async Task RunCsProjBuildAsync(
    string csProjPath,
    string tmpDirectory
  )
  {
    const string containerTmpDir = "/build_tmp";
    var gitDirectory = _projectHelper.FindGitDirectory(csProjPath);
    var relativePath = Path.GetRelativePath(
      gitDirectory,
      csProjPath
    ).ToLinuxPath();
    var tmpProjDirectory = _projectHelper.CreateTempDirectory();
    _projectHelper.CopyAndPrepareProjectDirectory(
      gitDirectory,
      tmpProjDirectory
    );
    GenerateNugetConfig(
      tmpProjDirectory,
      NugetLocalFeedUrl
    );

    await _docker.Pull(DotnetSdkImageName);

    await _docker.Batch(
      new CreateContainerParameters
      {
        Name = nameof(BuildCsProj),
        Image = DotnetSdkImageName,
        Env = new List<string>
        {
          $"ASSEMBLY_VERSION={CreateDevVersionFromDate(DateTime.Now)}",
          $"RELATIVE_PATH={relativePath}"
        },
        HostConfig = new HostConfig
        {
          Binds = new[]
          {
            $"{tmpProjDirectory}:/app",
            $"{tmpDirectory}:{containerTmpDir}",
            $"{GetNugetLocalPackagesCachePath()}:{NugetLocalFeedUrl}"
          }
        },
        WorkingDir = containerTmpDir,
        Entrypoint = EntryPointBuilderFactory.Create()
          .LinkUserNugetPackages()
          .SetCommand(
            "dotnet restore /app/$RELATIVE_PATH --configfile /app/NuGet.Config && dotnet publish --no-restore --configuration Release " +
            $"/p:AssemblyVersion=$ASSEMBLY_VERSION /p:Version=$ASSEMBLY_VERSION -o {containerTmpDir.ToLinuxPath()} /app/$RELATIVE_PATH"
          ).Build()
      }
    );

    await _docker.Wait(nameof(BuildCsProj));
    if (_fileService.DirectoryGetFiles(tmpDirectory).Length <= 1)
      throw new ApplicationException($"Failed to compile {csProjPath}");
  }

  private void GenerateNugetConfig(
    string tmpDirectory,
    string nugetLocalFeedUrl
  )
  {
    var nugetConfig = $@"<?xml version=""1.0"" encoding=""utf-8""?>
        <configuration>
            <packageSources>
                <add key=""ImpliciXToolChain"" value=""{nugetLocalFeedUrl}"" />           
                <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3""/>
            </packageSources>
        </configuration>";

    _fileService.WriteAllText(
      Path.Combine(
        tmpDirectory,
        "NuGet.Config"
      ), nugetConfig
    );
  }
}
