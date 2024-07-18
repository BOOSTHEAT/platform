using System;

namespace ImpliciX.Api.WebSocket
{
  public class ApiSettings
  {
    public string IP { get; set; } = String.Empty;
    public int Port { get; set; }

    public int Version { get; set; } = 2;
  }
}