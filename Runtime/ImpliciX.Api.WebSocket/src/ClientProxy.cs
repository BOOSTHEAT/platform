using System;
using ImpliciX.Data.Factory;
using ImpliciX.RuntimeFoundations;

namespace ImpliciX.Api.WebSocket
{
  public class ClientProxy
  {
    public static Startup Create(Guid clientId, WebSocketSend fnSocketSend, EventBusSend fnBusSend,
      ModelFactory modelFactory, Clock clock) =>
      new(new ClientProxy(clientId, fnSocketSend, fnBusSend, modelFactory, clock));

    public class Startup
    {
      private readonly ClientProxy _clientProxy;

      public Startup(ClientProxy clientProxy)
      {
        _clientProxy = clientProxy;
      }

      public ClientProxy Start(
        ApplicationRuntimeDefinition rtDef,
        ReadProperties fnReadProperties,
        ReadTimeSeries fnReadTimeSeries)
      {
        _clientProxy.Outgoing.Start(rtDef, fnReadProperties, fnReadTimeSeries);
        return _clientProxy;
      }
    }
    
    public readonly IncomingFromClient Incoming;
    public readonly OutgoingToClient Outgoing;

    private ClientProxy(Guid clientId,
      WebSocketSend socketSend,
      EventBusSend busSend,
      ModelFactory modelFactory, Clock clock)
    {
      Incoming = new IncomingFromClient(clientId, clock, modelFactory, busSend);
      Outgoing = new OutgoingToClient(clientId, clock, socketSend);
    }
    
  }
}