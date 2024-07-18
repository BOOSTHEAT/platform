using System;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Harmony.States
{
    public class Disabled : BaseState
    {
        public Disabled(IClock clock, PropertyUrn<Duration> enableDelayUrn) : base(nameof(Disabled))
        {
            _clock = clock;
            _enableDelayUrn = enableDelayUrn;
        }

        public Transition<BaseState, (Context, DomainEvent)> WhenTimeoutOccured(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is TimeoutOccured);

        protected override DomainEvent[] OnEntry(Context context, DomainEvent _) =>
            new DomainEvent[]
            {
                NotifyOnTimeoutRequested.Create(_enableDelayUrn, _clock.Now())
            };

        protected override DomainEvent[] OnState(Context context, DomainEvent @event) =>
            @event is TimeoutOccured timeoutOccured && timeoutOccured.TimerUrn == _enableDelayUrn
                ? new DomainEvent[] { new Enabled(_clock.Now()) }
                : base.OnState(context, @event);

        public override bool CanHandle(Context _, DomainEvent @event) => @event is TimeoutOccured || @event is Enabled;

        private readonly IClock _clock;
        private readonly PropertyUrn<Duration> _enableDelayUrn;

        public class Enabled : PrivateDomainEvent
        {
            public Enabled(TimeSpan at) : base(Guid.NewGuid(), at)
            {
            }
        }
    }
}