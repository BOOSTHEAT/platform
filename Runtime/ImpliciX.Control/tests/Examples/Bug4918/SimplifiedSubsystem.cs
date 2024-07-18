using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples.Bug4918
{
    public class SubsystemA : SubSystemDefinition<SubsystemA.State>
    {
        public enum State
        {
            A1,
            A2,
            A3,
        }
        
        // @formatter:off
        public SubsystemA()
        {
            Subsystem(examples.subsystem_a)
                .Initial(State.A1)
                    .Define(State.A1)
                        .Transitions
                            .When(Condition.GreaterThan(examples.subsystem_a.needs, examples.subsystem_a.threshold)).Then(State.A2)
                    .Define(State.A2)
                        .OnEntry
                            .Set(examples.subsystem_b._start)
                        .Transitions
                            .When(Condition.LowerThan(examples.subsystem_a.needs, examples.subsystem_a.threshold)).Then(State.A3)
                    .Define(State.A3)
                        .OnState
                            .Set(examples.dummy, examples.percentage)
                        .Transitions
                            .When(Condition.InState(examples.subsystem_b, SubsystemB.State.B1)).Then(State.A1)
                ;
        }
        // @formatter:on
    }
    public class SubsystemB : SubSystemDefinition<SubsystemB.State>
    {
        public enum State
        {
            B1,
            B2,
        }

        // @formatter:off
        public SubsystemB()
        {
            Subsystem(examples.subsystem_b)
                .Initial(State.B1)
                    .Define(State.B1)
                        .Transitions
                            .WhenMessage(examples.subsystem_b._start).Then(State.B2)
                    .Define(State.B2)
                        .Transitions
                            .WhenMessage(examples.subsystem_b._stop).Then(State.B1);

        }
        // @formatter:on
    }

    public class SubsystemAFix : SubSystemDefinition<SubsystemAFix.State>
    {
        public enum State
        {
            A1,
            A2,
            A3,
        }
        
        // @formatter:off
        public SubsystemAFix()
        {
            Subsystem(examples.subsystem_a)
                .Initial(State.A1)
                    .Define(State.A1)
                        .Transitions
                            .When(Condition.GreaterThan(examples.subsystem_a.needs, examples.subsystem_a.threshold))
                                .Then(State.A2)
                    .Define(State.A2)
                        .OnEntry
                            .Set(examples.subsystem_b.sync_state, subsystem_b.SyncState.StartRequested)
                        .Transitions
                            .When(Condition.LowerThan(examples.subsystem_a.needs, examples.subsystem_a.threshold)).Then(State.A3)
                    .Define(State.A3)
                        .OnState
                            .Set(examples.dummy, examples.percentage)
                        .Transitions
                            .When(Condition.Is(examples.subsystem_b.sync_state, subsystem_b.SyncState.Stopped)).Then(State.A1)
                ;
        }
        // @formatter:on
    }

    public class SubsystemBFix : SubSystemDefinition<SubsystemBFix.State>
    {
        public enum State
        {
            B1,
            B2,
        }

        // @formatter:off
        public SubsystemBFix()
        {
            Subsystem(examples.subsystem_b)
                .Initial(State.B1)
                    .Define(State.B1)
                        .OnEntry
                            .Set(examples.subsystem_b.sync_state, subsystem_b.SyncState.Stopped)
                        .OnExit
                            .Set(examples.subsystem_b.sync_state, subsystem_b.SyncState.Started)
                        .Transitions
                            .When(Condition.Is(examples.subsystem_b.sync_state, subsystem_b.SyncState.StartRequested)).Then(State.B2)
                    .Define(State.B2)
                        .Transitions
                            .When(Condition.Is(examples.subsystem_b.sync_state, subsystem_b.SyncState.StopRequested)).Then(State.B1)
             ;
        }
        // @formatter:on
    }
}