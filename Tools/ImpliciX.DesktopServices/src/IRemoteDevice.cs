using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DynamicData;

namespace ImpliciX.DesktopServices;

public interface IRemoteDevice
{
  IObservable<bool> IsConnected { get; }

  public string IPAddressOrHostname { get; }
  SourceCache<ImpliciXProperty, string> Properties { get; }

  IEnumerable<string> LocalIPAddresses { get; }

  ITargetSystem CurrentTargetSystem { get; }
  IObservable<ITargetSystem> TargetSystem { get; }
  IObservable<IRemoteDeviceDefinition> DeviceDefinition { get; }

  IAsyncEnumerable<string> Suggestions(string partOfIpAddressOrHostname);
  Task Connect(string ipAddressOrHostname);
  Task Disconnect(Exception e = null);
  Task<bool> Send(string json);
  Task Upload(string source, string destination);
}
