using System;

namespace ImpliciX.DesktopServices.Services.WebsocketInfrastructure;

internal class WebsocketConnectionFailure : ApplicationException
{
  public WebsocketConnectionFailure(string message) : base(message)
  {
  }
}