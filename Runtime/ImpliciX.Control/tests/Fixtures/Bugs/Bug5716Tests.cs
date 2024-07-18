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
using static ImpliciX.Control.Tests.TestUtilities.ControlEventHelper;

namespace ImpliciX.Control.Tests.Fixtures.Bugs
{
    [TestFixture]
    public class Bug5716Tests : SetupSubSystemTests
    {
        [Test]
        public void bug_5716()
        {
            var toggle = EventCommandRequested(examples.timeout_subsystem.toggle, default, TestTime);
            var timeoutOccured = EventTimeoutOccured(examples.timeout_subsystem.timeoutUrn, TestTime);
            var sut = CreateSut(State.A, new Bug5716SubsystemDefinition());

            var actualEvents1 = sut.Activate();
      
            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.A),TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] {State.A}, TestTime),
                NotifyOnTimeoutRequested.Create(examples.timeout_subsystem.timeoutUrn, TestTime)
            };
            
            Check.That(actualEvents1).IsEqualTo(expectedEvents);

            var actualEvents2 = sut.PlayEvents(toggle);

            expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.B),TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] {State.B}, TestTime),
            };

            Check.That(actualEvents2).IsEqualTo(expectedEvents);
            
            var actualEvents3 = sut.PlayEvents(toggle);
            
            expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(examples.timeout_subsystem.state, SubsystemState.Create(State.A),TestTime),
                EventStateChanged(examples.timeout_subsystem.Urn, new Enum[] {State.A}, TestTime),
                NotifyOnTimeoutRequested.Create(examples.timeout_subsystem.timeoutUrn, TestTime)
            };
            
            var actualEvents4 = sut.PlayEvents(TimeoutOccured.Create(examples.timeout_subsystem.timeoutUrn, TestTime, actualEvents1[2].EventId));
            
            expectedEvents = new DomainEvent[] {};

            Check.That(actualEvents4).IsEqualTo(expectedEvents);
        }
    }
}