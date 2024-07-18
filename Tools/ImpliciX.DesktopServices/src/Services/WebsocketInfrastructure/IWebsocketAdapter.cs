namespace ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

internal interface IWebsocketAdapter
{
  IWebsocketClient CreateClient(int timeout, string ipAddress, int port);
}