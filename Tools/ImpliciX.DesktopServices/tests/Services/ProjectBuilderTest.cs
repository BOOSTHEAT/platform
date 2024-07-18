using ImpliciX.DesktopServices.Services.Project;

namespace ImpliciX.DesktopServices.Tests.Services;

[TestFixture]
public class ProjectBuilderTest
{
    private readonly FakeBuilder _fakeBuilder;

    private class FakeBuilder
    {
        private readonly ProjectHelper _projectHelper;

        public void FakeCopyScriptToTmpDirectory(string tmpDirectory, string csProjBuilderScriptName)
            => _projectHelper.CopyScriptToTmpDirectory(tmpDirectory, csProjBuilderScriptName);

        public string FakeCreateTempDirectory()
            => _projectHelper.CreateTempDirectory();

        public string FakeFindGitDirectory(string filePath, int max_depth = 4)
            => _projectHelper.FindGitDirectory(filePath, max_depth);

        public FakeBuilder()
        {
            _projectHelper = new ProjectHelper();
        }
    }

    [Test]
    public void CopyBuilderScriptToTmpDirectory_ShouldThrowException_WhenResourceNotFound()
    {
        const string tmpDirectory = "tmp/dir";
        Assert.Throws<ArgumentException>(() =>
            _fakeBuilder.FakeCopyScriptToTmpDirectory(tmpDirectory, "non_existing_script.sh"));
    }


    [Test]
    public void CreateTempDirectory_ShouldReturnValidTempDirectory()
    {
        var tempDirectory = _fakeBuilder.FakeCreateTempDirectory();
        Assert.IsTrue(Directory.Exists(tempDirectory));
    }

    [TestCase("path/to/project.csproj", "tmp/dir", "project.dll")]
    [TestCase("path\\to\\project.csproj", "tmp\\dir", "project.dll", PlatformID.Win32NT)]
    public void GetGeneratedDllName_ShouldReturnCorrectDllName(string csProjPath, string tempDirectory,
        string expectedDllName, PlatformID? restrictedTo = null)
    {
        if (restrictedTo.HasValue && restrictedTo.Value != Environment.OSVersion.Platform)
        {
            Assert.Ignore();
            return;
        }

        var result = ProjectHelper.GetGeneratedDllName(csProjPath, tempDirectory);

        Assert.That(result, Is.EqualTo($"{tempDirectory}{Path.DirectorySeparatorChar}{expectedDllName}"));
    }

    [Test]
    public void FindGitDirectory_ShouldReturnValidGitDirectory()
    {
        var tmpDir = Directory.CreateTempSubdirectory("TestFindGitDirectory");
        var path0 = Path.Combine(tmpDir.FullName, "toto");
        var path1 = Path.Combine(path0, ".git");
        var path2 = Path.Combine(path0, "tutu", "titi");
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);
        var result = _fakeBuilder.FakeFindGitDirectory(path2);
        Assert.AreEqual(path0, result);
    }

    [Test]
    public void FindGitDirectory_ShouldThrowException_WhenGitDirectoryNotFoundWithinMaxDepth()
    {
        var tmpDir = Directory.CreateTempSubdirectory("TestFindGitDirectory");
        var path1 = Path.Combine(tmpDir.FullName, "toto", ".git");
        var path2 = Path.Combine(tmpDir.FullName, "toto", "tutu", "titi", "tata");
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);
        Assert.Throws<DirectoryNotFoundException>(() => _fakeBuilder.FakeFindGitDirectory(path2, 2));
    }

    public ProjectBuilderTest()
    {
        _fakeBuilder = new FakeBuilder();
    }
}