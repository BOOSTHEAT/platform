using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.Functions;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class SubsystemWithPeriodicComputation : SubSystemDefinition<SubsystemWithPeriodicComputation.State>
    {
        public enum State
        {
            A
        }

        public SubsystemWithPeriodicComputation()
        {
            var subsystem = examples.subsystemWithPeriodicComputation;
            
            Subsystem(examples.subsystemWithPeriodicComputation)
                .Initial(State.A)
                .Define(State.A)
                    .OnState
                        .Set(subsystem.propB, subsystem.initialValue)
                        .SetPeriodical(subsystem.propA, Displace.Func, subsystem.functionDefinition, subsystem.initialValue)
                ;
        }
    }
}