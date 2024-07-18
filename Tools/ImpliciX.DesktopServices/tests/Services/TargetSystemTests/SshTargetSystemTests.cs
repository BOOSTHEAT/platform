using ImpliciX.DesktopServices.Services;
using Moq;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services.TargetSystemTests;

public class SshTargetSystemTests : BaseClassForSshTests
{
  [Test]
  public async Task CapabilitiesContainDeviceName()
  {
    StubSshMMIApi(NoCapabilities).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    SftpClient.Verify();
    Check.That(sut.Name).IsEqualTo("BOOSTHEAT MMI Board");
  }

  [Test]
  public async Task CapabilitiesContainSystemInfo()
  {
    StubSshMMIApi(SystemInfo).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    SftpClient.Verify();
    Check.That(sut.SystemInfo).IsEqualTo(
      new SystemInfo(
        OS.linux,
        Architecture.arm,
        "imx7"
      )
    );
  }

  [Test]
  public void InvalidDownloadedCapabilities()
  {
    SftpClient.Setup(x => x.Download("/implicix.json")).Returns(Task.FromResult("")).Verifiable();
    Check
      .ThatAsyncCode(
        () => TargetSystem.Create(
          Concierge.Object,
          SshClientFactory,
          string.Empty,
          _consoleOutputSlice.Object
        )
      )
      .ThrowsAny().WithMessage("Invalid content in /implicix.json");
    SftpClient.Verify();
  }

  [Test]
  public void FailToDownloadCapabilities()
  {
    SftpClient.Setup(x => x.Download("/root/implicix.json")).Throws<Exception>().Verifiable();
    SftpClient.Setup(x => x.Download("/implicix.json")).Throws<Exception>().Verifiable();
    Check
      .ThatAsyncCode(
        () => TargetSystem.Create(
          Concierge.Object,
          SshClientFactory,
          string.Empty,
          _consoleOutputSlice.Object
        )
      )
      .ThrowsAny().WithMessage(
        "Failed to download capabilities, neither /root/implicix.json nor /implicix.json can be download"
      );
    SftpClient.Verify();
  }

  [Test]
  public async Task DownloadSystemCheck_Successful()
  {
    StubSshMMIApi(JournalOnlyCapabilities);
    SshClient.Setup(
        x => x.Execute(
          "executing SystemJournalBackup",
          Path.Combine(
            "the_folder",
            "log.txt.gz"
          )
        )
      )
      .Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.SystemCheck.Execute().AndSaveTo("the_folder");
    SshClient.Verify();
  }

  [Test]
  public async Task DownloadSystemCheck_Failure()
  {
    StubSshMMIApi(JournalOnlyCapabilities);
    SshClient.Setup(
        x => x.Execute(
          "executing SystemJournalBackup",
          Path.Combine(
            "the_folder",
            "log.txt.gz"
          )
        )
      )
      .Returns(Task.FromException(new Exception("whatever")))
      .Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    Check
      .ThatAsyncCode(() => sut.SystemCheck.Execute().AndSaveTo("the_folder"))
      .ThrowsAny().WithMessage("whatever");
    SshClient.Verify();
  }

  [Test]
  public async Task DownloadSystemCheck_IsAvailableWithoutAnyCapabilities_BecauseWeGetAtLeastTheConsoleOutput()
  {
    StubSshMMIApi(NoCapabilities);
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.SystemCheck.Execute().AndSaveTo("the_folder");
    _consoleOutputSlice.Verify(
      x => x.DumpInto(
        Path.Combine(
          "the_folder",
          "console.txt"
        )
      )
    );
  }

  [Test]
  public async Task FixAppConnection_AcceptFactoryResetConfirmation()
  {
    StubSshMMIApi(FullCapabilities);
    User.Setup(x => x.Show(It.Is<IUser.Box>(b => b.Title == "Connection failed")))
      .Returns(Task.FromResult(IUser.ChoiceType.Yes)).Verifiable();
    SshClient.Setup(x => x.Execute("executing ResetToFactorySoftware"))
      .Returns(Task.FromResult("Board reset OK"))
      .Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    SshClient.Verify();
    RemoteDevice.Verify();
    User.Verify();
    Check.That(ConsoleOutput).IsEqualTo(
      new []
      {
        "Board reset OK"
      }
    );
  }

