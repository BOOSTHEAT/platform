using System;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Api.WebSocket;

public interface IApiListener : IDisposable
{
  void StartListening();
  DomainEvent[] HandlePropertiesChanged(PropertiesChanged @event);
  DomainEvent[] HandleTimeSeriesChanged(TimeSeriesChanged @event);
}