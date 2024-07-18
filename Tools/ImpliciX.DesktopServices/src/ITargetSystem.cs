using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using ImpliciX.DesktopServices.Helpers;

namespace ImpliciX.DesktopServices;

public enum OS
{
  linux,
}

public enum Architecture
{
  arm,
  x64,
}

public record SystemInfo(OS Os, Architecture Architecture, string Hardware)
{
  [System.Text.Json.Serialization.JsonConverter(typeof(StringEnumConverter<OS>))]
  public OS Os { get; init; } = Os;

  [System.Text.Json.Serialization.JsonConverter(typeof(StringEnumConverter<Architecture>))]
  public Architecture Architecture { get; init; } = Architecture;

  public string Hardware { get; init; } = Hardware;

  public static SystemInfo FromString(string str) => JsonSerializer.Deserialize<SystemInfo>(str);
  public override string ToString() => JsonSerializer.Serialize(this);
}

public interface ITargetSystem
{
  string Name { get; }
  string ConnectionString { get; }
  string Address { get; }
  SystemInfo SystemInfo { get; }
  ITargetSystemCapability SystemCheck { get; }
  ITargetSystemCapability ResetToFactorySoftware { get; }
  ITargetSystemCapability InfluxDbBackup { get; }
  ITargetSystemCapability SystemJournalBackup { get; }
  ITargetSystemCapability ImplicixVarLibBackup { get; }
  ITargetSystemCapability SystemHistoryBackup { get; }
  ITargetSystemCapability SettingsReset { get; }
  ITargetSystemCapability SystemReboot { get; }
  ITargetSystemCapability MetricsColdStorageDownload { get; }
  ITargetSystemCapability MetricsColdStorageClear { get; }
  ITargetSystemCapability NewSetup { get; }
  ITargetSystemCapability NewTemporarySetup { get; }
  ITargetSystemCapability RecordsColdStorageDownload { get; }
  ITargetSystemCapability RecordsColdStorageClear { get; }
  Task FixAppConnection();
}

public interface ITargetSystemCapability
{
  bool IsAvailable { get; }
  IExecution Execute(params string[] args);

  public interface IExecution
  {
    Task AndWriteResultToConsole();
    Task AndSaveTo(string destination);
    IAsyncEnumerable<(int count, int length, string name, string checksum)> AndSaveManyTo(string destination);
  }
}
