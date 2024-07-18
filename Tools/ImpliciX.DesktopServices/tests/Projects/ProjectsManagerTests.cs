using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services;
using ImpliciX.DesktopServices.Services.Project;
using ImpliciX.Language.Core;
using Moq;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Projects;

public class ProjectsManagerTests
{
  [SetUp]
  public void Init()
  {
    UserSettings.Clear(PathHistory.PersistenceKey);
    _sut = new ProjectsManager(Mock.Of<IDockerService>(), Mock.Of<IConsoleService>());
    _loadedProjects = new List<Option<IManageProject>>();
    _loadedDevices = new List<Option<IDeviceDefinition>>();
    _sut.Projects.Subscribe(p => _loadedProjects.Add(p));
    _sut.Devices.Subscribe(d => _loadedDevices.Add(d));
    _sut.PreviousPaths.Subscribe(p => _previousPaths = p.ToList());
  }
  
  [Test]
  public void should_notify_project_load()
  {
    var fakeProject = new Mock<IManageProject>();
    var fakeDevice = new Mock<IDeviceDefinition>();
    fakeProject.Setup(x => x.Path).Returns("the path");
    fakeProject.Setup(x => x.Make()).Callback(() => _sut.OnMake(fakeDevice.Object));
    _sut.OnLoad(fakeProject.Object);
    Check.That(_loadedProjects.Single().GetValue()).IsEqualTo(fakeProject.Object);
    Check.That(_sut.LatestProject.IsSome).IsTrue();
    Check.That(_loadedProjects.Single().GetValue()).IsEqualTo(_sut.LatestProject.GetValue());
    Check.That(_sut.LatestDevice.IsSome).IsTrue();
    Check.That(_loadedDevices.Single().GetValue()).IsEqualTo(_sut.LatestDevice.GetValue());
    Check.That(_loadedDevices.Single().GetValue()).IsEqualTo(fakeDevice.Object);
    Check.That(_previousPaths).IsEqualTo(new [] {"the path"});
    Check.That(_sut.LatestPreviousPaths).IsEqualTo(new [] {"the path"});
  }

  
  [TestCase("BOOSTHEAT.Applications.Training.Timer.dll", "Training Timer")]
  // [TestCase("Demo.Caliper.2023.11.22.1.nupkg", "Caliper")]
  // [TestCase("DeviceCsProj/FooDevice.csproj", "Foo")]
  public void should_load_project(string filename, string expectedTitle)
  {
    var fullFilename = Path.Combine(Environment.CurrentDirectory, "Projects/resources", filename);
    _sut.Load(fullFilename);
    Check.That(_loadedProjects.Count).IsEqualTo(1);
    Check.That(_sut.LatestProject.IsSome).IsTrue();
    Check.That(_loadedProjects.Single().GetValue()).IsEqualTo(_sut.LatestProject.GetValue());
    Check.That(_loadedProjects.Single().GetValue().Path).IsEqualTo(fullFilename);
    Check.That(_loadedDevices.Count).IsEqualTo(1);
    Check.That(_sut.LatestDevice.IsSome).IsTrue();
    Check.That(_loadedDevices.Single().GetValue()).IsEqualTo(_sut.LatestDevice.GetValue());
    Check.That(_loadedDevices.Single().GetValue().Path).IsEqualTo(fullFilename);
    Check.That(_loadedDevices.Single().GetValue().Name).IsEqualTo(expectedTitle);
    Check.That(_previousPaths).IsEqualTo(new [] {fullFilename});
    Check.That(_sut.LatestPreviousPaths).IsEqualTo(new [] {fullFilename});
  }
  
  [TestCase("BOOSTHEAT.Applications.Training.Timer.dll", "Training Timer")]
  // [TestCase("Demo.Caliper.2023.11.22.1.nupkg", "Caliper")]
  // [TestCase("DeviceCsProj/FooDevice.csproj", "Foo")]
  public void should_unload_project(string filename, string expectedTitle)
  {
    var fullFilename = Path.Combine(Environment.CurrentDirectory, "Projects/resources", filename);
    _sut.Load(fullFilename);
    _sut.UnLoad();
    Check.That(_sut.LatestProject.IsNone).IsTrue();
    Check.That(_loadedProjects.Count).IsEqualTo(2);
    Check.That(_loadedProjects.Last()).IsEqualTo(Option<IManageProject>.None());
    Check.That(_sut.LatestDevice.IsNone).IsTrue();
    Check.That(_loadedDevices.Count).IsEqualTo(2);
    Check.That(_loadedDevices.Last()).IsEqualTo(Option<IDeviceDefinition>.None());
  }

  private ProjectsManager _sut;
  private List<Option<IManageProject>> _loadedProjects;
  private List<Option<IDeviceDefinition>> _loadedDevices;
  private List<string> _previousPaths = null!;
}