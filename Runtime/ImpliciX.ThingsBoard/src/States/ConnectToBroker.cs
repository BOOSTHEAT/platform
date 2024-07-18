using System;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.ThingsBoard.Infrastructure;

namespace ImpliciX.ThingsBoard.States
{
  public class ConnectToBroker : BaseState
  {
    public ConnectToBroker(IClock clock) : base(nameof(ConnectToBroker))
    {
      _clock = clock;
    }

    public Transition<BaseState, (Context, DomainEvent)> WhenConnectionIsSuccess(BaseState to)
      => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is ConnectionSuccess);

    public Transition<BaseState, (Context, DomainEvent)> WhenConnectionIsFailed(BaseState to)
      => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is ConnectionFailed);

    protected override DomainEvent[] OnEntry(Context context, DomainEvent _)
    {
      var connectionDetails = new MqttAdapter.ConnectionDetails
      {
        Host = context.Host,
        AccessToken = context.AccessToken
      };
      return MqttAdapter.CreateFor(connectionDetails).Match(error =>
        {
          Log.Error(error.Message);
          context.RetryContext.DecrementRemainingRetries();
          return new DomainEvent[] { new ConnectionFailed(_clock.Now()) };
        },
        adapter =>
        {
          context.Adapter = adapter;
          context.RetryContext.ResetRemainingRetries();
          return new DomainEvent[] { new ConnectionSuccess(_clock.Now()) };
        });
    }

    public override bool CanHandle(Context _, DomainEvent @event) =>
      @event is ConnectionSuccess || @event is SystemTicked || @event is ConnectionFailed;

    private readonly IClock _clock;

    public class ConnectionFailed : PrivateDomainEvent
    {
      public ConnectionFailed(TimeSpan at) : base(Guid.NewGuid(), at)
      {
      }
    }

    public class ConnectionSuccess : PrivateDomainEvent
    {
      public ConnectionSuccess(TimeSpan at) : base(Guid.NewGuid(), at)
      {
      }
    }
  }
}