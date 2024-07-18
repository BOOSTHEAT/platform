using System.CommandLine;
using System.IO.Compression;
using ImpliciX.Linker.Services;

namespace ImpliciX.Linker;

public class Build : DotnetCommand
{
  public static Command CreateCommand()
  {
    var command = new Command("build",
      "Create the executable project for a given application")
    {
      new Option<string>(
        new[] { "-t", "--target" },
        "Target OS and hardware architecture").DefaultValue("linux-arm"),
      new Option<string>(
        new[] { "-v", "--version" },
        "Application version").VersionNumberOnly().Required(),
      new Option<string>(
        new[] { "-o", "--output" },
        "Output zip file").LegalFilePathsOnly().NonExistingOnly().Required(),
    };
    AddOptionsTo(command);
    var _ = new Build(command);
    return command;
  }
  
  private Build(Command command) : base(command)
  {
  }
  
  protected override int Execute(IEnumerable<string> references, string appName, string entry,
    Dictionary<string, object> arguments)
  {
    var projectPath = GetProjectPath(appName , "runtime");
    CreateSourceCode(references, entry, (string)arguments["version"], projectPath);

    if (arguments.TryGetValue("source", out var source))
      new CreateNugetConfigFileOperation((IEnumerable<string>)source, projectPath, new FileSystemService()).Execute();

    return PublishProject(projectPath, appName, arguments);
  }
    
  private static string GetProjectPath(string appName,string suffix = "")
  {
    var projectFolder = Tools.GetTempFolder(appName, "source",suffix).FullName;
    Directory.Delete(projectFolder, true);
    Directory.CreateDirectory(projectFolder);
    var projectName = "BOOSTHEAT.Boiler.App";
    var projectPath = Path.Combine(projectFolder, $"{projectName}.csproj");
    return projectPath;
  }


  private static void CreateSourceCode(IEnumerable<string> references, string entryPoint, string version, string projectPath)
  {
    var refs = string.Join('\n', references.Select(r => $"    {r}"));
    File.WriteAllText(projectPath,
      $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{SdkVersion}</TargetFramework>
    <ServerGarbageCollection>false</ServerGarbageCollection>
    <ConcurrentGarbageCollection>false</ConcurrentGarbageCollection>
    <FileVersion>{version}</FileVersion>
    <AssemblyVersion>{version}</AssemblyVersion>
    <PublishReadyToRun>True</PublishReadyToRun>
    <DebugType>None</DebugType>
    <DebugSymbols>false</DebugSymbols>
  </PropertyGroup>

  <ItemGroup>
{refs}
    <RuntimeHostConfigurationOption Include=""System.Globalization.Invariant"" Value=""true"" />
  </ItemGroup>
</Project>
");
    File.WriteAllText(Path.Combine(Path.GetDirectoryName(projectPath)!, "Program.cs"),
      $@"namespace ImpliciXGeneratedMainProgramNamespace
{{
  public static class Program
  {{
    private static void Main(string[] args)
    {{
      ImpliciX.Runtime.Application.Run( new {entryPoint}(), args );
    }}
  }}
}}
");
  }

  private static int PublishProject(string projectPath, string appName, Dictionary<string, object> arguments)
  {
    try
    {
      var publishFolder = Tools.GetTempFolder(appName, "publish").FullName;
      var diagnostics = (bool)arguments["diagnostics"];
      var target = (string)arguments["target"];
      Tools.Restore(new FileInfo(projectPath), diagnostics,target);
      var result = Tools.Publish(projectPath, publishFolder, diagnostics,target);
      if (result.ExitCode != 0)
        return result.ExitCode;
      var outputZipFile = (string)arguments["output"];
      Directory.CreateDirectory(Path.GetDirectoryName(outputZipFile)!);
      ZipFile.CreateFromDirectory(publishFolder, outputZipFile);
      Console.WriteLine("Publish complete.");
      return 0;
    }
    catch (Exception e)
    {
      Console.WriteLine("Publish failed.");
      Console.WriteLine(e.Message);
      return e.HResult;
    }
  }
}