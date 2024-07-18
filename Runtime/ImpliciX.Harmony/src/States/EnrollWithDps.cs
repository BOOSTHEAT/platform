using System;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.Harmony.States
{
    public class EnrollWithDps : BaseState
    {
        public EnrollWithDps(IClock clock,
            Func<string, DpsSettings, IotHubSettings, Result<string>> registerWithDps) : base(nameof(EnrollWithDps))
        {
            _clock = clock;
            _registerWithDps = registerWithDps;
        }

        public Transition<BaseState, (Context, DomainEvent)> WhenEnrollmentIsSuccess(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is EnrollmentSuccess);

        public Transition<BaseState, (Context, DomainEvent)> WhenEnrollmentFailed(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is EnrollmentFailed);

        protected override DomainEvent[] OnEntry(Context context, DomainEvent _)
        {
            var registrationResult = _registerWithDps(context.DeviceId, context.DpsSettings, context.IotHubSettings);
            if (registrationResult.IsError)
            {
                Log.Error(registrationResult.Error.Message);
                context.DpsRetryContext.DecrementRemainingRetries();
                return new DomainEvent[] { new EnrollmentFailed(_clock.Now()) };
            }
            else
            {
                context.IotHubSettings.Uri = registrationResult.Value;
                Log.Information($"Device {context.DeviceId} registered to {context.IotHubSettings.Uri}");
                context.DpsRetryContext.ResetRemainingRetries();
                return new DomainEvent[] { new EnrollmentSuccess(_clock.Now()) };
            }
        }

        public override bool CanHandle(Context _, DomainEvent @event) => @event is EnrollmentSuccess || @event is EnrollmentFailed;

        private readonly IClock _clock;
        private readonly Func<string, DpsSettings, IotHubSettings, Result<string>> _registerWithDps;

        public class EnrollmentSuccess : PrivateDomainEvent
        {
            public EnrollmentSuccess(TimeSpan at) : base(Guid.NewGuid(), at)
            {
            }
        }

        public class EnrollmentFailed : PrivateDomainEvent
        {
            public EnrollmentFailed(TimeSpan at) : base(Guid.NewGuid(), at)
            {
            }
        }
    }
}