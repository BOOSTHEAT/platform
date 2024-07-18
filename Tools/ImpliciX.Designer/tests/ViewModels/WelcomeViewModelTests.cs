using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using ImpliciX.Designer.Features;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class WelcomeViewModelTests
{
  private readonly Subject<IEnumerable<ISessionService.Session>> _history = new ();
  private WelcomeViewModel _sut = null!;

  [SetUp]
  public void Init()
  {
    var sessionMock = new Mock<ISessionService>();
    sessionMock.Setup(x => x.HistoryUpdates).Returns(_history);
    var conciergeMock = new Mock<ILightConcierge>();
    conciergeMock.Setup(x => x.Session).Returns(sessionMock.Object);
    var featuresMock = new Mock<IFeatures>();
    featuresMock.Setup(x => x.Concierge).Returns(conciergeMock.Object);
    _sut = new WelcomeViewModel(featuresMock.Object);
  }

  [Test]
  public void ListOfDeviceDefinitionPaths()
  {
    _history.OnNext(
      new ISessionService.Session[]
      {
        new (
          "/a/foo.nupkg",
          "here"
        ),
        new (
          "/a/foo.nupkg",
          "there"
        ),
        new (
          "/b/bar.csproj",
          ""
        ),
        new (
          "",
          "somewhere"
        ),
        new (
          "",
          ""
        )
      }
    );
    var res = _sut.DeviceDefinitionPaths;
    Assert.That(
      res.Select(x => x.Text),
      Is.EqualTo(
        new[]
        {
          "load foo and reconnect to here",
          "load foo and reconnect to there",
          "load bar",
          "reconnect to somewhere"
        }
      )
    );
  }
}
