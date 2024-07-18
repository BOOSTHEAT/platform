using System;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using TimeZone = ImpliciX.Language.Model.TimeZone;

namespace ImpliciX.Harmony.States
{
    public class GatherConfiguration : BaseState
    {
        public GatherConfiguration(HarmonyModuleDefinition moduleDefinition) : base(nameof(GatherConfiguration))
        {
            _moduleDefinition = moduleDefinition;
        }

        private readonly HarmonyModuleDefinition _moduleDefinition;

        protected override DomainEvent[] OnState(Context context, DomainEvent @event) =>
            @event is PropertiesChanged propertiesChanged
                ? Handle(context, propertiesChanged)
                : base.OnState(context, @event);

        public override bool CanHandle(Context _, DomainEvent @event) => @event is PropertiesChanged || @event is GatheringComplete;

        public Transition<BaseState, (Context, DomainEvent)> WhenGatheringIsComplete(BaseState to)
            => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is GatheringComplete);

        public DomainEvent[] Handle(Context context, PropertiesChanged @event)
        {
            @event.GetPropertyValue<Literal>(_moduleDefinition.DeviceId)
                .Tap(v => context.DeviceId = v.ToString());

            @event.GetPropertyValue<Literal>(_moduleDefinition.IDScope)
                .Tap(v => context.DpsSettings.IdScope = v.ToString());

            @event.GetPropertyValue<Literal>(_moduleDefinition.SymmetricKey)
                .Tap(v => context.IotHubSettings.SymmetricKey = v.ToString());

            @event.GetPropertyValue<Literal>(_moduleDefinition.DeviceSerialNumber)
                .Tap(v => context.SerialNumber = v.ToString());

            @event.GetPropertyValue<SoftwareVersion>(_moduleDefinition.ReleaseVersion)
                .Tap(v => context.ReleaseVersion = v.ToString());

            @event.GetPropertyValue<TimeZone>(_moduleDefinition.UserTimeZone)
                .Tap(v => context.UserTimeZone = v.ToString());

            var gatheringComplete = new[]
            {
                context.DeviceId,
                context.SerialNumber,
                context.DpsSettings.IdScope,
                context.IotHubSettings.SymmetricKey,
                context.ReleaseVersion,
                context.UserTimeZone
            }.All(v => !string.IsNullOrEmpty(v));

            return gatheringComplete
                ? new DomainEvent[] { new GatheringComplete(@event.At) }
                : Array.Empty<DomainEvent>();
        }

        public class GatheringComplete : PrivateDomainEvent
        {
            public GatheringComplete(TimeSpan at) : base(Guid.NewGuid(), at)
            {
            }
        }
    }
}