  [Test]
  public async Task FixAppConnection_DownloadSystemCheck()
  {
    StubSshMMIApi(FullCapabilities);
    User.Setup(
        x => x.Show(
          It.Is<IUser.Box>(
            b =>
              b.Title == "Connection failed"
              && b.Buttons.Select(c => c.Type).Is(IUser.ChoiceType.Yes | IUser.ChoiceType.No | IUser.ChoiceType.Custom1)
          )
        )
      )
      .Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    User.Setup( x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Ok, "the_folder"))).Verifiable();
    SshClient.Setup(
        x => x.Execute(
          "executing SystemJournalBackup",
          Path.Combine(
            "the_folder",
            "log.txt.gz"
          )
        )
      )
      .Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    User.Verify();
    SshClient.Verify();
    RemoteDevice.Verify();
    _consoleOutputSlice.Verify(
      x => x.DumpInto(
        Path.Combine(
          "the_folder",
          "console.txt"
        )
      )
    );
  }

  [Test]
  public async Task FixAppConnection_NoJournalCapability_DownloadSystemCheckWithConsoleOnly()
  {
    StubSshMMIApi(ResetOnlyCapabilities);
    User.Setup(
        x => x.Show(
          It.Is<IUser.Box>(
            b =>
              b.Title == "Connection failed"
              && b.Buttons.Select(c => c.Type).Is(IUser.ChoiceType.Yes | IUser.ChoiceType.No | IUser.ChoiceType.Custom1)
          )
        )
      )
      .Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    User.Setup( x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Ok, "the_folder"))).Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    User.Verify();
    SshClient.Verify();
    RemoteDevice.Verify();
    _consoleOutputSlice.Verify(
      x => x.DumpInto(
        Path.Combine(
          "the_folder",
          "console.txt"
        )
      )
    );
  }

  [Test]
  public async Task FixAppConnection_DownloadSystemCheckButCancelFolderSelection()
  {
    StubSshMMIApi(FullCapabilities);
    User.Setup(
        x => x.Show(
          It.Is<IUser.Box>(
            b =>
              b.Title == "Connection failed"
              && b.Buttons.Select(c => c.Type).Is(IUser.ChoiceType.Yes | IUser.ChoiceType.No | IUser.ChoiceType.Custom1)
          )
        )
      )
      .Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    User.Setup( x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Cancel, ""))).Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    User.Verify();
    SshClient.VerifyNoOtherCalls();
    RemoteDevice.Verify();
  }

  [Test]
  public async Task FixAppConnection_CannotFactoryResetAfterConfirmation()
  {
    StubSshMMIApi(FullCapabilities);
    User.Setup(x => x.Show(It.Is<IUser.Box>(b => b.Title == "Connection failed")))
      .Returns(Task.FromResult(IUser.ChoiceType.Yes)).Verifiable();
    SshClient.Setup(x => x.Execute("executing ResetToFactorySoftware"))
      .Returns(Task.FromException<string>(new Exception("Client not connected.")));
    RemoteDevice
      .Setup(x => x.Disconnect(It.Is<Exception>(e => e.Message == "Client not connected.")))
      .Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    RemoteDevice.Verify();
  }

  [Test]
  public async Task FixAppConnection_RefuseFactoryResetConfirmation()
  {
    StubSshMMIApi(FullCapabilities);
    User.Setup(x => x.Show(It.Is<IUser.Box>(b => b.Title == "Connection failed")))
      .Returns(Task.FromResult(IUser.ChoiceType.No)).Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    RemoteDevice.Verify();
    User.Verify();
  }

  [Test]
  public async Task FixAppConnection_NoFactoryResetCapability()
  {
    StubSshMMIApi(NoCapabilities);
    User.Setup(x => x.Show(It.Is<IUser.Box>(b => b.Title == "Connection failed")))
      .Returns(Task.FromResult(IUser.ChoiceType.Ok)).Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    RemoteDevice.Verify();
    User.Verify();
  }

  [Test]
  public async Task FixAppConnection_NoFactoryResetCapability_DownloadSystemCheck()
  {
    StubSshMMIApi(JournalOnlyCapabilities);
    User.Setup(
        x => x.Show(
          It.Is<IUser.Box>(
            b =>
              b.Title == "Connection failed"
              && b.Buttons.Select(c => c.Type).Is(IUser.ChoiceType.Ok | IUser.ChoiceType.Custom1)
          )
        )
      )
      .Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    User.Setup( x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Ok, "the_folder"))).Verifiable();
    SshClient.Setup(
        x => x.Execute(
          "executing SystemJournalBackup",
          Path.Combine(
            "the_folder",
            "log.txt.gz"
          )
        )
      )
      .Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    User.Verify();
    SshClient.Verify();
    RemoteDevice.Verify();
    _consoleOutputSlice.Verify(
      x => x.DumpInto(
        Path.Combine(
          "the_folder",
          "console.txt"
        )
      )
    );
  }

  [Test]
  public async Task FixAppConnection_NoFactoryResetCapability_NoJournalCapability_DownloadSystemCheckWithConsoleOnly()
  {
    StubSshMMIApi(NoCapabilities);
    User.Setup(
        x => x.Show(
          It.Is<IUser.Box>(
            b =>
              b.Title == "Connection failed"
              && b.Buttons.Select(c => c.Type).Is(IUser.ChoiceType.Ok | IUser.ChoiceType.Custom1)
          )
        )
      )
      .Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    User.Setup( x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Ok, "the_folder"))).Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    User.Verify();
    SshClient.Verify();
    RemoteDevice.Verify();
    _consoleOutputSlice.Verify(
      x => x.DumpInto(
        Path.Combine(
          "the_folder",
          "console.txt"
        )
      )
    );
  }

  [Test]
  public async Task FixAppConnection_NoFactoryResetCapability_DownloadSystemCheckButCancelFolderSelection()
  {
    StubSshMMIApi(JournalOnlyCapabilities);
    User.Setup(
        x => x.Show(
          It.Is<IUser.Box>(
            b =>
              b.Title == "Connection failed"
              && b.Buttons.Select(c => c.Type).Is(IUser.ChoiceType.Ok | IUser.ChoiceType.Custom1)
          )
        )
      )
      .Returns(Task.FromResult(IUser.ChoiceType.Custom1)).Verifiable();
    User.Setup( x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Cancel, ""))).Verifiable();
    RemoteDevice.Setup(x => x.Disconnect(null)).Verifiable();
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      _consoleOutputSlice.Object
    );
    await sut.FixAppConnection();
    User.Verify();
    SshClient.VerifyNoOtherCalls();
    RemoteDevice.Verify();
  }
}
