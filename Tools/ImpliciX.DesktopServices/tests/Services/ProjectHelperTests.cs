using ImpliciX.DesktopServices.Services.Project;

namespace ImpliciX.DesktopServices.Tests.Services;

[TestFixture]
public class ProjectHelperTests
{
  [Test]
  public void GetGeneratedDllName_ShouldReturnCorrectDllName()
  {
    const string csProjPath = "path/to/project.csproj";
    const string tempDirectory = "tmp/dir";
    var expectedDllName = $"tmp/dir{Path.DirectorySeparatorChar}project.dll";

    var result = ProjectHelper.GetGeneratedDllName(csProjPath, tempDirectory);

    Assert.AreEqual(expectedDllName, result);
  }

  [TestCase("2023-12-25 20:23:45.2", "0.2023.8612.14252")]
  [TestCase("2023-12-25 20:23:45.3", "0.2023.8612.14253")]
  [TestCase("2023-01-01 00:00:00", "0.2023.0.0")]
  [TestCase("2023-02-01 00:00:12", "0.2023.744.120")]
  [TestCase("2023-12-31 23:59:59.9", "0.2023.8759.35999")]
  [TestCase("2024-01-01 00:00:00", "0.2024.0.0")]
  [TestCase("2024-02-01 00:00:12", "0.2024.744.120")]
  [TestCase("2024-12-31 23:59:59.9", "0.2024.8783.35999")]
  public void DateTimeToVersionNumber(string actualDatetime, string expectedVersion)
  {
    var dt = DateTime.Parse(actualDatetime);
    Assert.That(ProjectHelper.CreateDevVersionFromDate(dt), Is.EqualTo(expectedVersion));
  }




}