using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class LiveConnectViewModel : ViewModelBase
{
  public LiveConnectViewModel(ILightConcierge concierge)
  {
    _appConnection = concierge.RemoteDevice;
    ConnectionString = "";
    IsConnected = false;
    concierge.RemoteDevice.IsConnected
      .Subscribe(isConnected =>
      {
        if (isConnected)
        {
          IsConnected = true;
          return;
        }
        IsConnected = false;
        CanInitiateConnection = true;
      });
    this
      .WhenPropertyChanged(x => x.ConnectionString)
      .Subscribe(cs => { CanInitiateConnection = !string.IsNullOrEmpty(cs.Value?.Trim()); });
  }

#pragma warning disable CS8974 // Converting method group to non-delegate type
  public object Populate => PopulateAsync;

  public Task<IEnumerable<object>> PopulateAsync(string input, CancellationToken cancel) =>
    Task.FromResult<IEnumerable<object>>(_appConnection.Suggestions(input).ToBlockingEnumerable());

  public async Task Connect()
  {
    if (!CanInitiateConnection)
      return;
    CanInitiateConnection = false;
    await _appConnection.Connect(ConnectionString);
  }

  public void Disconnect()
  {
    _appConnection.Disconnect();
  }

  public string ConnectionString
  {
    get => _connectionString;
    set => this.RaiseAndSetIfChanged(ref _connectionString, value);
  }

  private string _connectionString;

  public bool IsConnected
  {
    get => _isConnected;
    set => this.RaiseAndSetIfChanged(ref _isConnected, value);
  }

  private bool _isConnected;

  public bool CanInitiateConnection
  {
    get => _canInitiateConnection;
    set => this.RaiseAndSetIfChanged(ref _canInitiateConnection, value);
  }

  private bool _canInitiateConnection;


  private readonly IRemoteDevice _appConnection;
}