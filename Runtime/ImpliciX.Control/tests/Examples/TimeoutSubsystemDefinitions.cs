using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public enum State
    {
        A,
        B,
        C
    }

    public class Bug5716SubsystemDefinition : SubSystemDefinition<State>
    {
        public Bug5716SubsystemDefinition()
        {
            // @formatter:off
            Subsystem(examples.timeout_subsystem)
                .Initial(State.A)
                .Define(State.A)
                   .OnEntry
                        .StartTimer(examples.timeout_subsystem.timeoutUrn)
                    .Transitions
                        .WhenMessage(examples.timeout_subsystem.toggle).Then(State.B)
                        .WhenTimeout(examples.timeout_subsystem.timeoutUrn).Then(State.C)
                .Define(State.B)
                    .Transitions
                        .WhenMessage(examples.timeout_subsystem.toggle).Then(State.A)
                .Define(State.C);
            // @formatter:on
        }
    }

    public class TimeoutSubsystemAThenB : SubSystemDefinition<State>
    {
        public TimeoutSubsystemAThenB()
        {
            // @formatter:off
            Subsystem(examples.timeout_subsystem)
                .Initial(State.A)
                .Define(State.A)
                   .OnEntry
                        .StartTimer(examples.timeout_subsystem.timeoutUrn)
                    .Transitions
                        .WhenMessage(examples.timeout_subsystem.toggle).Then(State.B)
                .Define(State.B)
                    .Transitions
                        .WhenTimeout(examples.timeout_subsystem.timeoutUrn).Then(State.C)
                .Define(State.C);
            // @formatter:on
        }
    }

    public class TimeoutSubsystemComposite : SubSystemDefinition<State>
    {
        public TimeoutSubsystemComposite()
        {
            // @formatter:off
            Subsystem(examples.timeout_subsystem)
                .Initial(State.A)
                .Define(State.A)
                   .OnEntry
                        .StartTimer(examples.timeout_subsystem.timeoutUrn)
                .Define(State.B).AsInitialSubStateOf(State.A)
                    .Transitions
                        .WhenTimeout(examples.timeout_subsystem.timeoutUrn).Then(State.C)
                .Define(State.C).AsSubStateOf(State.A);
            // @formatter:on
        }
    }

    public class TimeoutSubsystemA : SubSystemDefinition<State>
    {
        public TimeoutSubsystemA()
        {
            // @formatter:off
            Subsystem(examples.timeout_subsystem_a)
                .Initial(State.A)
                .Define(State.A)
                   .OnEntry
                        .StartTimer(examples.timeout_subsystem_a.timeoutUrn);
            // @formatter:on
        }
    }

    public class TimeoutSubsystemB : SubSystemDefinition<State>
    {
        public TimeoutSubsystemB()
        {
            // @formatter:off
            Subsystem(examples.timeout_subsystem_b)
                .Initial(State.B)
                .Define(State.B)
                    .Transitions
                        .WhenTimeout(examples.timeout_subsystem_a.timeoutUrn).Then(State.C)
                .Define(State.C);
            // @formatter:on
        }
    }
}

