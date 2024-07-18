using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class SecondaryStore : SubSystemDefinition<SecondaryStore.State>
    {
        public enum State
        {
            Open,
            SemiOpen,
            Closed
        }

        public SecondaryStore()
        {
            Subsystem(domotic.secondary_store)
                .Initial(State.Open)
                .Define(State.Open)
                    .Transitions
                        .WhenMessage(domotic.secondary_store._close).Then(State.SemiOpen)
                .Define(State.SemiOpen)
                    .Transitions
                        .WhenMessage(domotic.secondary_store._close).Then(State.Closed)
                        .WhenMessage(domotic.secondary_store._open).Then(State.Open)
                .Define(State.Closed)
                    .Transitions
                        .WhenMessage(domotic.secondary_store._open).Then(State.Open);
        }
    }
}