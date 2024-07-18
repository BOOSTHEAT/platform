using ImpliciX.DesktopServices.Helpers;

namespace ImpliciX.DesktopServices.Tests;

[TestFixture]
[Ignore("Manual test only with appropriate .nupkg file")]
public class NupkgLoaderTests
{
  [Test]
  public void should_load_assemblies()
  {
    var fullFilename = Path.Combine(Environment.CurrentDirectory, "Projects", "resources", "Demo.Caliper.2023.11.22.1.nupkg");
    var result = NupkgLoader.LoadAssemblies(fullFilename);
    Assert.That(result.NuPkgId, Is.EqualTo("Demo.Caliper"));
    Assert.That(
      result.Assemblies.Select(a => a.FullName).OrderBy(n => n),
      Is.EqualTo( new []
      {
        "Caliper.App, Version=2023.11.22.1, Culture=neutral, PublicKeyToken=null",
        "Caliper.Model, Version=2023.11.22.1, Culture=neutral, PublicKeyToken=null",
      })
    );
  }
  
  [Test]
  public void should_create_application_definition()
  {
    var fullFilename = Path.Combine(Environment.CurrentDirectory, "Projects", "resources", "Demo.Caliper.2023.11.22.1.nupkg");
    var result = NupkgLoader.CreateApplication(fullFilename);
    Assert.That(result.NuPkgId, Is.EqualTo("Demo.Caliper"));
    Assert.That(result.App.AppName, Is.EqualTo("Caliper"));
    Assert.That(
      result.App.DataModelDefinition.Assembly.FullName,
      Is.EqualTo("Caliper.Model, Version=2023.11.22.1, Culture=neutral, PublicKeyToken=null")
    );
  }
}