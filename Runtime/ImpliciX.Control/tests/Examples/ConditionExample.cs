using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using static ImpliciX.Language.Control.Condition;

namespace ImpliciX.Control.Tests.Examples
{
    public class ConditionExample : SubSystemDefinition<ConditionExample.State>
    {
        public enum State
        {
            A,
            B,
            C,
            D,
            E
        }

        public enum OtherSubSystemState
        {
            SomeState,
            SomeSubState
        }

        public new static SubSystemNode SubSystemNode => new SubSystemNode(nameof(ConditionExample), null);
        public static SubSystemNode OtherSubSystemNode => new SubSystemNode("OtherSubsystemNode", null);
        public static PropertyUrn<Temperature> x1Urn => PropertyUrn<Temperature>.Build("x1", "measure");
        public static PropertyUrn<Temperature> x2Urn => PropertyUrn<Temperature>.Build("x2", "measure");
        public static PropertyUrn<Temperature> x3Urn => PropertyUrn<Temperature>.Build("x3", "measure");
        public static PropertyUrn<Temperature> x4Urn => PropertyUrn<Temperature>.Build("x4", "measure");
        public static PropertyUrn<Temperature> epsilonUrn => PropertyUrn<Temperature>.Build("x5", "epsilon");
        public static PropertyUrn<Percentage> toleranceUrn => PropertyUrn<Percentage>.Build("x6", "tolerance");
        public static PropertyUrn<PowerSupply> powerUrn => PropertyUrn<PowerSupply>.Build("power1", "power");
        public static PropertyUrn<PowerSupply> powerUrn2 => PropertyUrn<PowerSupply>.Build("power2", "power");
        public static PropertyUrn<PowerSupply> powerUrn3 => PropertyUrn<PowerSupply>.Build("power3", "power");
        public static PropertyUrn<PowerSupply> powerUrn4 => PropertyUrn<PowerSupply>.Build("power4", "power");

        public ConditionExample()
        {
            // @formatter:off
            Subsystem(SubSystemNode)
                .Initial(State.A)
                .Define(State.A)
                    .Transitions
                        .When(LowerThan(x1Urn,x2Urn)).Then(State.B)
                        .When(EqualWithEpsilon(x3Urn,x4Urn, epsilonUrn)).Then(State.B)
                        .When(EqualWithTolerance(x3Urn,x4Urn, toleranceUrn)).Then(State.B)
                .Define(State.B)
                    .Transitions
                        .When(Is(powerUrn,PowerSupply.On)).Then(State.C)
                .Define(State.C)
                    .Transitions
                        .When(Is(powerUrn,PowerSupply.On).And(LowerMinusEpsilon(x3Urn,x4Urn, epsilonUrn))).Then(State.D)
                        .When(InState(OtherSubSystemNode, OtherSubSystemState.SomeState)).Then(State.D)
                .Define(State.D)
                    .Transitions
                        .When(Any(Is(powerUrn,PowerSupply.Off), Is(powerUrn2, PowerSupply.Off))
                            .And(Any(LowerMinusEpsilon(x3Urn,x4Urn, epsilonUrn), Is(powerUrn3,PowerSupply.Off))))
                        .Then(State.A)
                .Define(State.E)
                    .Transitions
                        .When(Any(Is(powerUrn,PowerSupply.Off),Is(powerUrn2,PowerSupply.Off).And(Any(Is(powerUrn3,PowerSupply.Off),Is(powerUrn4,PowerSupply.Off)))))
                        .Then(State.A)
                        // (powerUrn or (powerUrn2 and (powerUrn3 or powerUrn4))
                ;
            // @formatter:on
        }
    }
}