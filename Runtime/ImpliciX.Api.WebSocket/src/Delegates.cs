using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Api.WebSocket
{
  public delegate IEnumerable<IDataModelValue> ReadProperties();

  public delegate void EventBusSend(params DomainEvent[] domainEvent);

  public delegate Task<bool> WebSocketSend(Guid clientId, string json);

  public delegate IDictionary<Urn, Dictionary<Urn, HashSet<TimeSeriesValue>>> ReadTimeSeries();

  public delegate TimeSpan Clock();
}