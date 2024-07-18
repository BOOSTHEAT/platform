using System;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Harmony.States
{
    public abstract class WaitBeforeRetry : BaseState
    {
        private readonly IClock _clock;
        private readonly PropertyUrn<Duration> _retryDelayUrn;
        private readonly HarmonyRetryContext _retryContext;


        protected WaitBeforeRetry(string name, IClock clock, PropertyUrn<Duration> retryDelayUrn,
            HarmonyRetryContext retryContext) : base(name)
        {
            _clock = clock;
            _retryDelayUrn = retryDelayUrn;
            _retryContext = retryContext;
        }

        public Transition<BaseState, (Context, DomainEvent)> WhenConnectionIsDisabled(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is ConnectionDisabled);

        public Transition<BaseState, (Context, DomainEvent)> WhenTimeoutOccured(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is TimeoutOccured);

        protected override DomainEvent[] OnEntry(Context context, DomainEvent _)
        {
            context.AzureIoTHubAdapter?.Dispose();
            if (_retryContext.RemainingAttempts < 0)
            {
                return new DomainEvent[]
                {
                    new ConnectionDisabled(_clock.Now())
                };
            }
            else
                return new DomainEvent[]
                {
                    NotifyOnTimeoutRequested.Create(_retryDelayUrn, _clock.Now())
                };
        }

        protected override DomainEvent[] OnState(Context context, DomainEvent @event) =>
            @event is TimeoutOccured timeoutOccured && timeoutOccured.TimerUrn == _retryDelayUrn
                ? new DomainEvent[] { new ConnectionDisabled(_clock.Now()) }
                : base.OnState(context, @event);

        public override bool CanHandle(Context _, DomainEvent @event) =>
            @event is TimeoutOccured || @event is ConnectionDisabled;

        protected override DomainEvent[] OnExit(Context context, DomainEvent _) => Array.Empty<DomainEvent>();

        public class ConnectionDisabled : PrivateDomainEvent
        {
            public ConnectionDisabled(TimeSpan at) : base(Guid.NewGuid(), at)
            {
            }
        }
    }

    public class WaitBeforeRetryEnrollment : WaitBeforeRetry
    {
        public WaitBeforeRetryEnrollment(IClock clock, PropertyUrn<Duration> retryDelayUrn,
            HarmonyRetryContext retryContext) : base(nameof(WaitBeforeRetryEnrollment), clock, retryDelayUrn,
            retryContext)
        {
        }
    }

    public class WaitBeforeRetryConnect : WaitBeforeRetry
    {
        public WaitBeforeRetryConnect(IClock clock, PropertyUrn<Duration> retryDelayUrn,
            HarmonyRetryContext retryContext) : base(nameof(WaitBeforeRetryConnect), clock, retryDelayUrn,
            retryContext)
        {
        }
    }
}