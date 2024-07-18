using System;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

internal interface IWebsocketClient : IDisposable
{
  Task Start();
  bool IsConnected { get; }
  Task<bool> Send(string json);
  event EventHandler<string> MessageReceived;
  event EventHandler<bool> ConnectionLost;
}