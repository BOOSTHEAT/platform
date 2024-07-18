using System.Reactive.Disposables;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using Moq;

namespace ImpliciX.DesktopServices.Tests.Services;

public class ApplicationsManagerTests
{
  private ApplicationsManager _sut = null!;
  private IDisposable _sub = null!;
  private List<Option<IDeviceDefinition>> _actualDevices = null!;
  private List<string> _previousPaths = null!;
  private Mock<IConsoleService> _console = null!;
  private Mock<IDeviceDefinitionFactory> _definitionFactory = null!;

  [Test]
  public void should_be_initially_empty()
  {
    Assert.True(_sut.LatestDevice.IsNone);
  }

  [Test]
  public void should_load_definition()
  {
    _sut.Load("some_file");
    Assert.True(_sut.LatestDevice.IsSome);
    Assert.That(_sut.LatestDevice.GetValue().Path, Is.EqualTo("Path[some_file]"));
    Assert.That(_sut.LatestDevice.GetValue().Name, Is.EqualTo("Def[App[some_file]]"));
    Assert.That(_actualDevices.Count, Is.EqualTo(1));
    Assert.That(_actualDevices[0].GetValue().Name, Is.EqualTo("Def[App[some_file]]"));
    Assert.That(_previousPaths, Is.EqualTo(new[] {"some_file"}));
  }

  [Test]
  public void should_unload_definition()
  {
    _sut.Load("some_file");
    _sut.UnLoad();
    Assert.True(_sut.LatestDevice.IsNone);
    Assert.That(_actualDevices.Count, Is.EqualTo(2));
    Assert.That(_actualDevices.Last(), Is.EqualTo(Option<IDeviceDefinition>.None()));
  }

  [Test]
  public void should_load_multiple_definitions()
  {
    _sut.Load("some_file");
    _sut.Load("some_other_file");
    Assert.True(_sut.LatestDevice.IsSome);
    Assert.That(_sut.LatestDevice.GetValue().Path, Is.EqualTo("Path[some_other_file]"));
    Assert.That(_sut.LatestDevice.GetValue().Name, Is.EqualTo("Def[App[some_other_file]]"));
    Assert.That(_actualDevices.Count, Is.EqualTo(2));
    Assert.That(_actualDevices[0].GetValue().Path, Is.EqualTo("Path[some_file]"));
    Assert.That(_actualDevices[1].GetValue().Path, Is.EqualTo("Path[some_other_file]"));
    Assert.That(_actualDevices[0].GetValue().Name, Is.EqualTo("Def[App[some_file]]"));
    Assert.That(_actualDevices[1].GetValue().Name, Is.EqualTo("Def[App[some_other_file]]"));
    Assert.That(_previousPaths, Is.EqualTo(new[] {"some_other_file", "some_file"}));
  }

  [Test]
  public void should_write_to_console()
  {
    _sut.Load("some_file");
    _sut.Load("some_other_file");
    _console.Verify(c => c.WriteLine("Loading some_file"));
    _console.Verify(c => c.WriteLine("Loading some_file complete."));
    _console.Verify(c => c.WriteLine("Loading some_other_file"));
    _console.Verify(c => c.WriteLine("Loading some_other_file complete."));
    _console.VerifyNoOtherCalls();
  }

  [SetUp]
  public void Init()
  {
    _actualDevices = new();
    _previousPaths = new();
    _console = new Mock<IConsoleService>();
    _definitionFactory = new Mock<IDeviceDefinitionFactory>();
    _definitionFactory
      .Setup(x => x.Create(It.IsAny<string>(), It.IsAny<ApplicationDefinition>()))
      .Returns<string,ApplicationDefinition>((path,appDef) =>
      {
        var dd = new Mock<IDeviceDefinition>();
        dd.Setup(y => y.Path).Returns($"Path[{path}]");
        dd.Setup(y => y.Name).Returns($"Def[{appDef.AppName}]");
        return dd.Object;
      });
    UserSettings.Clear(PathHistory.PersistenceKey);
    _sut = new ApplicationsManager(
      _console.Object,
      x => new ApplicationDefinition { AppName = $"App[{x}]" },
      _definitionFactory.Object);
    _sub = new CompositeDisposable(
      _sut.Devices.Subscribe(d => _actualDevices.Add(d)),
      _sut.PreviousPaths.Subscribe(p => _previousPaths = p.ToList())
    );
  }

  [TearDown]
  public void Clean()
  {
    _sub.Dispose();
  }
}