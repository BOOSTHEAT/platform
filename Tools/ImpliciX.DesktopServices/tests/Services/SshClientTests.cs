using ImpliciX.DesktopServices.Services.SshInfrastructure;
using Moq;

namespace ImpliciX.DesktopServices.Tests.Services;

public class SshClientTests
{
  [Test]
  public async Task GivenNoChecksumSentByRemote_ExecuteMany()
  {
    var sshClient = new Mock<ISshClient>();
    sshClient.Setup(x => x.Execute("TheSource")).Returns(
      Task.FromResult("A\tcmdA\nB\tcmdB\nC\tcmdC\n")
    ).Verifiable();

    sshClient.Setup(
      x => x.Execute("cmdA", Path.Combine("TheDestinationFolder", "A"))
    ).Verifiable();

    sshClient.Setup(
      x => x.Execute("cmdB", Path.Combine("TheDestinationFolder", "B"))
    ).Verifiable();

    sshClient.Setup(
      x => x.Execute("cmdC", Path.Combine("TheDestinationFolder", "C"))
    ).Verifiable();

    var actualResult = new List<string>();
    await foreach (var (count, length, name, checksum) in
                   sshClient.Object.ExecuteMany("TheSource", "TheDestinationFolder"))
    {
      actualResult.Add($"{count}/{length} {name} {checksum}".Trim());
    }

    Assert.That(actualResult, Is.EqualTo(new[]
    {
      "1/3 A",
      "2/3 B",
      "3/3 C"
    }));

    sshClient.Verify();
  }

  [Test]
  public async Task GivenChecksumSentByRemote_ExecuteMany()
  {
    var sshClient = new Mock<ISshClient>();

    const string cmdResultFromRemote =
      $"123{SshClientExtensions.HEADER_ELEMENT_SEPARATOR}A{SshClientExtensions.HEADER_COMMAND_SEPARATOR}cmdA\n" +
      $"456{SshClientExtensions.HEADER_ELEMENT_SEPARATOR}B{SshClientExtensions.HEADER_COMMAND_SEPARATOR}cmdB\n" +
      $"789{SshClientExtensions.HEADER_ELEMENT_SEPARATOR}C{SshClientExtensions.HEADER_COMMAND_SEPARATOR}cmdC\n";

    sshClient.Setup(x => x.Execute("TheSource")).Returns(
      Task.FromResult($"{cmdResultFromRemote}\n")
    ).Verifiable();

    sshClient.Setup(
      x => x.Execute("cmdA", Path.Combine("TheDestinationFolder", "A"))
    ).Verifiable();

    sshClient.Setup(
      x => x.Execute("cmdB", Path.Combine("TheDestinationFolder", "B"))
    ).Verifiable();

    sshClient.Setup(
      x => x.Execute("cmdC", Path.Combine("TheDestinationFolder", "C"))
    ).Verifiable();

    var actualResult = new List<string>();
    await foreach (var (count, length, name, checksum) in
                   sshClient.Object.ExecuteMany("TheSource", "TheDestinationFolder"))
    {
      actualResult.Add($"{count}/{length} {name} {checksum}");
    }

    Assert.That(actualResult, Is.EqualTo(new[]
    {
      "1/3 A 123",
      "2/3 B 456",
      "3/3 C 789"
    }));

    sshClient.Verify();
  }
}
