using ImpliciX.DesktopServices.Services;
using Moq.Language;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services.TargetSystemTests;

[TestFixture(typeof(ExecuteNoArgsToConsoleVerifier), "ResetToFactorySoftware")]
[TestFixture(typeof(ExecuteNoArgsToFileVerifier), "InfluxDbBackup")]
[TestFixture(typeof(ExecuteNoArgsToFileVerifier), "SystemJournalBackup")]
[TestFixture(typeof(ExecuteNoArgsToManyVerifier), "SystemHistoryBackup")]
[TestFixture(typeof(ExecuteNoArgsToConsoleVerifier), "SettingsReset")]
[TestFixture(typeof(ExecuteNoArgsToConsoleVerifier), "SystemReboot")]
[TestFixture(typeof(ExecuteArgsToConsoleVerifier), "NewSetup")]
[TestFixture(typeof(ExecuteArgsToConsoleVerifier), "NewTemporarySetup")]
public class SshTargetSystemCapabilitiesTests<TVerifier> : BaseClassForSshTests where TVerifier : IVerifier, new()
{
  private readonly string _capabilityName;
  private readonly IVerifier _verifier;
  private readonly Func<ITargetSystem, ITargetSystemCapability> _capability;

  public SshTargetSystemCapabilitiesTests(
    string capabilityName
  )
  {
    _capabilityName = capabilityName;
    _verifier = new TVerifier();
    _capability = d =>
      (ITargetSystemCapability)typeof(ITargetSystem).GetProperty(capabilityName)!.GetValue(d);
  }

  [Test]
  public async Task NominalMMI()
  {
    StubSshMMIApi(FullCapabilities);
    _verifier.Arrange(
      this,
      _capabilityName
    );
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      null
    );
    var capability = _capability(sut);
    Check.That(capability.IsAvailable).IsTrue();
    await _verifier.Act(
      this,
      capability
    );
    _verifier.Assert(this);
  }

  [Test]
  public async Task NominalKEP()
  {
    StubSshKEPApi(FullCapabilities);
    _verifier.Arrange(
      this,
      _capabilityName
    );
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      null
    );
    var capability = _capability(sut);
    Check.That(capability.IsAvailable).IsTrue();
    await _verifier.Act(
      this,
      capability
    );
    _verifier.Assert(this);
  }

  [Test]
  public async Task Failure()
  {
    StubSshMMIApi(FullCapabilities);
    _verifier.ArrangeThrow(
      this,
      _capabilityName
    ).Throws(new Exception("Client not connected."));
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      null
    );
    var capability = _capability(sut);
    Check
      .ThatAsyncCode(
        () => _verifier.Act(
          this,
          capability
        )
      )
      .ThrowsAny().WithMessage("Client not connected.");
  }

  [Test]
  public async Task Impossible()
  {
    StubSshMMIApi(NoCapabilities);
    var sut = await TargetSystem.Create(
      Concierge.Object,
      SshClientFactory,
      string.Empty,
      null
    );
    var capability = _capability(sut);
    Check.That(capability.IsAvailable).IsFalse();
  }
}

public interface IVerifier
{
  void Arrange(
    BaseClassForSshTests host,
    string capabilityName
  );

  IThrows ArrangeThrow(
    BaseClassForSshTests host,
    string capabilityName
  );

  Task Act(
    BaseClassForSshTests host,
    ITargetSystemCapability capability
  );

  void Assert(
    BaseClassForSshTests host
  );
}

public class ExecuteNoArgsToConsoleVerifier : IVerifier
{
  public void Arrange(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    host.SshClient.Setup(x => x.Execute($"executing {capabilityName}"))
      .Returns(Task.FromResult("executing OK"))
      .Verifiable();
  }

  public IThrows ArrangeThrow(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    return host.SshClient.Setup(x => x.Execute($"executing {capabilityName}"));
  }

  public Task Act(
    BaseClassForSshTests host,
    ITargetSystemCapability capability
  )
  {
    return capability.Execute().AndWriteResultToConsole();
  }

  public void Assert(
    BaseClassForSshTests host
  )
  {
    host.SshClient.Verify();
    Check.That(host.ConsoleOutput).IsEqualTo(
      new[]
      {
        "executing OK"
      }
    );
  }
}

public class ExecuteArgsToConsoleVerifier : IVerifier
{
  public void Arrange(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    host.SshClient.Setup(x => x.Execute($"executing {capabilityName} foo bar"))
      .Returns(Task.FromResult("executing OK"))
      .Verifiable();
  }

  public IThrows ArrangeThrow(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    return host.SshClient.Setup(x => x.Execute($"executing {capabilityName} foo bar"));
  }

  public Task Act(
    BaseClassForSshTests host,
    ITargetSystemCapability capability
  )
  {
    return capability.Execute(
      "foo",
      "bar"
    ).AndWriteResultToConsole();
  }

  public void Assert(
    BaseClassForSshTests host
  )
  {
    host.SshClient.Verify();
    Check.That(host.ConsoleOutput).IsEqualTo(
      new[]
      {
        "executing OK"
      }
    );
  }
}

public class ExecuteNoArgsToFileVerifier : IVerifier
{
  public void Arrange(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    host.SshClient.Setup(
        x => x.Execute(
          $"executing {capabilityName}",
          "output"
        )
      )
      .Returns(Task.CompletedTask)
      .Verifiable();
  }

  public IThrows ArrangeThrow(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    return host.SshClient.Setup(
      x => x.Execute(
        $"executing {capabilityName}",
        "output"
      )
    );
  }

  public Task Act(
    BaseClassForSshTests host,
    ITargetSystemCapability capability
  )
  {
    return capability.Execute().AndSaveTo("output");
  }

  public void Assert(
    BaseClassForSshTests host
  )
  {
    host.SshClient.Verify();
  }
}

public class ExecuteNoArgsToManyVerifier : IVerifier
{
  public void Arrange(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    host.SshClient.Setup(x => x.Execute($"executing {capabilityName}"))
      .Returns(Task.FromResult("A\tcmdA\nB\tcmdB\nC\tcmdC\n"))
      .Verifiable();
  }

  public IThrows ArrangeThrow(
    BaseClassForSshTests host,
    string capabilityName
  )
  {
    return host.SshClient.Setup(x => x.Execute($"executing {capabilityName}"));
  }

  public async Task Act(
    BaseClassForSshTests host,
    ITargetSystemCapability capability
  )
  {
    await foreach (var (count, length, name, _) in capability.Execute().AndSaveManyTo("output"))
      host.Concierge.Object.Console.WriteLine($"{count}/{length} {name}");
  }

  public void Assert(
    BaseClassForSshTests host
  )
  {
    host.SshClient.Verify();
    Check.That(host.ConsoleOutput).IsEqualTo(
      new[]
      {
        "1/3 A",
        "2/3 B",
        "3/3 C"
      }
    );
  }
}
