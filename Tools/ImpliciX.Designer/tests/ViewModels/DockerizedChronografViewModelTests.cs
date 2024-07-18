using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using ImpliciX.Designer.ViewModels.Tools;
using ImpliciX.DesktopServices;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

[TestFixture]
public class DockerizedChronografViewModelTests
{
  [Test]
  public void open_url()
  {
    var concierge = new Mock<IConcierge>();
    var connectApp = new Mock<IRemoteDevice>();
    concierge.Setup(x => x.RemoteDevice).Returns(connectApp.Object);
    var user = new Mock<IUser>();
    user.Setup(x => x.Show(It.IsAny<IUser.Box>())).Returns(Task.FromResult(IUser.ChoiceType.Ok));
    concierge.Setup(x => x.User).Returns(user.Object);

    var docker = new Mock<IDockerService>();
    concierge.Setup(x => x.Docker).Returns(docker.Object);

    var os = new Mock<IOperatingSystem>();
    os.Setup(x => x.OpenUrl(It.Is<string>(s => s == "http://127.0.0.1:7710")))
      .Returns(Task.CompletedTask).Verifiable();
    concierge.Setup(x => x.OperatingSystem).Returns(os.Object);

    var sut = new DockerizedChronografViewModel(concierge.Object);

    sut.Open();

    docker.Verify(
      x => x.Pull(
        "chronograf:latest",
        It.IsAny<AuthConfig>()
      )
    );
    docker.Verify(
      x => x.Launch(
        "chronograf:latest",
        "bhChronograf7710",
        false,
        It.Is<IDictionary<string, IList<PortBinding>>>(
          b =>
            b.Count == 1 && b["8888/tcp"].Single().HostIP == "0.0.0.0" && b["8888/tcp"].Single().HostPort == "7710"
        ),
        It.IsAny<IEnumerable<(string, string)>>(),
        It.IsAny<IEnumerable<string>> (),
        It.IsAny<IEnumerable<(string, string)>>()
      )
    );
    os.Verify();
  }

  [Test]
  public void error_message_if_image_cannot_be_pulled()
  {
    var concierge = new Mock<IConcierge>();

    var docker = new Mock<IDockerService>();
    docker.Setup(
      x => x.Pull(
        "chronograf:latest",
        It.IsAny<AuthConfig>()
      )
    ).Throws(new Exception("IT FAILED"));
    concierge.Setup(x => x.Docker).Returns(docker.Object);

    var user = new Mock<IUser>();
    user.Setup(x => x.Show(It.Is<IUser.Box>(b => b.Message.Trim() == "IT FAILED")))
      .Returns(Task.FromResult(IUser.ChoiceType.Ok)).Verifiable();
    concierge.Setup(x => x.User).Returns(user.Object);

    var sut = new DockerizedChronografViewModel(concierge.Object);

    sut.Open();

    docker.Verify(
      x => x.Pull(
        "chronograf:latest",
        It.IsAny<AuthConfig>()
      )
    );
    user.Verify();
  }
}
