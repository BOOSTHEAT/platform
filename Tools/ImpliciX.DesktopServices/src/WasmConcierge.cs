using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using ImpliciX.DesktopServices.Services;

namespace ImpliciX.DesktopServices;

public class WasmConcierge : ILightConcierge
{
  private WasmConcierge(IUser user)
  {
    Console = new WasmConsoleService();
    Export = new ExportService(Console.WriteLine);
    Applications = new ApplicationsManager(Console);
    RemoteDevice = new WasmRemoteDevice();
    User = user;
  }

  public IConsoleService Console { get; }

  public IIdentity Identity
  {
    get
    {
      System.Console.WriteLine("WasmConcierge.Identity");
      return null;
    }
  }

  public IRemoteDevice RemoteDevice { get; }

  public IOperatingSystem OperatingSystem
  {
    get
    {
      System.Console.WriteLine("WasmConcierge.OperatingSystem");
      return null;
    }
  }

  public IUser User { get; }

  public IFileSystemService FileSystemService
  {
    get
    {
      System.Console.WriteLine("WasmConcierge.FileSystemService");
      throw new NotImplementedException();
    }
  }

  public IExport Export { get; }

  public RuntimeFlags RuntimeFlags
  {
    get
    {
      System.Console.WriteLine("WasmConcierge.RuntimeFlags");
      return null;
    }
  }

  public IManageApplicationDefinitions Applications { get; }

  public ISessionService Session => new WasmSession();

  public static ILightConcierge Create(IUser user)
  {
    System.Console.WriteLine("\ud83d\udd51 WasmConcierge.Create");
    var c = new WasmConcierge(user);
    System.Console.WriteLine("\u2713 WasmConcierge.Create");
    return c;
  }
}

internal class WasmConsoleService : IConsoleService
{
  public event EventHandler<string> LineWritten;

  public void WriteLine(string text)
  {
    Console.WriteLine(text);
  }

  public event EventHandler<Exception> Errors;

  public void WriteError(Exception e)
  {
    Console.Error.WriteLine(e);
  }
}

public class WasmRemoteDevice : IRemoteDevice
{
  private readonly Subject<IRemoteDeviceDefinition> _deviceDefinition;
  private readonly Subject<bool> _isConnected;
  private readonly Subject<ITargetSystem> _targetSystem;

  public WasmRemoteDevice()
  {
    _targetSystem = new Subject<ITargetSystem>();
    _isConnected = new Subject<bool>();
    _deviceDefinition = new Subject<IRemoteDeviceDefinition>();
  }

  public IObservable<bool> IsConnected => _isConnected;

  public string IPAddressOrHostname
  {
    get
    {
      Console.WriteLine("WasmRemoteDevice.IPAddressOrHostname");
      return null;
    }
  }

  public SourceCache<ImpliciXProperty, string> Properties
  {
    get
    {
      Console.WriteLine("WasmRemoteDevice.Properties");
      return null;
    }
  }

  public IEnumerable<string> LocalIPAddresses
  {
    get
    {
      Console.WriteLine("WasmRemoteDevice.LocalIPAddresses");
      return null;
    }
  }

  public ITargetSystem CurrentTargetSystem => new WasmTargetSystem();

  public IObservable<ITargetSystem> TargetSystem => _targetSystem;
  public IObservable<IRemoteDeviceDefinition> DeviceDefinition => _deviceDefinition;

  public IAsyncEnumerable<string> Suggestions(string partOfIpAddressOrHostname)
  {
    Console.WriteLine("WasmRemoteDevice.Suggestions");
    throw new NotImplementedException();
  }

  public Task Connect(string ipAddressOrHostname)
  {
    Console.WriteLine("WasmRemoteDevice.Connect");
    throw new NotImplementedException();
  }

  public Task Disconnect(Exception e = null)
  {
    Console.WriteLine("WasmRemoteDevice.Disconnect");
    throw new NotImplementedException();
  }

  public Task<bool> Send(string json)
  {
    Console.WriteLine("WasmRemoteDevice.Send");
    throw new NotImplementedException();
  }

  public Task Upload(string source, string destination)
  {
    Console.WriteLine("WasmRemoteDevice.Upload");
    throw new NotImplementedException();
  }
}

public class WasmTargetSystem : ITargetSystem
{
  public string Name
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.Name");
      return null;
    }
  }

  public string ConnectionString
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.ConnectionString");
      return null;
    }
  }

  public string Address
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.Address");
      return null;
    }
  }

  public SystemInfo SystemInfo
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.SystemInfo");
      return null;
    }
  }

  public Task FixAppConnection()
  {
    throw new NotImplementedException();
  }

  public ITargetSystemCapability SystemCheck
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.SystemCheck");
      return null;
    }
  }

  public ITargetSystemCapability ResetToFactorySoftware
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.ResetToFactorySoftware");
      return null;
    }
  }

  public ITargetSystemCapability InfluxDbBackup
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.InfluxDbBackup");
      return null;
    }
  }

  public ITargetSystemCapability SystemJournalBackup
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.SystemJournalBackup");
      return null;
    }
  }

  public ITargetSystemCapability ImplicixVarLibBackup
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.ImplicixVarLibBackup");
      return null;
    }
  }

  public ITargetSystemCapability SystemHistoryBackup
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.SystemHistoryBackup");
      return null;
    }
  }

  public ITargetSystemCapability SettingsReset
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.SettingsReset");
      return null;
    }
  }

  public ITargetSystemCapability SystemReboot
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.SystemReboot");
      return null;
    }
  }

  public ITargetSystemCapability MetricsColdStorageDownload
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.MetricsColdStorageDownload");
      return null;
    }
  }

  public ITargetSystemCapability MetricsColdStorageClear
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.MetricsColdStorageClear");
      return null;
    }
  }

  public ITargetSystemCapability NewSetup
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.NewSetup");
      return null;
    }
  }

  public ITargetSystemCapability NewTemporarySetup
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.NewTemporarySetup");
      return null;
    }
  }

  public ITargetSystemCapability RecordsColdStorageDownload
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.RecordsColdStorageDownload");
      return null;
    }
  }

  public ITargetSystemCapability RecordsColdStorageClear
  {
    get
    {
      Console.WriteLine("WasmTargetSystem.RecordsColdStorageClear");
      return null;
    }
  }
}

public class WasmSession : ISessionService
{
  private const string PersistenceKey = "SessionHistory";

  private static readonly int SessionHistorySize =
    int.Parse(Environment.GetEnvironmentVariable("IMPLICIX_SESSION_HISTORY_SIZE") ?? "10");

  private readonly History<ISessionService.Session> _history = new(SessionHistorySize, PersistenceKey);

  public WasmSession()
  {
    Properties = new SourceCache<ImpliciXProperty, string>(x => x.Urn);
    Updates = new Subject<ISessionService.Session>();
  }

  public void Dispose()
  {
    throw new NotImplementedException();
  }

  public ISessionService.Session Current
  {
    get
    {
      Console.WriteLine("WasmSession.Current");
      return null;
    }
  }

  public IObservable<ISessionService.Session> Updates { get; }

  public IEnumerable<ISessionService.Session> History => _history;
  public IObservable<IEnumerable<ISessionService.Session>> HistoryUpdates => _history.Subject;

  public SourceCache<ImpliciXProperty, string> Properties { get; }
}
