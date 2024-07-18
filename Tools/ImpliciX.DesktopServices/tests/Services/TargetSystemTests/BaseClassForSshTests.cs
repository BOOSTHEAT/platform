using ImpliciX.DesktopServices.Services;
using ImpliciX.DesktopServices.Services.SshInfrastructure;
using Moq;
using Moq.Language.Flow;

namespace ImpliciX.DesktopServices.Tests.Services.TargetSystemTests;

public class BaseClassForSshTests
{
  protected const string NoCapabilities = @"{ ""Name"" : ""BOOSTHEAT MMI Board"" }";

  protected const string FullCapabilities = @"{
  ""Name"" : ""BOOSTHEAT MMI Board"",
  ""Capabilities"" : {
    ""ResetToFactorySoftware"" : ""executing ResetToFactorySoftware"",
    ""InfluxDbBackup"" : ""executing InfluxDbBackup"",
    ""SystemJournalBackup"" : ""executing SystemJournalBackup"",
    ""SystemHistoryBackup"" : ""executing SystemHistoryBackup"",
    ""SettingsReset"" : ""executing SettingsReset"",
    ""SystemReboot"" : ""executing SystemReboot"",
    ""NewSetup"" : ""executing NewSetup $1 $2"",
    ""NewTemporarySetup"" : ""executing NewTemporarySetup $1 $2""
  }
}
";

  protected const string ResetOnlyCapabilities = @"{
  ""Name"" : ""BOOSTHEAT MMI Board"",
  ""Capabilities"" : {
    ""ResetToFactorySoftware"" : ""executing ResetToFactorySoftware""
  }
}
";

  protected const string JournalOnlyCapabilities = @"{
  ""Name"" : ""BOOSTHEAT MMI Board"",
  ""Capabilities"" : {
    ""SystemJournalBackup"" : ""executing SystemJournalBackup""
  }
}
";

  protected const string SystemInfo = @"{
    ""Name"":""BOOSTHEAT MMI Board"",
    ""SystemInfo"":{
        ""Os"":""Linux"",
        ""Architecture"":""ARM"",
        ""Hardware"":""imx7""
    }
}";

  protected Mock<IConsoleOutputSlice> _consoleOutputSlice;
  public Mock<ILightConcierge> Concierge;
  public List<string> ConsoleOutput;
  protected Mock<IRemoteDevice> RemoteDevice;
  internal Mock<ISftpClient> SftpClient;
  internal Mock<ISshClient> SshClient;
  internal SshClientFactory SshClientFactory;
  protected Mock<IUser> User;

  internal IReturnsResult<ISftpClient> StubSshMMIApi(
    string capabilities
  )
  {
    return SftpClient.Setup(x => x.Download("/implicix.json")).Returns(Task.FromResult(capabilities));
  }

  internal IReturnsResult<ISftpClient> StubSshKEPApi(
    string capabilities
  )
  {
    return SftpClient.Setup(x => x.Download("/root/implicix.json")).Returns(Task.FromResult(capabilities));
  }

  [SetUp]
  public void Init()
  {
    Concierge = new Mock<ILightConcierge>();
    Environment.SetEnvironmentVariable(
      "IMPLICIX_BOARD_CAPABILITIES",
      "1"
    );
    Concierge.Setup(x => x.RuntimeFlags).Returns(new RuntimeFlags());
    SshClient = new Mock<ISshClient>();
    SftpClient = new Mock<ISftpClient>();
    SftpClient.Setup(x => x.Download("/root/implicix.json")).Throws<Exception>().Verifiable();
    SshClientFactory = new SshClientFactory(
      () => Task.FromResult(SshClient.Object),
      () => Task.FromResult(SftpClient.Object)
    );
    RemoteDevice = new Mock<IRemoteDevice>();
    Concierge.Setup(x => x.RemoteDevice).Returns(RemoteDevice.Object);
    ConsoleOutput = new List<string>();
    var console = new ConsoleService();
    console.LineWritten += (
      sender,
      s
    ) => ConsoleOutput.Add(s);
    Concierge.Setup(x => x.Console).Returns(console);
    User = new Mock<IUser>();
    Concierge.Setup(x => x.User).Returns(User.Object);
    _consoleOutputSlice = new Mock<IConsoleOutputSlice>();
  }
}
