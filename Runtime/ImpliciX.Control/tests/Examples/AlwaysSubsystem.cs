using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.Functions;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using static ImpliciX.Language.Control.Condition;

namespace ImpliciX.Control.Tests.Examples
{
    public class AlwaysSubsystem : SubSystemDefinition<AlwaysSubsystem.PrivateState>
    {
        public enum PrivateState
        {
            A,
            Aa,
            Ab,
            B
        }

        [ValueObject]
        public enum PublicState
        {
            PublicA,
            Other
        }

        public AlwaysSubsystem()
        {
            // @formatter:off
            var self = examples.always;
            Subsystem(self)
                .Always
                    .Set(self.zprop, Polynomial1.Func, self.func, self.tprop)
                    .Set(self.always_public_state)
                        .With(PublicState.PublicA).When(InState(self, PrivateState.A))
                        .With(PublicState.Other).Otherwise()
                    .Set(self.xprop)
                        .With(Literal.Create("Value1")).When(LowerThan(self.propA, self.prop25))
                        .With(Literal.Create("Value2")).When(LowerThan(self.propA, self.prop25))
                        .With(Literal.Create("Value3")).When(LowerThan(self.propA, self.prop100))
                        .With(Literal.Create("Value4")).Otherwise()
                    .Set(self.yprop)
                        .With(examples.always.yprop_default).When(LowerThan(self.propC, self.prop25))
                        .With(Literal.Create("Otherwise")).Otherwise()
                .Initial(PrivateState.A)
                    .Define(PrivateState.A)
                        .Transitions
                            .WhenMessage(self._toB, default).Then(PrivateState.B)
                    .Define(PrivateState.Aa).AsInitialSubStateOf(PrivateState.A)
                        .Transitions
                            .WhenMessage(self._toAb, default).Then(PrivateState.Ab)
                    .Define(PrivateState.Ab).AsSubStateOf(PrivateState.A)
                        .Transitions
                            .WhenMessage(self._toAa, default).Then(PrivateState.Aa)
                    .Define(PrivateState.B);
            // @formatter:on
        }
    }

    public class WithSubsystem : SubSystemDefinition<WithSubsystem.PrivateState>
    {
        public enum PrivateState
        {
            A,
        }



        public WithSubsystem()
        {
            // @formatter:off
            var self = examples.always;
            Subsystem(self)
                .Always
                    .Set(self.yprop)
                        .With(examples.always.yprop_default).When(LowerThan(self.propC, self.prop25))
                        .With(Literal.Create("Otherwise")).Otherwise()
                .Initial(PrivateState.A)
                    .Define(PrivateState.A);
            // @formatter:on
        }
    }
}