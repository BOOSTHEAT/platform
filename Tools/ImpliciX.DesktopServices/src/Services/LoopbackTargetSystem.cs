using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Data;

namespace ImpliciX.DesktopServices.Services;

internal sealed class LoopbackTargetSystem : ITargetSystem
{
  public LoopbackTargetSystem(string address)
  {
    Address = address;
    ConnectionString = address;
    MetricsColdStorageDownload = new ColdStorageDownloadCapability(this, "external/metrics/finished", "*.metrics.zip");
    RecordsColdStorageDownload = new ColdStorageDownloadCapability(this, "external/records/finished", "*.records.zip");
    ;
  }

  public static string LocalStorage => Environment.GetEnvironmentVariable("IMPLICIX_LOCAL_STORAGE") ?? "/tmp/slot";

  public string Name { get; } = "Loopback";
  public string ConnectionString { get; }
  public string Address { get; }
  public SystemInfo SystemInfo { get; } = new LocalSystemInfo();

  public Task FixAppConnection() => Task.CompletedTask;
  public ITargetSystemCapability SystemCheck { get; } = new MissingCapability();
  public ITargetSystemCapability ResetToFactorySoftware { get; } = new MissingCapability();
  public ITargetSystemCapability InfluxDbBackup { get; } = new MissingCapability();
  public ITargetSystemCapability SystemJournalBackup { get; } = new MissingCapability();
  public ITargetSystemCapability ImplicixVarLibBackup { get; } = new MissingCapability();
  public ITargetSystemCapability SystemHistoryBackup { get; } = new MissingCapability();
  public ITargetSystemCapability SettingsReset { get; } = new MissingCapability();
  public ITargetSystemCapability SystemReboot { get; } = new MissingCapability();
  public ITargetSystemCapability MetricsColdStorageDownload { get; }
  public ITargetSystemCapability MetricsColdStorageClear { get; } = new NeutralCapability();
  public ITargetSystemCapability NewSetup { get; } = new MissingCapability();
  public ITargetSystemCapability NewTemporarySetup { get; } = new MissingCapability();
  public ITargetSystemCapability RecordsColdStorageDownload { get; }
  public ITargetSystemCapability RecordsColdStorageClear { get; } = new NeutralCapability();

  record LocalSystemInfo() : SystemInfo(GetLocalOs(), GetLocalArchitecture(), null)
  {
    private static OS GetLocalOs() => OS.linux;
    private static Architecture GetLocalArchitecture() => Architecture.x64;
  }

  class MissingCapability : ITargetSystemCapability
  {
    public bool IsAvailable { get; } = false;
    public ITargetSystemCapability.IExecution Execute(params string[] args) => throw new System.NotSupportedException();
  }

  class NeutralCapability : ITargetSystemCapability
  {
    public bool IsAvailable { get; } = true;
    public ITargetSystemCapability.IExecution Execute(params string[] args) => new NeutralExecution();
  }

  class NeutralExecution : ITargetSystemCapability.IExecution
  {
    public Task AndWriteResultToConsole() => Task.CompletedTask;
    public Task AndSaveTo(string destination) => Task.CompletedTask;
    public async IAsyncEnumerable<(int count, int length, string name, string checksum)> AndSaveManyTo(
      string destination)
    {
      yield break;
    }
  }

  public class ColdStorageDownloadCapability : ITargetSystemCapability
  {
    private readonly LoopbackTargetSystem _lts;

    public ColdStorageDownloadCapability(LoopbackTargetSystem lts, string folderPath, string fileExtension)
    {
      _lts = lts;
      ColdFinishedFolder = Path.Combine(LocalStorage, folderPath);
      FileExtension = fileExtension;
    }

    public string FileExtension { get; set; }

    public string ColdFinishedFolder { get; set; }

    public bool IsAvailable { get; } = true;

    public ITargetSystemCapability.IExecution Execute(params string[] args)
    {
      return new FileCopyExecution(Directory
        .EnumerateFiles(ColdFinishedFolder, FileExtension)
        .Order());
    }
  }

  class FileCopyExecution : ITargetSystemCapability.IExecution
  {
    private readonly IEnumerable<string> _files;
    public FileCopyExecution(IEnumerable<string> files) => _files = files;
    public Task AndWriteResultToConsole() => throw new System.NotSupportedException();
    public Task AndSaveTo(string destination) => throw new System.NotSupportedException();

    public async IAsyncEnumerable<(int count, int length, string name, string checksum)> AndSaveManyTo(
      string destination)
    {
      var files = _files.ToArray();
      foreach (var f in files.Select((path, index) =>
               {
                 var filename = Path.GetFileName(path);
                 File.Copy(path, Path.Combine(destination, filename));
                 return (index + 1, files.Length, filename, Sha256.OfFile(path));
               }))
        yield return f;
    }
  }
}
