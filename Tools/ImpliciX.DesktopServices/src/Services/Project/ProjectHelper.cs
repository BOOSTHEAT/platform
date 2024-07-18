using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ImpliciX.DesktopServices.Services.Project;

internal sealed class ProjectHelper : IProjectHelper
{
    internal const string NugetLocalFeedUrl = "/nugetPackages";
    public const string DotnetSdkImageName = "implicixpublic.azurecr.io/implicix-dotnet7:latest";
    internal const string QtImageName = "implicixpublic.azurecr.io/implicix-qt5:latest";
    internal const string QtImageNameArm32 = "implicixpublic.azurecr.io/implicix-qt5-arm32:latest";

    internal static readonly string[] RunLinker =
    {
        "/usr/bin/dotnet", "exec",
        "--runtimeconfig",
        "/usr/share/dotnet/shared/Microsoft.AspNetCore.App/7.0.7/Microsoft.AspNetCore.App.runtimeconfig.json",
        "/linker/ImpliciX.Linker.dll"
    };

    public void CopyScriptToTmpDirectory(string tmpDirectory, string csProjBuilderScriptName)
    {
        var resourceName = "ImpliciX.DesktopServices.Services.Project." + csProjBuilderScriptName;
        var assembly = Assembly.GetExecutingAssembly();
        using var resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream == null)
        {
            throw new ArgumentException("No such resource", resourceName);
        }

        using var fileStream = new FileStream(Path.Combine(tmpDirectory, csProjBuilderScriptName), FileMode.Create, FileAccess.Write);
        resourceStream.CopyTo(fileStream);
    }

    public void CopyAndPrepareProjectDirectory(string sourceFolder, string destFolder)
    {
        if (!Directory.Exists(sourceFolder))
        {
            throw new DirectoryNotFoundException($"Source directory does not exist: {sourceFolder}");
        }

        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
        }

        foreach (var file in Directory.GetFiles(sourceFolder))
        {
            if (Path.GetFileName(file.ToLower()).StartsWith(".git") || Path.GetFileName(file.ToLower()) == "nuget.config") continue;
            var destFile = Path.Combine(destFolder, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceFolder))
        {
            if (Path.GetFileName(dir) == ".git" || Path.GetFileName(dir) == ".idea" || Path.GetFileName(dir) == ".vscode") continue;
            var destDir = Path.Combine(destFolder, Path.GetFileName(dir));
            CopyAndPrepareProjectDirectory(dir, destDir);
        }
    }

    public string CreateTempDirectory()
    {
        var name = $"DesignerAppBuilder.{Guid.NewGuid()}";
        var tmpDir = Path.GetTempPath();
        var tmpAppDir = Path.Combine(tmpDir, name);
        Directory.CreateDirectory(tmpAppDir);
        return tmpAppDir;
    }

    public string FindGitDirectory(string filePath, int max_depth = 4)
    {
        var dir = new DirectoryInfo(filePath);
        var currentDepth = 0;

        while (dir != null && currentDepth <= max_depth)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
            currentDepth++;
        }

        throw new DirectoryNotFoundException("Could not find .git directory");
    }

    public async Task Until(Func<bool> condition, int timeout = 10_000)
    {
        var waitTime = 0;
        while (!condition() && waitTime < timeout)
        {
            await Task.Delay(100);
            waitTime += 100;
        }
    }

    public static string CreateDevVersionFromDate(DateTime now)
    {
        var hoursInCurrentYear = (int) (24 * now.Subtract(new DateTime(now.Year, 1, 1)).TotalDays);
        var tenthOfSecondInCurrentHour = (int) (now.TimeOfDay.Subtract(new TimeSpan(now.TimeOfDay.Hours, 0, 0)).TotalMilliseconds / 100);
        return $"0.{now.ToString("yyyy")}.{hoursInCurrentYear}.{tenthOfSecondInCurrentHour}";
    }

    public static string GetNugetLocalPackagesCachePath()
    {
        var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (path == null)
        {
            throw new InvalidOperationException("Could not get executing assembly location");
        }

        return Path.Combine(path, "NuGetPackages");
    }

    public static string GetGeneratedDllName(string csProjPath, string tempDirectory)
    {
        var filename = Path.GetFileName(csProjPath);
        filename = filename.Replace(".csproj", ".dll");
        return Path.Combine(tempDirectory, filename);
    }

    public const string QtBuildFolderName = "qt_build";

    public const string ImpliciXGuiExeName = "ImpliciX.GUI";

    public static string GenerateLinkerWrapperProj(string destFolder)
    {
        var projPath = Path.Combine(destFolder, "LinkerWrapper.csproj");
        const string csProj = $@"<Project Sdk=""Microsoft.NET.Sdk"">

            <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net8.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <LangVersion>latestmajor</LangVersion>
            </PropertyGroup>
            <ItemGroup>
            <PackageReference Include=""ImpliciX.Linker"" Version=""*"" />
            </ItemGroup>
            </Project>";
        File.WriteAllText(projPath, XDocument.Parse(csProj).ToString());
        const string nuget = $@"<?xml version=""1.0"" encoding=""utf-8""?>
            <configuration>
            <packageSources>
            <add key=""ImpliciX"" value=""{NugetLocalFeedUrl}"" />
            <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
            </packageSources>
            </configuration>";
        File.WriteAllText(Path.Combine(destFolder, "NuGet.Config"), XDocument.Parse(nuget).ToString());
        const string program = $@"Console.WriteLine(""Linker wrapper > Build Success"");";
        File.WriteAllText(Path.Combine(destFolder, "Program.cs"), program);
        
        return projPath;
    }
}

internal static class ProjectHelperExtensions
{
    public static string ToLinuxPath(this string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/');
    }
}