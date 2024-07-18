using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Services.SshInfrastructure;
using ImpliciX.Language.Core;

namespace ImpliciX.DesktopServices.Services;

internal class TargetSystem : ITargetSystem
{
  private const string Root = "/root";
  private const string ImpliciXDeviceCapabilities = "/implicix.json";
  private readonly BoardSshApi _api;
  private readonly IBaseConcierge _concierge;
  private readonly SshClientFactory _sshClientFactory;

  private TargetSystem(
    IBaseConcierge concierge,
    SshClientFactory sshClientFactory,
    string connectionString,
    IConsoleOutputSlice systemCheckConsoleOutput,
    string capabilities
  )
  {
    _concierge = concierge;
    _sshClientFactory = sshClientFactory;
    ConnectionString = connectionString;
    _api = JsonSerializer.Deserialize<BoardSshApi>(capabilities);
    var commands = _api.Capabilities ?? new Dictionary<string, string>();
    ResetToFactorySoftware = new Capability(this, commands, nameof(ResetToFactorySoftware));
    InfluxDbBackup = new Capability(this, commands, nameof(InfluxDbBackup));
    SystemJournalBackup = new Capability(this, commands, nameof(SystemJournalBackup));
    ImplicixVarLibBackup = new Capability(this, commands, nameof(ImplicixVarLibBackup));
    SystemHistoryBackup = new Capability(this, commands, nameof(SystemHistoryBackup));
    SettingsReset = new Capability(this, commands, nameof(SettingsReset));
    SystemReboot = new Capability(this, commands, nameof(SystemReboot));
    MetricsColdStorageDownload = new Capability(this, commands, nameof(MetricsColdStorageDownload));
    MetricsColdStorageClear = new Capability(this, commands, nameof(MetricsColdStorageClear));
    RecordsColdStorageDownload = new Capability(this, commands, nameof(RecordsColdStorageDownload));
    RecordsColdStorageClear = new Capability(this, commands, nameof(RecordsColdStorageClear));
    NewSetup = new Capability(this, commands, nameof(NewSetup));
    NewTemporarySetup = new Capability(this, commands, nameof(NewTemporarySetup));
    SystemCheck = new TargetSystemCheck(concierge, systemCheckConsoleOutput, this);
  }

  public string Name => _api.Name;
  public string ConnectionString { get; }
  public string Address => "127.0.0.1";
  public SystemInfo SystemInfo => _api.SystemInfo;

  public ITargetSystemCapability SystemCheck { get; }
  public ITargetSystemCapability ResetToFactorySoftware { get; }
  public ITargetSystemCapability InfluxDbBackup { get; }
  public ITargetSystemCapability SystemJournalBackup { get; }

  public ITargetSystemCapability ImplicixVarLibBackup { get; }
  public ITargetSystemCapability SystemHistoryBackup { get; }
  public ITargetSystemCapability SettingsReset { get; }
  public ITargetSystemCapability SystemReboot { get; }
  public ITargetSystemCapability MetricsColdStorageDownload { get; }
  public ITargetSystemCapability MetricsColdStorageClear { get; }
  public ITargetSystemCapability NewSetup { get; }
  public ITargetSystemCapability NewTemporarySetup { get; }
  public ITargetSystemCapability RecordsColdStorageDownload { get; }
  public ITargetSystemCapability RecordsColdStorageClear { get; }

  public async Task FixAppConnection()
  {
    var choice = await UserDecisionOnFixAppConnection();
    var actions = new List<Func<Task>>();
    if (choice == IUser.ChoiceType.Custom1)
    {
      var folder = await _concierge.User.OpenFolder(
        new IUser.FileSelection
        {
          Title = "Select system check destination folder"
        }
      );
      if (folder.Choice == IUser.ChoiceType.Ok)
        actions.Add(async () => await SystemCheck.Execute().AndSaveTo(folder.Path));
    }

    if (choice == IUser.ChoiceType.Yes)
      actions.Add(() => ResetToFactorySoftware.Execute().AndWriteResultToConsole());
    try
    {
      foreach (var action in actions)
        await action();
      await _concierge.RemoteDevice.Disconnect();
    }
    catch (Exception e)
    {
      await _concierge.RemoteDevice.Disconnect(e);
    }
  }
  public static async Task<ITargetSystem> Create(
    IBaseConcierge concierge,
    SshClientFactory sshClientFactory,
    string connectionString,
    IConsoleOutputSlice systemCheckConsoleOutput
  )
  {
    var capabilities = await DownloadCapabilities(sshClientFactory);
    try
    {
      return new TargetSystem(concierge, sshClientFactory, connectionString, systemCheckConsoleOutput, capabilities);
    }
    catch (Exception e)
    {
      throw new ApplicationException($"Invalid content in {ImpliciXDeviceCapabilities}", e);
    }
  }

