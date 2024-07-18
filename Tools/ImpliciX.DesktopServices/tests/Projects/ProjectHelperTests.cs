using ImpliciX.DesktopServices.Services.Project;

namespace ImpliciX.DesktopServices.Tests.Projects;

internal class ProjectHelperTests
{
    [Test]
    public void GenerateLinkerWrapperProj_WritesExpectedFiles()
    {
        var destFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(destFolder);

        var csProjPath = ProjectHelper.GenerateLinkerWrapperProj(destFolder);
        
        Assert.AreEqual("LinkerWrapper.csproj", Path.GetFileName(csProjPath));
        Assert.True(File.Exists(Path.Combine(destFolder, "LinkerWrapper.csproj")));
        Assert.True(File.Exists(Path.Combine(destFolder, "NuGet.Config")));
        Assert.True(File.Exists(Path.Combine(destFolder, "Program.cs")));

        var csprojContent = File.ReadAllText(Path.Combine(destFolder, "LinkerWrapper.csproj"));
        var nugetConfigContent = File.ReadAllText(Path.Combine(destFolder, "NuGet.Config"));
        var programContent = File.ReadAllText(Path.Combine(destFolder, "Program.cs"));

        Assert.True(csprojContent.Contains("ImpliciX.Linker"));
        Assert.True(csprojContent.Contains("net8.0"));
        Assert.True(nugetConfigContent.Contains("ImpliciX"));
        Assert.True(programContent.Contains("Linker wrapper > Build Success"));
        
        Directory.Delete(destFolder, true);
    }
}