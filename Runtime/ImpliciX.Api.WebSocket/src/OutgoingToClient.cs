using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Data.Api;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.SharedKernel.Scheduling;
using TimeSeriesValue = ImpliciX.Data.Api.TimeSeriesValue;

namespace ImpliciX.Api.WebSocket;

public class OutgoingToClient
{
  private readonly Guid _clientId;
  private readonly Clock _clock;
  private readonly WebSocketSend _socketSend;

  public OutgoingToClient(Guid clientId, Clock clock, WebSocketSend socketSend)
  {
    _clientId = clientId;
    _clock = clock;
    _socketSend = socketSend;
  }

  public void Start(ApplicationRuntimeDefinition rtDef, ReadProperties retrieveCurrentProperties,
    ReadTimeSeries fnReadTimeSeries)
  {
    var currentProperties = retrieveCurrentProperties().ToArray();
    var dmd = rtDef.Application.DataModelDefinition;
    Output(new MessagePrelude
    {
      Kind = MessageKind.prelude,
      Name = rtDef.Application.AppName,
      Version = currentProperties.FirstOrDefault(p => p.Urn.Value == dmd.AppVersion?.Value)?.ModelValue()?.ToString(),
      Setup = currentProperties.FirstOrDefault(p => p.Urn.Value == dmd.AppEnvironment?.Value)?.ModelValue()?.ToString(),
      Setups = rtDef.Setups
    });
    PropertiesOutput(PropertiesChanged.Create(currentProperties, _clock()));
    fnReadTimeSeries()
      .Select(it => TimeSeriesChanged.Create(it.Key, it.Value, _clock()))
      .ForEach(TimeSeriesOutput);
  }

  public Action<DomainEvent> PreludeOutput => Output((PropertiesChanged propertiesChanged) =>
    new MessageProperties
    {
      Kind = MessageKind.properties,
      Properties = propertiesChanged
        .ModelValues
        .SelectMany(mv => new[] { new Property(mv) })
        .ToArray()
    });

  public Action<DomainEvent> PropertiesOutput => Output((PropertiesChanged propertiesChanged) =>
    new MessageProperties
    {
      Kind = MessageKind.properties,
      Properties = propertiesChanged
        .ModelValues
        .SelectMany(mv => new[] { new Property(mv) })
        .ToArray()
    });
  
  public Action<DomainEvent> TimeSeriesOutput => Output((TimeSeriesChanged timeSeriesChanged) =>
    new MessageTimeSeries()
    {
      Urn = timeSeriesChanged.Urn,
      DataPoints = timeSeriesChanged.TimeSeries.Aggregate(new Dictionary<string, List<TimeSeriesValue>>(),
        (acc, n) =>
        {
          acc[n.Key.Value] = n.Value
            .Select(tsv => new TimeSeriesValue(new DateTime(tsv.At.Ticks, DateTimeKind.Utc), tsv.Value)).ToList();
          return acc;
        })
    });
  
  public Action<DomainEvent> IdleOutput => Output((Idle idle) =>
    new MessageProperties { Kind = MessageKind.idle, At = Formaters.FormatTime(idle.At) });

  private Action<DomainEvent> Output<T>(Func<T,Message> serializer) where T : DomainEvent
    => domainEvent => Output(serializer((T)domainEvent));

  private Task<bool> Output(Message message)
  {
    var outgoingJson = message.ToJson();
    Log.Verbose("WebApi send: {0}", outgoingJson);
    return _socketSend(_clientId, outgoingJson);
  }
}