  private static async Task<string> DownloadCapabilities(
    SshClientFactory sshClientFactory
  )
  {
    using var sftpClient = await sshClientFactory.CreateSftpClient();
    try
    {
      return await sftpClient.Download(Root + ImpliciXDeviceCapabilities);
    }
    catch (Exception e1)
    {
      try
      {
        return await sftpClient.Download(ImpliciXDeviceCapabilities);
      }
      catch (Exception e2)
      {
        throw new ApplicationException(
          $"Failed to download capabilities, neither {Root}{ImpliciXDeviceCapabilities} nor {ImpliciXDeviceCapabilities} can be download",
          e2
        );
      }
    }
  }

  private async Task<IUser.ChoiceType> UserDecisionOnFixAppConnection()
  {
    var (message, buttons) = ResetToFactorySoftware.IsAvailable
      ? (@"Cannot connect to the embedded application.
Do you want to reset the target board to factory software?",
        IUser.StandardButtons(
          IUser.ChoiceType.Yes,
          IUser.ChoiceType.No
        ))
      : (@"Cannot connect to the embedded application and
the target board has no reset to factory software capability.",
        IUser.StandardButtons(IUser.ChoiceType.Ok));
    var box = new IUser.Box
    {
      Title = "Connection failed",
      Message = message,
      Icon = IUser.Icon.Error,
      Buttons = buttons.Append(new IUser.Choice {Type = IUser.ChoiceType.Custom1, Text = "System Check..."})
    };
    var choice = await _concierge.User.Show(box);
    return choice;
  }

  private class BoardSshApi
  {
    public string Name { get; set; }
    public Dictionary<string, string> Capabilities { get; set; }
    public SystemInfo SystemInfo { get; set; }
  }

  private class Capability : ITargetSystemCapability
  {
    private readonly Option<string> _command;

    private readonly TargetSystem _device;

    public Capability(
      TargetSystem device,
      Dictionary<string, string> commands,
      string key
    )
    {
      _device = device;
      _command =
        commands.TryGetValue(
          key,
          out var command
        )
          ? Option<string>.Some(command)
          : Option<string>.None();
    }

    public bool IsAvailable => _command.IsSome;

    public ITargetSystemCapability.IExecution Execute(
      params string[] args
    )
    {
      return new Execution(
        this,
        args
      );
    }

    public override string ToString()
    {
      return _command.GetValueOrDefault("");
    }

    private class Execution : ITargetSystemCapability.IExecution
    {
      private readonly Capability _capability;
      private readonly string _command;

      public Execution(
        Capability capability,
        string[] args
      )
      {
        _capability = capability;
        _command = args
          .Select(
            (
              val,
              idx
            ) => (val, idx)
          )
          .Aggregate(
            _capability._command.GetValue(),
            (
              cmd,
              arg
            ) => cmd.Replace(
              $"${arg.idx + 1}",
              arg.val
            )
          );
      }

      public async Task AndWriteResultToConsole()
      {
        using var sshClient = await _capability._device._sshClientFactory.CreateSshClient();
        var result = await sshClient.Execute(_command);
        _capability._device._concierge.Console.WriteLine(result);
      }

      public async Task AndSaveTo(
        string destination
      )
      {
        using var sshClient = await _capability._device._sshClientFactory.CreateSshClient();
        await sshClient.Execute(
          _command,
          destination
        );
      }

      public async IAsyncEnumerable<(int count, int length, string name, string checksum)> AndSaveManyTo(
        string destination
      )
      {
        using var sshClient = await _capability._device._sshClientFactory.CreateSshClient();
        await foreach (var item in sshClient.ExecuteMany(
                         _command,
                         destination
                       ))
          yield return item;
      }
    }
  }
}
