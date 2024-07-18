namespace ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

internal class WebSocketClientWrapper : IWebsocketAdapter
{
  public IWebsocketClient CreateClient(int timeout, string ipAddress, int port) =>
    new WebsocketClient(timeout, ipAddress, port);
}