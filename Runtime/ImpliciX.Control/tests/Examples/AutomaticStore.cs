using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples
{
    public class AutomaticStore : SubSystemDefinition<AutomaticStore.State>
    {
        public enum State
        {
            FullyOpen,
            FullyClosed,
            ClosureInProgress
        }

        public AutomaticStore()
        {
            Subsystem(domotic.automatic_store)
                .Initial(State.FullyOpen)
                .Define(State.FullyOpen)
                .OnEntry
                .Set(domotic.secondary_store._open)
                .Transitions
                .WhenMessage(domotic.automatic_store._close).Then(State.ClosureInProgress)
                .Define(State.ClosureInProgress)
                .OnEntry
                .StartTimer(domotic.automatic_store.Timer)
                .Set(domotic.automatic_store._toDriver)
                .Transitions
                .WhenMessage(domotic.automatic_store._closed).Then(State.FullyClosed)
                .WhenTimeout(domotic.automatic_store.Timer).Then(State.FullyClosed)
                .Define(State.FullyClosed)
                .OnEntry
                .Set(domotic.secondary_store._closeWithParam, HowMuch.Full)
                .Set(domotic.automatic_store.light._switch.command, Switch.On)
                .Set(domotic.automatic_store.light._change_color, domotic.automatic_store.light_settings.default_color)
                .Set(domotic.automatic_store.light_settings.default_intensity, constant.parameters.percentage.one)
                .OnState
                .Set(domotic.automatic_store.light._intensity, domotic.automatic_store.light_settings.intensity)
                .Set(domotic.automatic_store.light_settings.intensity, domotic.automatic_store.light_settings.default_intensity)
                .Set(domotic.automatic_store.light._change_color, domotic.automatic_store.light_settings.default_color)
                .Transitions
                .WhenMessage(domotic.automatic_store._open, Position.Full).Then(State.FullyOpen);


        }
    }
}

