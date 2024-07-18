using System.Globalization;
using DynamicData;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.DesktopServices.Services;
using ImpliciX.DesktopServices.Services.SshInfrastructure;
using ImpliciX.DesktopServices.Services.WebsocketInfrastructure;
using Moq;
using ReactiveUI;

namespace ImpliciX.DesktopServices.Tests.Services;

public class RemoteDeviceTests
{
  private const string MyPublicKey = "SomePublicKey";
  private Mock<ITargetSystem> _connectedDevice;
  private List<Exception> _consoleErrors;
  private List<string> _consoleOutput;
  private Mock<ISshIdentity> _identity;
  private bool _isConnected;
  private Mock<ISshAdapter> _sshAdapter;

  private IRemoteDevice _sut;
  private Mock<IWebsocketAdapter> _wsAdapter;
  private CultureInfo originalCulture;
  [Test]
  public void NoSshConnectionToLoopback()
  {
    var wsClient = new Mock<IWebsocketClient>();
    _wsAdapter.Setup(x => x.CreateClient(It.IsAny<int>(), "127.0.0.1", RemoteDevice.WebSocketLocalPort))
      .Returns(wsClient.Object);
    wsClient.Setup(x => x.Start()).Verifiable();
    _sut.Connect("127.0.0.1");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to 127.0.0.1",
      $"Connected on port {RemoteDevice.WebSocketLocalPort}"
    }));
    Assert.That(_consoleErrors, Is.Empty);
    Assert.True(_isConnected);
    wsClient.Verify();
    var expectedTargetSystem = new LoopbackTargetSystem("127.0.0.1");
    Assert.That(_sut.CurrentTargetSystem.Name, Is.EqualTo(expectedTargetSystem.Name));
    Assert.That(_sut.CurrentTargetSystem.SystemInfo.Os, Is.EqualTo(expectedTargetSystem.SystemInfo.Os));
    Assert.That(_sut.CurrentTargetSystem.SystemInfo.Architecture,
      Is.EqualTo(expectedTargetSystem.SystemInfo.Architecture));
  }

  [Test]
  public void NoSshConnectionToDockerContainer()
  {
    var wsClient = new Mock<IWebsocketClient>();
    _wsAdapter.Setup(x => x.CreateClient(It.IsAny<int>(), "172.3.2.1", RemoteDevice.WebSocketLocalPort))
      .Returns(wsClient.Object);
    wsClient.Setup(x => x.Start()).Verifiable();
    _sut.Connect("172.3.2.1");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to 172.3.2.1",
      $"Connected on port {RemoteDevice.WebSocketLocalPort}"
    }));
    Assert.That(_consoleErrors, Is.Empty);
    Assert.True(_isConnected);
    wsClient.Verify();
    var expectedTargetSystem = new LoopbackTargetSystem("172.3.2.1");
    Assert.That(_sut.CurrentTargetSystem.Name, Is.EqualTo(expectedTargetSystem.Name));
    Assert.That(_sut.CurrentTargetSystem.SystemInfo.Os, Is.EqualTo(expectedTargetSystem.SystemInfo.Os));
    Assert.That(_sut.CurrentTargetSystem.SystemInfo.Architecture,
      Is.EqualTo(expectedTargetSystem.SystemInfo.Architecture));
  }

  [Test]
  public void SshConnectionToIpAddress()
  {
    var sshClient = new Mock<ISshClient>();
    _sshAdapter.Setup(x => x.CreateClient("10.33.45.98", 22, "root", _identity.Object)).Returns(sshClient.Object);
    var wsClient = new Mock<IWebsocketClient>();
    _wsAdapter.Setup(x => x.CreateClient(It.IsAny<int>(), "127.0.0.1", RemoteDevice.WebSocketLocalPort))
      .Returns(wsClient.Object);
    wsClient.Setup(x => x.Start()).Verifiable();
    _connectedDevice.Setup(x => x.Address).Returns("127.0.0.1");
    _sut.Connect("10.33.45.98");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to 10.33.45.98",
      "Setting up Direct SSH Connection to 10.33.45.98",
      $"Connecting with identity {MyPublicKey}",
      "Direct SSH Connection setup complete",
      $"Connected on port {RemoteDevice.WebSocketLocalPort}"
    }));
    Assert.That(_consoleErrors, Is.Empty);
    Assert.True(_isConnected);
    wsClient.Verify();
  }

  [Test]
  public void SshConnectionToPingableHostname()
  {
    _sshAdapter.Setup(x => x.IsPingable("myboard")).Returns(Task.FromResult(true));
    var sshClient = new Mock<ISshClient>();
    _sshAdapter.Setup(x => x.CreateClient("myboard", 22, "root", _identity.Object)).Returns(sshClient.Object);
    var wsClient = new Mock<IWebsocketClient>();
    _wsAdapter.Setup(x => x.CreateClient(It.IsAny<int>(), "127.0.0.1", RemoteDevice.WebSocketLocalPort))
      .Returns(wsClient.Object);
    wsClient.Setup(x => x.Start()).Verifiable();
    _connectedDevice.Setup(x => x.Address).Returns("127.0.0.1");
    _sut.Connect("myboard");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to myboard",
      "Ping success for hostname myboard",
      "Setting up Direct SSH Connection to myboard",
      $"Connecting with identity {MyPublicKey}",
      "Direct SSH Connection setup complete",
      $"Connected on port {RemoteDevice.WebSocketLocalPort}"
    }));
    Assert.That(_consoleErrors, Is.Empty);
    Assert.True(_isConnected);
    wsClient.Verify();
  }

  [Test]
  public void SshConnectionToUnpingableHostname()
  {
    _sshAdapter.Setup(x => x.IsPingable("myboard")).Returns(Task.FromResult(false));
    var proxyJump = new Mock<ISshClient>();
    _sshAdapter.Setup(x => x.CreateClient("remotemoe.boostheat.org", 2222, "whatever", _identity.Object))
      .Returns(proxyJump.Object);
    proxyJump.Setup(x => x.ForwardPort("127.0.0.1", 8222, "myboard.vm-remotemoe-prod", 22)).Verifiable();
    var sshClient = new Mock<ISshClient>();
    _sshAdapter.Setup(x => x.CreateClient("127.0.0.1", 8222, "root", _identity.Object)).Returns(sshClient.Object);
    var wsClient = new Mock<IWebsocketClient>();
    _wsAdapter.Setup(x => x.CreateClient(It.IsAny<int>(), "127.0.0.1", RemoteDevice.WebSocketLocalPort))
      .Returns(wsClient.Object);
    wsClient.Setup(x => x.Start()).Verifiable();
    _connectedDevice.Setup(x => x.Address).Returns("127.0.0.1");
    _sut.Connect("myboard");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to myboard",
      "Setting up Proxified SSH Connection to myboard.vm-remotemoe-prod through remotemoe.boostheat.org",
      $"Connecting with identity {MyPublicKey}",
      "Proxified SSH Connection setup complete",
      $"Connected on port {RemoteDevice.WebSocketLocalPort}"
    }));
    Assert.That(_consoleErrors, Is.Empty);
    Assert.True(_isConnected);
    proxyJump.Verify();
    wsClient.Verify();
  }

  [Test]
  public void FailToSshConnect()
  {
    _sshAdapter.Setup(x => x.CreateClient("10.33.45.98", 22, "root", _identity.Object))
      .Throws(new Exception("Cannot connect ssh"));
    _sut.Connect("10.33.45.98");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to 10.33.45.98",
      "Setting up Direct SSH Connection to 10.33.45.98",
      $"Connecting with identity {MyPublicKey}",
      "Disconnected from 10.33.45.98\n",
      "--------------------------------",
    }));
    Assert.That(_consoleErrors.Select(e => e.Message), Is.EqualTo(new[]
    {
      "Cannot connect ssh"
    }));
    Assert.False(_isConnected);
  }

  [Test]
  public void FailToConnectToWebsocketAfterSshConnection()
  {
    var sshClient = new Mock<ISshClient>();
    _sshAdapter.Setup(x => x.CreateClient("10.33.45.98", 22, "root", _identity.Object)).Returns(sshClient.Object);
    var wsClient = new Mock<IWebsocketClient>();
    _wsAdapter.Setup(x => x.CreateClient(It.IsAny<int>(), "127.0.0.1", RemoteDevice.WebSocketLocalPort))
      .Returns(wsClient.Object);
    wsClient.Setup(x => x.Start()).Throws(new WebsocketConnectionFailure("Fail to connect to websocket"));
    _connectedDevice.Setup(x => x.Address).Returns("127.0.0.1");
    _connectedDevice.Setup(x => x.FixAppConnection()).Verifiable();
    _sut.Connect("10.33.45.98");
    Assert.That(_consoleOutput, Is.EqualTo(new[]
    {
      "Connecting to 10.33.45.98",
      "Setting up Direct SSH Connection to 10.33.45.98",
      $"Connecting with identity {MyPublicKey}",
      "Direct SSH Connection setup complete",
    }));
    Assert.That(_consoleErrors, Is.Empty);
    Assert.False(_isConnected);
    _connectedDevice.Verify();
  }

  [Test]
  public void UpdateDeviceDefinitionFromJson()
  {
    var json =
      """{"$type":"prelude","Name":"yolo","Version":"1.2.3.4","Setup":"foo","Setups":["foo","bar"],"Kind":"prelude"}""";
    var dd = new Mock<IObserver<IRemoteDeviceDefinition>>();
    RemoteDevice.UpdateFromJson(json, dd.Object, null, null);
    dd.Verify(x => x.OnNext(It.Is<IRemoteDeviceDefinition>(def =>
      def.Name == "yolo"
      && def.Version == "1.2.3.4"
      && def.Setup == "foo"
      && def.Setups.Length == 2
      && def.Setups[0] == "foo"
      && def.Setups[1] == "bar"
    )));
  }

  [Test]
  public void UpdatePropertiesFromJson_ShouldUpdatePropertiesAndLogWhenNull()
  {
    var json =
      """{"$type":"properties","Properties":[{"Urn":"analytics:production:auxiliary:public_state:Other:duration","Value":null,"At":"04:00:00.639"}],"Kind":"properties"}""";
    var sc = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    var logMessages = new List<string>();
    Action<string> log = logMessages.Add;
    RemoteDevice.UpdateFromJson(json, null, sc, log);
    Assert.That(logMessages, Has.Some.Contains("Unexpected null value for"));
  }


  [Test]
  public void UpdatePropertiesFromJson_ShouldUpdateProperties_WhenCultureIsUs()
  {
    CultureInfo.CurrentCulture = new CultureInfo("en_US");
    var json =
      """{"$type":"properties","Properties":[{"Urn":"analytics:production:auxiliary:public_state:Other:duration","Value":0.5,"At":"04:00:00.639"}],"Kind":"properties"}""";
    var sc = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    var logMessages = new List<string>();
    Action<string> log = logMessages.Add;
    RemoteDevice.UpdateFromJson(json, null, sc, log);
    Assert.That(sc.Count, Is.EqualTo(1));
    Assert.That(sc.Items.First().Value, Is.EqualTo("0.5"));
  }

  [Test]
  public void UpdatePropertiesFromJson_ShouldUpdateProperties_WhenCultureIsFR()
  {
    CultureInfo.CurrentCulture = new CultureInfo("fr_FR");
    var json =
      """{"$type":"properties","Properties":[{"Urn":"analytics:production:auxiliary:public_state:Other:duration","Value":0.5,"At":"04:00:00.639"}],"Kind":"properties"}""";
    var sc = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    var logMessages = new List<string>();
    Action<string> log = logMessages.Add;
    RemoteDevice.UpdateFromJson(json, null, sc, log);
    Assert.That(sc.Count, Is.EqualTo(1));
    Assert.That(sc.Items.First().Value, Is.EqualTo("0.5"));
  }

  [Test]
  public void SuggestFromSuccessfulPastConnections()
  {
    var sshClient = new Mock<ISshClient>();
    _sshAdapter.Setup(x =>
        x.CreateClient(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), _identity.Object))
      .Returns(sshClient.Object);
    _sut.Connect("buzz");
    _sut.Connect("fizz");
    _sut.Connect("foo");
    Assert.That(_sut.Suggestions("f").ToBlockingEnumerable(),
      Is.EqualTo(new[] {"foo", "fizz"}));
    Assert.That(_sut.Suggestions("zz").ToBlockingEnumerable(),
      Is.EqualTo(new[] {"fizz", "buzz"}));
  }

  [SetUp]
  public void Init()
  {
    _consoleOutput = new List<string>();
    _consoleErrors = new List<Exception>();
    var console = new ConsoleService();
    console.LineWritten += (sender, s) => _consoleOutput.Add(s);
    console.Errors += (sender, e) => _consoleErrors.Add(e);
    _sshAdapter = new Mock<ISshAdapter>();
    _identity = new Mock<ISshIdentity>();
    _identity.Setup(x => x.PublicKey).Returns(MyPublicKey);
    _sshAdapter.Setup(x => x.LoadIdentity(It.IsAny<IIdentity>())).Returns(Task.FromResult(_identity.Object));
    _wsAdapter = new Mock<IWebsocketAdapter>();
    _connectedDevice = new Mock<ITargetSystem>();
    UserSettings.Clear(RemoteDeviceHistory.PersistenceKey);
    _sut = new RemoteDevice(console, null, _sshAdapter.Object, _wsAdapter.Object,
      (_, _, _) => Task.FromResult(_connectedDevice.Object));
    _sut.IsConnected.BindTo(this, x => x._isConnected);
    _isConnected = false;
    originalCulture = CultureInfo.CurrentCulture;
  }

  [TearDown]
  public void Cleanup()
  {
    CultureInfo.CurrentCulture = originalCulture;
  }
}
