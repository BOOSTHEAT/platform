using System.Xml.Linq;
using ImpliciX.Linker.Services;

namespace ImpliciX.Linker;

internal sealed class CreateNugetConfigFileOperation
{
    private readonly IEnumerable<string> _sourcePaths;
    private readonly string _targetProjectFilePath;
    private readonly IFileSystemService _fileSystemService;

    public CreateNugetConfigFileOperation(IEnumerable<string> sourcePaths, string targetProjectFilePath, IFileSystemService fileSystemService)
    {
        _sourcePaths = sourcePaths;
        _targetProjectFilePath = targetProjectFilePath;
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
    }

    public void Execute()
    {
        var projectFileName = Path.GetFileName(_targetProjectFilePath);
        var nugetConfigFullPath = _targetProjectFilePath.Replace(projectFileName, "NuGet.Config");

        var xmlPackageSourcesPart = ToXmlPackageSources(_sourcePaths.ToArray());
        var generatedXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageSources>
    <clear />
    <add key=""nuget.org"" value=""https://api.nuget.org/v3/index.json"" protocolVersion=""3"" />
    {xmlPackageSourcesPart}
  </packageSources>
</configuration>";
        _fileSystemService.WriteAllText(nugetConfigFullPath, XDocument.Parse(generatedXml).ToString());
    }
    

    private static string ToXmlPackageSources(IReadOnlyList<string> nugetPackageSourcePaths)
    {
        var result = "";
        for (var i = 0; i < nugetPackageSourcePaths.Count; ++i)
        {
            result += $@"<add key=""NugetSourceKey{i}"" value=""{nugetPackageSourcePaths[i]}"" />";
        }

        return result;
    }
}