using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using Moq;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels;

public class LiveConnectViewModelTests
{
  private Subject<bool> _isConnected;
  private Mock<IRemoteDevice> _remoteDevice;

  private LiveConnectViewModel _sut;
  [SetUp]
  public void Init()
  {
    var concierge = new Mock<ILightConcierge>();
    _isConnected = new Subject<bool>();
    _remoteDevice = new Mock<IRemoteDevice>();
    _remoteDevice.Setup(x => x.IsConnected).Returns(_isConnected);
    concierge.Setup(x => x.RemoteDevice).Returns(_remoteDevice.Object);
    _sut = new LiveConnectViewModel(concierge.Object);
    // _remoteDevice.VerifyAdd(x => x.ConnectionEstablished += It.IsAny<EventHandler<bool>>());
    // _remoteDevice.VerifyAdd(x => x.ConnectionLost += It.IsAny<EventHandler<bool>>());
  }

  [Test]
  public void CannotInitiateConnectionWhenNoConnectionString()
  {
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(false));
  }

  [Test]
  public void CannotInitiateConnectionWhenEmptyConnectionString()
  {
    _sut.ConnectionString = "";
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(false));
  }

  [Test]
  public void CannotInitiateConnectionWhenSpacesConnectionString()
  {
    _sut.ConnectionString = "   ";
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(false));
  }

  [Test]
  public void CanInitiateConnectionWhenConnectionString()
  {
    _sut.ConnectionString = "foo";
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(true));
  }

  [Test]
  public void NotConnectedAtInitialisation()
  {
    Assert.That(_sut.IsConnected, Is.EqualTo(false));
  }

  [Test]
  public async Task DoNotConnectIfCannotInitiateConnection()
  {
    _sut.ConnectionString = " ";
    await _sut.Connect();
    Assert.That(_sut.IsConnected, Is.EqualTo(false));
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(false));
    _remoteDevice.VerifyGet(x => x.IsConnected);
    _remoteDevice.VerifyNoOtherCalls();
  }

  [Test]
  public async Task CanConnectToGivenAddress()
  {
    _remoteDevice
      .Setup(x => x.Connect("foo"))
      .Callback(() => _isConnected.OnNext(true));
    _sut.ConnectionString = "foo";
    await _sut.Connect();
    Assert.That(_sut.IsConnected, Is.EqualTo(true));
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(false));
    _remoteDevice.Verify(x => x.Connect("foo"));
    _remoteDevice.VerifyGet(x => x.IsConnected);
    _remoteDevice.VerifyNoOtherCalls();
  }

  [Test]
  public async Task CanDisconnect()
  {
    _remoteDevice
      .Setup(x => x.Connect("foo"))
      .Callback(() => _isConnected.OnNext(true));
    _remoteDevice
      .Setup(x => x.Disconnect(null))
      .Callback(() => _isConnected.OnNext(false));
    _sut.ConnectionString = "foo";
    await _sut.Connect();
    _sut.Disconnect();
    Assert.That(_sut.IsConnected, Is.EqualTo(false));
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(true));
    _remoteDevice.Verify(x => x.Connect("foo"));
    _remoteDevice.Verify(x => x.Disconnect(null));
    _remoteDevice.VerifyGet(x => x.IsConnected);
    _remoteDevice.VerifyNoOtherCalls();
  }

  [Test]
  public async Task CannotConnectToGivenAddress()
  {
    _remoteDevice
      .Setup(x => x.Connect("foo"))
      .Callback(() => _isConnected.OnNext(false));
    _sut.ConnectionString = "foo";
    await _sut.Connect();
    Assert.That(_sut.IsConnected, Is.EqualTo(false));
    Assert.That(_sut.CanInitiateConnection, Is.EqualTo(true));
    _remoteDevice.Verify(x => x.Connect("foo"));
    _remoteDevice.VerifyGet(x => x.IsConnected);
    _remoteDevice.VerifyNoOtherCalls();
  }

  [Test]
  public async Task CanPopulateWithSuggestions()
  {
    _remoteDevice
      .Setup(x => x.Suggestions("foo"))
      .Returns(ToAsyncEnumerable("fizz", "buzz"));
    _sut.ConnectionString = "foo";
    var populate = (Func<string, CancellationToken, Task<IEnumerable<object>>>)_sut.Populate;
    Assert.That(await populate("foo", default), Is.EqualTo(new[] {"fizz", "buzz"}));
  }

  public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(params T[] items)
  {
    foreach (var item in items)
      yield return await Task.FromResult(item);
  }
}
