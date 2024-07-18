using System;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class SubsystemWithPeriodicComputationTests : SetupSubSystemTests
    {
        [Test]
        public void should_compute_when_system_ticked_occured()
        {
            var sut = CreateSut(SubsystemWithPeriodicComputation.State.A, new SubsystemWithPeriodicComputation());

            var functionDefinition = new FunctionDefinition(new[] {("Kd", -3f)});

            WithProperties(
                (examples.subsystemWithPeriodicComputation.functionDefinition, functionDefinition),
                (examples.subsystemWithPeriodicComputation.initialValue, Temperature.Create(4f)));

            sut.PlayEvents(
                EventSystemTicked(1000, TimeSpan.FromMilliseconds(1000))
            );

            var resultingEvents = sut.PlayEvents(
                EventSystemTicked(1000, TimeSpan.FromMilliseconds(1000))
            );

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(new (Urn urn, object value)[]
                {
                    (examples.subsystemWithPeriodicComputation.propA, Temperature.Create(1f))
                }, TestTime)
            };

            Check.That(resultingEvents).ContainsExactly(expectedEvents);
        }
        
        
        [Test]
        public void should_not_compute_when_system_ticked_occured()
        {
            var sut = CreateSut(SubsystemWithPeriodicComputation.State.A, new SubsystemWithPeriodicComputation());

            var functionDefinition = new FunctionDefinition(new[] {("Kd", -3f)});

            WithProperties(
                (examples.subsystemWithPeriodicComputation.functionDefinition, functionDefinition),
                (examples.subsystemWithPeriodicComputation.initialValue, Temperature.Create(4f)));

            var resultingEvents = sut.PlayEvents(
                EventPropertyChanged(examples.subsystemWithPeriodicComputation.initialValue, 5f, TimeSpan.Zero)
            );
            Check.That(resultingEvents.FilterEvents<PropertiesChanged>(examples.subsystemWithPeriodicComputation.propA)).IsEmpty();
        }
    }
}