using System.Xml.Linq;
using ImpliciX.Linker.Services;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class CreateNugetConfigFileOperationTests
{
    [Test]
    public void GivenSingleSource_WhenIExecute_ThenICreateTheNugetConfigFileExpected()
    {
        // Given
        var nugetSourcePath = new[] { "https://bh/index.json" };
        const string targetProjectFilePath = "/tmp/proj.csproj";
        const string nugetFilePathExpected = "/tmp/NuGet.Config";

        var fileSystemService = new Mock<IFileSystemService>();

        // When
        new CreateNugetConfigFileOperation(nugetSourcePath, targetProjectFilePath, fileSystemService.Object).Execute();

        // Then
        var nugetContentExpected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        nugetContentExpected += "<configuration><packageSources>";
        nugetContentExpected += "<clear />";
        nugetContentExpected += "<add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" protocolVersion=\"3\" />";
        nugetContentExpected += "<add key=\"NugetSourceKey0\" value=\"https://bh/index.json\" />";
        nugetContentExpected += "</packageSources></configuration>";

        fileSystemService.Verify(o => o.WriteAllText(nugetFilePathExpected, XDocument.Parse(nugetContentExpected).ToString()), Times.Once);
    }

    [Test]
    public void GivenTwoSource_WhenIExecute_ThenICreateTheNugetConfigFileExpected()
    {
        // Given
        var nugetSourcePath = new[] { "https://bh/index.json", "https://bh/index2.json" };
        const string targetProjectFilePath = "/tmp/proj.csproj";
        const string nugetFilePathExpected = "/tmp/NuGet.Config";

        var fileSystemService = new Mock<IFileSystemService>();

        // When
        new CreateNugetConfigFileOperation(nugetSourcePath, targetProjectFilePath, fileSystemService.Object).Execute();

        // Then
        var nugetContentExpected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        nugetContentExpected += "<configuration><packageSources>";
        nugetContentExpected += "<clear />";
        nugetContentExpected += "<add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" protocolVersion=\"3\" />";
        nugetContentExpected += "<add key=\"NugetSourceKey0\" value=\"https://bh/index.json\" />";
        nugetContentExpected += "<add key=\"NugetSourceKey1\" value=\"https://bh/index2.json\" />";
        nugetContentExpected += "</packageSources></configuration>";

        fileSystemService.Verify(o => o.WriteAllText(nugetFilePathExpected, XDocument.Parse(nugetContentExpected).ToString()), Times.Once);
    }

    [Test]
    public void GivenThreeSource_WhenIExecute_ThenICreateTheNugetConfigFileExpected()
    {
        // Given
        var nugetSourcePath = new[] { "https://bh/index.json", "https://bh/index2.json", "https://bh/index3.json" };
        const string targetProjectFilePath = "/tmp/proj.csproj";
        const string nugetFilePathExpected = "/tmp/NuGet.Config";

        var fileSystemService = new Mock<IFileSystemService>();

        // When
        new CreateNugetConfigFileOperation(nugetSourcePath, targetProjectFilePath, fileSystemService.Object).Execute();

        // Then
        var nugetContentExpected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        nugetContentExpected += "<configuration><packageSources>";
        nugetContentExpected += "<clear />";
        nugetContentExpected += "<add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" protocolVersion=\"3\" />";
        nugetContentExpected += "<add key=\"NugetSourceKey0\" value=\"https://bh/index.json\" />";
        nugetContentExpected += "<add key=\"NugetSourceKey1\" value=\"https://bh/index2.json\" />";
        nugetContentExpected += "<add key=\"NugetSourceKey2\" value=\"https://bh/index3.json\" />";
        nugetContentExpected += "</packageSources></configuration>";

        fileSystemService.Verify(o => o.WriteAllText(nugetFilePathExpected, XDocument.Parse(nugetContentExpected).ToString()), Times.Once);
    }

 
}