using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using ImpliciX.Designer.ViewModels.Tools;
using ImpliciX.DesktopServices;
using Moq;
using NuGet.Protocol;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

[TestFixture]
public class DockerizedGrafanaViewModelTests
{
  private DockerizedGrafanaViewModel sut;

  [Test]
  public void error_message_if_image_cannot_be_pulled()
  {
    var concierge = new Mock<IConcierge>();

    var docker = new Mock<IDockerService>();
    docker.Setup(
      x => x.Pull(
        "docker.io/grafana/grafana-oss:latest",
        It.IsAny<AuthConfig>()
      )
    ).Throws(new Exception("IT FAILED"));
    concierge.Setup(x => x.Docker).Returns(docker.Object);

    var user = new Mock<IUser>();
    user.Setup(x => x.Show(It.Is<IUser.Box>(b => b.Message.Trim() == "IT FAILED")))
      .Returns(Task.FromResult(IUser.ChoiceType.Ok)).Verifiable();
    concierge.Setup(x => x.User).Returns(user.Object);

    sut = new DockerizedGrafanaViewModel(concierge.Object);

    sut.Open();

    docker.Verify(
      x => x.Pull(
        "docker.io/grafana/grafana-oss:latest",
        It.IsAny<AuthConfig>()
      )
    );
    user.Verify();
  }

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
    os.Setup(x => x.OpenUrl(It.Is<string>(s => s == "http://127.0.0.1:7714")))
      .Returns(Task.CompletedTask).Verifiable();
    concierge.Setup(x => x.OperatingSystem).Returns(os.Object);

    sut = new DockerizedGrafanaViewModel(concierge.Object);

    sut.Open();

    docker.Verify(
      x => x.Pull(
        "docker.io/grafana/grafana-oss:latest",
        It.IsAny<AuthConfig>()
      )
    );

    var imageName = "docker.io/grafana/grafana-oss:latest";
    var containerName = "bhGrafana7714";
    docker.Verify(
      _ => _.Launch(
        imageName,
        containerName,
        false,
        It.Is<IDictionary<string, IList<PortBinding>>>(pbs => AssertGoodPortBinding(pbs)) ,
        It.Is<IEnumerable<(string, string)>>(
          b => assertGoodBinding(
            b,
            Directory.GetParent(DockerizedGrafanaViewModel.GrafanaDatasourcesFilePath).FullName
          )
        ) ,
        null,
        It.Is<IEnumerable<(string, string)>>(e => assertGoodEnvironnements(e))
      )
    );
    os.Verify();
    user.Verify(_ => _.Show(It.Is<IUser.Box>(box => verrifyBox(box))));
  }

  private bool verrifyBox(
    IUser.Box box
  )
  {
    Assert.AreEqual(
      "Grafana",
      box.Title
    );
    Assert.AreEqual(
      1,
      box.Buttons.Count()
    );
    Assert.AreEqual(
      IUser.Icon.Info,
      box.Icon
    );
    Assert.True(
      box.Message.StartsWith(
        "Grafana is running inside a Docker container.\nA web page will now open at the following web address"
      )
    );
    return true;
  }

  private bool assertGoodStartingParameters(
    CreateContainerParameters c,
    Dictionary<string, object> e
  )
  {
    Assert.AreEqual(
      e["Image"],
      c.Image
    );
    Assert.AreEqual(
      e["Name"],
      c.Name
    );
    Assert.AreEqual(
      e["Env"],
      c.Env
    );
    Assert.AreEqual(
      e["ExposedPorts"],
      c.ExposedPorts
    );

    var expectedHostConfig = (Dictionary<string, object>)e["HostConfig"];
    var hostConfig = c.HostConfig;
    Assert.AreEqual(
      expectedHostConfig["PortBindings"].ToJson(),
      hostConfig.PortBindings.ToJson()
    );
    Assert.AreEqual(
      expectedHostConfig["AutoRemove"],
      hostConfig.AutoRemove
    );
    Assert.AreEqual(
      expectedHostConfig["Binds"],
      hostConfig.Binds
    );
    return true;
  }

  private bool assertGoodEnvironnements(
    IEnumerable<(string, string)> environments
  )
  {
    Assert.AreEqual(
      3,
      environments.Count()
    );
    var environmentsDictionary = environments.ToDictionary(
      tuple => tuple.Item1,
      tuple => tuple.Item2
    );
    Assert.True(environmentsDictionary.ContainsKey("GF_AUTH_ANONYMOUS_ORG_ROLE"));
    Assert.True(environmentsDictionary.ContainsKey("GF_AUTH_ANONYMOUS_ENABLED"));
    Assert.True(environmentsDictionary.ContainsKey("GF_INSTALL_PLUGINS"));
    Assert.AreEqual(
      "Admin",
      environmentsDictionary["GF_AUTH_ANONYMOUS_ORG_ROLE"]
    );
    Assert.AreEqual(
      "true",
      environmentsDictionary["GF_AUTH_ANONYMOUS_ENABLED"]
    );
    Assert.AreEqual(
      "grafana-clock-panel, grafana-influxdb-flux-datasource, simpod-json-datasource",
      environmentsDictionary["GF_INSTALL_PLUGINS"]
    );
    return true;
  }

  private bool assertGoodBinding(
    IEnumerable<(string, string)> bindings,
    string tmp
  )
  {
    Assert.AreEqual(
      1,
      bindings.Count()
    );
    var binding = bindings.Single();
    var localPath = Path.Combine(
      tmp,
      "grafana-datasources.yml"
    ) ;
    Assert.AreEqual(
      ("/etc/grafana/provisioning/datasources/datasource.yml:ro", localPath),
      binding
    );
    return true;
  }

  private bool AssertGoodPortBinding(
    IDictionary<string, IList<PortBinding>> pbs
  )
  {
    Assert.AreEqual(
      1,
      pbs.Count
    );
    var bs = pbs["3000/tcp"];
    Assert.AreEqual(
      1,
      bs.Count
    );
    var pb = bs.Single();
    Assert.AreEqual(
      "0.0.0.0",
      pb.HostIP
    );
    Assert.AreEqual(
      "7714",
      pb.HostPort
    );
    return true;
  }
  
}
