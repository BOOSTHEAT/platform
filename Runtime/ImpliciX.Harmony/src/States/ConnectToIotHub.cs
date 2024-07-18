using System;
using ImpliciX.Harmony.Infrastructure;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Harmony.States
{
    public class ConnectToIotHub : BaseState
    {
        public ConnectToIotHub(IClock clock) : base(nameof(ConnectToIotHub))
        {
            _clock = clock;
        }

        public Transition<BaseState, (Context, DomainEvent)> WhenConnectionIsSuccess(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is ConnectionSuccess);

        public Transition<BaseState, (Context, DomainEvent)> WhenConnectionIsFailed(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is ConnectionFailed);

        protected override DomainEvent[] OnEntry(Context context, DomainEvent _)
        {
            return AzureIotHubAdapter.Create(context.DeviceId, context.IotHubSettings).Match(error =>
                {
                    Log.Error(error.Message);
                    context.IotHubRetryContext.DecrementRemainingRetries();
                    return new DomainEvent[] { new ConnectionFailed(_clock.Now()) };
                },
                adapter =>
                {
                    Log.Information("Connection to {Device} created", context.DeviceId);
                    context.AzureIoTHubAdapter = adapter;
                    context.IotHubRetryContext.ResetRemainingRetries();
                    return new DomainEvent[] { new ConnectionSuccess(_clock.Now()) };
                });
        }

        public override bool CanHandle(Context _, DomainEvent @event) => @event is ConnectionSuccess || @event is SystemTicked || @event is ConnectionFailed;

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