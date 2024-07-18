using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Scheduling;
using WatsonWebsocket;

namespace ImpliciX.Api.WebSocket
{
  public class ApiListener : IApiListener
  {
    private readonly IEventBusWithFirewall _eventBus;
    private readonly ApplicationRuntimeDefinition _runtimeDefinition;
    private readonly Clock _clock;
    private readonly ConcurrentDictionary<string, IDataModelValue> _propertiesCache;
    private readonly ConcurrentDictionary<Urn, Dictionary<Urn, HashSet<TimeSeriesValue>>> _timeSeriesCache;
    private readonly WatsonWsServer _webSocketServer;
    private readonly ConcurrentDictionary<string, ClientProxy> _proxyList;

    public ApiListener(ApiSettings settings, IEventBusWithFirewall eventBus, ApplicationRuntimeDefinition rtDef, Clock clock)
    {
      _eventBus = eventBus;
      _runtimeDefinition = rtDef;
      _clock = clock;
      _propertiesCache = new ConcurrentDictionary<string, IDataModelValue>();
      _timeSeriesCache = new ConcurrentDictionary<Urn, Dictionary<Urn, HashSet<TimeSeriesValue>>>();
      _webSocketServer = new WatsonWsServer(settings.IP, settings.Port, false);
      _proxyList = new ConcurrentDictionary<string, ClientProxy>();
      StartListening();
    }

    public void StartListening()
    {
      _webSocketServer.ClientConnected += (sender, arg) =>
      {
        Debug.PreCondition(() => sender != null, () => "Sender can't be null");
        var clientProxy = ClientProxy.Create(
            clientId: arg.Client.Guid,
            fnSocketSend: async (guid, json) =>
            {
              var result = await _webSocketServer.SendAsync(guid, json);
              return result;
            }, 
            fnBusSend: _eventBus.Publish,
            modelFactory: _runtimeDefinition.ModelFactory,
            clock: _clock)
          .Start(
            _runtimeDefinition,
            fnReadProperties: () => _propertiesCache.Values,
            fnReadTimeSeries: () => _timeSeriesCache
          );

        if (_proxyList.TryAdd(arg.Client.IpPort, clientProxy))
        {
          _eventBus.Subscribe(clientProxy, typeof(PropertiesChanged), evt => clientProxy.Outgoing.PropertiesOutput(evt));
          _eventBus.Subscribe(clientProxy, typeof(Idle), evt => clientProxy.Outgoing.IdleOutput(evt));
          _eventBus.Subscribe(clientProxy, typeof(TimeSeriesChanged), evt => clientProxy.Outgoing.TimeSeriesOutput(evt));
        }
      };
      _webSocketServer.ClientDisconnected += (sender, arg) =>
      {
        if (_proxyList.TryRemove(arg.Client.IpPort, out var proxy))
        {
          _eventBus.UnSubscribe(proxy, typeof(PropertiesChanged));
          _eventBus.UnSubscribe(proxy, typeof(Idle));
        }
      };
      _webSocketServer.MessageReceived += (sender, arg) =>
      {
        Log.Verbose("Received from {Sender}: {$Data}", arg.Client.IpPort, arg.Data);
        if (_proxyList.TryGetValue(arg.Client.IpPort, out var proxy))
        {
          proxy.Incoming.Input(arg.Data);
        }

        ;
      };
      _webSocketServer.Start();
      Log.Information("Websocket started on API V2");
    }


    public DomainEvent[] HandlePropertiesChanged(PropertiesChanged @event)
    {
      foreach (var modelValue in @event.ModelValues)
        _propertiesCache[modelValue.Urn] = modelValue;
      return Array.Empty<DomainEvent>();
    }

    public DomainEvent[] HandleTimeSeriesChanged(TimeSeriesChanged @event)
    {
      _timeSeriesCache[@event.Urn] = @event.TimeSeries;
      return Array.Empty<DomainEvent>();
    }

    public void Dispose()
    {
      _webSocketServer?.Dispose();
    }
  }
}