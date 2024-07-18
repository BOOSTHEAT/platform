using System.CommandLine;
using System.Reflection;
using ImpliciX.Language;
using ImpliciX.Language.GUI;
using ImpliciX.Linker.Services;
using ImpliciX.ToQml;
using DateTime = System.DateTime;

namespace ImpliciX.Linker;

public class Qml : DotnetCommand
{
    public static void Generate(object entry, string outputFolder)
    {
        Console.WriteLine($"Begin QML generation");
        var moduleDefinition =
            (UserInterfaceModuleDefinition)((ApplicationDefinition)entry).ModuleDefinitions.First(m => m is UserInterfaceModuleDefinition);
        var folder = new DirectoryInfo(outputFolder);
        Console.WriteLine($"Generating QML in {folder.FullName}");
        folder.Create();
        var gui = moduleDefinition.UserInterface().ToSemanticModel();
        var copyrightManager = new CopyrightManager(((ApplicationDefinition)entry).AppName, DateTime.Now.Year);
        var rendering = new QmlRenderer(folder, copyrightManager);
        var result = QmlApplication.Create(gui, rendering);
        File.WriteAllLines(Path.Combine(folder.FullName,"generation_result.txt"), result);
        foreach (var row in result)
            Console.WriteLine(row);
        Console.WriteLine($"End QML generation");
    }

    public static Command CreateCommand()
    {
        var command = new Command("qml",
            "Create the QML GUI project for a given application")
        {
            new Option<string>(
                new[] { "-v", "--version" },
                "GUI version").VersionNumberOnly().Required(),
            new Option<string>(
                new[] { "-o", "--output" },
                "Output folder").LegalFilePathsOnly().NonExistingOnly().Required(),
        };
        AddOptionsTo(command);
        var _ = new Qml(command);
        return command;
    }

    private Qml(Command command) : base(command)
    {
    }

    protected override int Execute(IEnumerable<string> references, string appName, string entry,
        Dictionary<string, object> arguments)
    {
        var projectPath = GetProjectPath(appName, "QmlGenerator");
        CreateCsProj(references, (string)arguments["version"], projectPath);

        if (arguments.TryGetValue("source", out var source))
            new CreateNugetConfigFileOperation((IEnumerable<string>)source, projectPath, new FileSystemService()).Execute();

        var exitCode = CreateAndRunGenerationProgram(arguments, projectPath);
        return exitCode;
    }

    private static string GetProjectPath(string name, string suffix = "")
    {
        var projectFolder = Tools.GetTempFolder(name, "source", suffix).FullName;
        Directory.Delete(projectFolder, true);
        Directory.CreateDirectory(projectFolder);
        var projectName = "GenerateQML";
        var projectPath = Path.Combine(projectFolder, $"{projectName}.csproj");
        return projectPath;
    }

    private static void CreateCsProj(IEnumerable<string> references, string version, string projectPath)
    {
        var refs = string.Join('\n', references.Select(r => $"    {r}"));
        File.WriteAllText(projectPath,
            $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>{SdkVersion}</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
{refs}
    <Reference Include=""{Assembly.GetExecutingAssembly().Location}"" />
  </ItemGroup>
</Project>
");
    }

    private static int CreateAndRunGenerationProgram(Dictionary<string, object> arguments, string projectPath)
    {
        CreateMainProgram((string)arguments["entry"], projectPath, (string)arguments["output"]);
        var diagnostics = (bool)arguments["diagnostics"];
        Tools.Restore(new FileInfo(projectPath), diagnostics);
        var run = Tools.Run(new FileInfo(projectPath), diagnostics);
        return run.ExitCode;
    }

    private static void CreateMainProgram(string entryPoint, string projectPath, string outputFolder)
    {
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(projectPath)!, "Program.cs"),
            $@"namespace {Path.GetFileNameWithoutExtension(projectPath)}
{{
  public static class Program
  {{
    private static void Main(string[] args)
    {{
      ImpliciX.Linker.Qml.Generate( new {entryPoint}(), ""{outputFolder}"" );
    }}
  }}
}}
");
    }
}