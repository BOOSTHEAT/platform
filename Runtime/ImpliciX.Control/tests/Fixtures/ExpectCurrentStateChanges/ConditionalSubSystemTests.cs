using System;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.PropertiesChangedHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectCurrentStateChanges
{
    [TestFixture]
    public class ConditionalSubSystemTests : SetupSubSystemTests
    {
        [Test]
        public void should_transition_when_condition_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.A, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.x2Urn, Temperature.Create(2f)));

            WithProperties((ConditionExample.x1Urn, Temperature.Create(0f)), (ConditionExample.x2Urn, Temperature.Create(2f)));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.B);
        }

        [Test]
        public void should_not_transition_when_condition_is_not_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.A, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.x1Urn, Temperature.Create(2f)));

            WithProperties((ConditionExample.x1Urn, Temperature.Create(4f)), (ConditionExample.x2Urn, Temperature.Create(2f)));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.A);
        }

        [Test]
        public void should_transition_when_equality_with_epsilon_condition_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.A, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.x3Urn, Temperature.Create(0f)));

            WithProperties((ConditionExample.x3Urn, Temperature.Create(0f)), (ConditionExample.x4Urn, Temperature.Create(0.1f)), (ConditionExample.epsilonUrn, Temperature.Create(0.2f)));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.B);
        }

        [Test]
        public void should_transition_when_equality_with_tolerance_condition_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.A, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.x3Urn, Temperature.Create(0.55f)));

            WithProperties((ConditionExample.x3Urn, Temperature.Create(0.55f)), (ConditionExample.x4Urn, Temperature.Create(0.5f)), (ConditionExample.toleranceUrn, Percentage.FromFloat(0.2f).Value));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.B);
        }

        [Test]
        public void should_transition_when_equality_condition_with_enum_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.B, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn, PowerSupply.On));
            WithProperties((ConditionExample.powerUrn, PowerSupply.On));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.C);
        }

        [Test]
        public void should_not_transition_when_conjunction_of_conditions_is_not_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.C, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn, PowerSupply.On));
            WithProperties(
                (ConditionExample.x3Urn, Temperature.Create(1f)),
                (ConditionExample.x4Urn, Temperature.Create(0.1f)),
                (ConditionExample.epsilonUrn, Temperature.Create(0.2f)),
                (ConditionExample.powerUrn, PowerSupply.On));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.C);
        }

        [Test]
        public void should_transition_when_conjunction_of_conditions_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.C, new ConditionExample());
            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn, PowerSupply.On));
            WithProperties(
                (ConditionExample.x3Urn, Temperature.Create(1f)),
                (ConditionExample.x4Urn, Temperature.Create(42f)),
                (ConditionExample.epsilonUrn, Temperature.Create(0.2f)),
                (ConditionExample.powerUrn, PowerSupply.On));

            sut.PlayEvents(changed1);

            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.D);
        }

        [Test]
        public void should_transition_when_InState_condition_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.C, new ConditionExample());

            var statesSet = EnumSequence.Create(new Enum[]
            {
                ConditionExample.OtherSubSystemState.SomeState,
                ConditionExample.OtherSubSystemState.SomeSubState
            });
            var changed1 = CreatePropertyChanged(
                TimeSpan.Zero,
                (ConditionExample.OtherSubSystemNode.Urn, statesSet)
            );
            WithProperties(
                (ConditionExample.OtherSubSystemNode.Urn, statesSet)
            );

            sut.PlayEvents(changed1);
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.D);
        }

        [Test]
        public void should_transition_when_Any_condition_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.D, new ConditionExample());

            WithProperties(
                (ConditionExample.x3Urn, Temperature.Create(1f)),
                (ConditionExample.x4Urn, Temperature.Create(42f)),
                (ConditionExample.epsilonUrn, Temperature.Create(0.2f)),
                (ConditionExample.powerUrn, PowerSupply.Off),
                (ConditionExample.powerUrn2, PowerSupply.Off)
            );

            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn2, PowerSupply.Off));

            sut.PlayEvents(changed1);
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.A);
        }

        [Test]
        public void should_transition_when_Any_condition_is_not_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.D, new ConditionExample());

            WithProperties(
                (ConditionExample.x3Urn, Temperature.Create(1f)),
                (ConditionExample.x4Urn, Temperature.Create(42f)),
                (ConditionExample.epsilonUrn, Temperature.Create(0.2f)),
                (ConditionExample.powerUrn, PowerSupply.On),
                (ConditionExample.powerUrn2, PowerSupply.On)
            );

            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn2, PowerSupply.On));

            sut.PlayEvents(changed1);
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.D);
        }

        [Test]
        public void should_transition_when_multiple_Any_condition_is_not_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.D, new ConditionExample());

            WithProperties(
                (ConditionExample.x3Urn, Temperature.Create(100f)),
                (ConditionExample.x4Urn, Temperature.Create(42f)),
                (ConditionExample.epsilonUrn, Temperature.Create(0.2f)),
                (ConditionExample.powerUrn, PowerSupply.On),
                (ConditionExample.powerUrn2, PowerSupply.Off),
                (ConditionExample.powerUrn3, PowerSupply.On)
            );

            var changed1 = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn2, PowerSupply.Off));

            sut.PlayEvents(changed1);
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.D);
        }

        [Test]
        public void should_transition_when_nested_any_condition_is_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.E, new ConditionExample());
            
            WithProperties(
                (ConditionExample.powerUrn,PowerSupply.On),
                (ConditionExample.powerUrn2,PowerSupply.Off),
                (ConditionExample.powerUrn3,PowerSupply.Off),
                (ConditionExample.powerUrn4,PowerSupply.On)
                );

            var changed = CreatePropertyChanged(TimeSpan.Zero, (ConditionExample.powerUrn, PowerSupply.On));

            sut.PlayEvents(changed);
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.A);
        }
        
        [Test]
        public void should_not_transition_when_nested_any_condition_is_not_satisfied()
        {
            var sut = CreateSut(ConditionExample.State.E, new ConditionExample());
            
            WithProperties(
                (ConditionExample.powerUrn,PowerSupply.On),
                (ConditionExample.powerUrn2,PowerSupply.On),
                (ConditionExample.powerUrn3,PowerSupply.On),
                (ConditionExample.powerUrn4,PowerSupply.On)
            );

            var changed = CreatePropertyChanged(TimeSpan.Zero, (examples.dummy, Percentage.FromFloat(0.2f).Value));

            sut.PlayEvents(changed);
            Check.That(sut.CurrentState).IsEqualTo(ConditionExample.State.E);
        }

        [Test]
        public void should_get_urns()
        {
            var sut = new ConditionDefinition();
            sut.Add(Op.Is, new[] {ConditionExample.powerUrn, ConditionExample.powerUrn2});
            sut.Add(Op.Is, new[] {ConditionExample.powerUrn3});
            sut.Add(Op.Is, new[] {ConditionExample.toleranceUrn});

            var urns = sut.GetUrns();

            var expected = new Urn[] {ConditionExample.powerUrn, ConditionExample.powerUrn2, ConditionExample.powerUrn3, ConditionExample.toleranceUrn};

            Check.That(urns).ContainsExactly(expected);
        }

                
        [Test]
        public void when_only_one_any_condition_should_get_urns()
        {
            var conditionDefinitionOperand1 = new ConditionDefinition();
            conditionDefinitionOperand1.Add(Op.Is, new Urn[] {ConditionExample.x1Urn});

            var conditionDefinitionOperand2 = new ConditionDefinition();
            conditionDefinitionOperand2.Add(Op.Is, new Urn[] {ConditionExample.x2Urn});

            var sut = new ConditionDefinition();
            sut.Add(Op.Any, new Urn[0], new[] {conditionDefinitionOperand1, conditionDefinitionOperand2});

            var urns = sut.GetUrns();

            var expected = new Urn[] {ConditionExample.x1Urn, ConditionExample.x2Urn};

            Check.That(urns).ContainsExactly(expected);
        }

        [Test]
        public void when_multiple_any_conditions_should_get_urns()
        {
            var conditionDefinitionOperand1 = new ConditionDefinition();
            conditionDefinitionOperand1.Add(Op.Is, new Urn[] {ConditionExample.x1Urn});

            var conditionDefinitionOperand2 = new ConditionDefinition();
            conditionDefinitionOperand2.Add(Op.Is, new Urn[] {ConditionExample.x2Urn});

            var sut = new ConditionDefinition();
            sut.Add(Op.Any, new Urn[0], new[] {conditionDefinitionOperand1, conditionDefinitionOperand2});
            sut.Add(Op.Is, new Urn[] {ConditionExample.powerUrn3});
            sut.Add(Op.Is, new Urn[] {ConditionExample.toleranceUrn});

            var urns = sut.GetUrns();

            var expected = new Urn[] {ConditionExample.x1Urn, ConditionExample.x2Urn, ConditionExample.powerUrn3, ConditionExample.toleranceUrn};

            Check.That(urns).ContainsExactly(expected);
        }
    }
}