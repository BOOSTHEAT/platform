using System;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.Control.Tests.TestUtilities.ControlEventHelper;    

namespace ImpliciX.Control.Tests.Fixtures.ExpectCurrentStateChanges
{
    [TestFixture]
    public class MessagesTests : SetupSubSystemTests
    {
        public CommandRequested ActivateSlave { get; }
        public CommandRequested ActivateMaster { get; }

        public MessagesTests()
        {
            ActivateSlave = EventCommandRequested(domotic.connected_fan_slave._activate, default, TestTime);
            ActivateMaster = EventCommandRequested(domotic.connected_fan_master._activate, default, TestTime);
        }

        [Test]
        public void when_receives_message()
        {
            var automaticStore = CreateSut(AutomaticStore.State.FullyClosed, new AutomaticStore());
            var OpenMid = EventCommandRequested(domotic.automatic_store._open, Position.Mid, TestTime);
            automaticStore.PlayEvents(OpenMid);

            Check.That(automaticStore.CurrentState).IsEqualTo(AutomaticStore.State.FullyClosed);

            var OpenFull = EventCommandRequested(domotic.automatic_store._open, Position.Full, TestTime);
            automaticStore.PlayEvents(OpenFull);

            Check.That(automaticStore.CurrentState).IsEqualTo(AutomaticStore.State.FullyOpen);
        }

        [Test]
        public void when_receives_two_messages_on_same_state()
        {
            var secondaryStore = CreateSut(SecondaryStore.State.Open, new SecondaryStore());
            var _close = EventCommandRequested(domotic.secondary_store._close, default, TestTime);
            var _open = EventCommandRequested(domotic.secondary_store._open, default, TestTime);

            secondaryStore.PlayEvents(_close);
            Check.That(secondaryStore.CurrentState).IsEqualTo(SecondaryStore.State.SemiOpen);

            secondaryStore.PlayEvents(_close);
            Check.That(secondaryStore.CurrentState).IsEqualTo(SecondaryStore.State.Closed);

            secondaryStore.PlayEvents(_open);
            Check.That(secondaryStore.CurrentState).IsEqualTo(SecondaryStore.State.Open);

            secondaryStore.PlayEvents(_close);
            Check.That(secondaryStore.CurrentState).IsEqualTo(SecondaryStore.State.SemiOpen);

            secondaryStore.PlayEvents(_open);
            Check.That(secondaryStore.CurrentState).IsEqualTo(SecondaryStore.State.Open);
        }

        [Test]
        public void when_sending_command_to_secondary_store_without_param()
        {
            var automaticStore = CreateSut(AutomaticStore.State.FullyClosed, new AutomaticStore());
            var mainStoreOpen = EventCommandRequested(domotic.automatic_store._open, Position.Full, TestTime);

            var expectedEvents = new DomainEvent[]
            {
                EventPropertyChanged(domotic.automatic_store.state, SubsystemState.Create(AutomaticStore.State.FullyOpen), TestTime),
                EventCommandRequested(domotic.secondary_store._open, default(NoArg), TestTime)
            };

            var resultingEvents = automaticStore.PlayEvents(mainStoreOpen);

            Check.That(automaticStore.CurrentState).IsEqualTo(AutomaticStore.State.FullyOpen);
            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void when_sending_command_to_secondary_store_with_param()
        {
            var automaticStore = CreateSut(AutomaticStore.State.ClosureInProgress, new AutomaticStore());
            var close = EventCommandRequested(domotic.automatic_store._closed, default(NoArg), TestTime);

            var expectedEvents = new DomainEvent[]
            {
                EventCommandRequested(domotic.secondary_store._closeWithParam, HowMuch.Full, TestTime)
            };

            var resultingEvents = automaticStore.PlayEvents(close);

            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void when_subsystem_change_state_is_send()
        {
            var connectedFanMaster = CreateSut(ConnectedFanMaster.State.Off, new ConnectedFanMaster());
            var masterFanStart = EventCommandRequested(domotic.connected_fan_master._start, default, TestTime);
            var resultingEvents = connectedFanMaster.PlayEvents(ActivateMaster, masterFanStart);
            DomainEvent[] expectedEvents = {
                EventPropertyChanged(domotic.connected_fan_master.state, SubsystemState.Create(ConnectedFanMaster.State.Off), TestTime),
                EventPropertyChanged(domotic.connected_fan_master.state, SubsystemState.Create(ConnectedFanMaster.State.On), TestTime)
            };
            Check.That(resultingEvents).Contains(expectedEvents);
        }

        [Test]
        public void when_subsystem_receive_change_state_from_another_subsystem()
        {
            var connectedFanSlave = CreateSut(ConnectedFanSlave.State.SlaveOff, new ConnectedFanSlave());
        
            WithProperties(
              (domotic.connected_fan_master.Urn, EnumSequence.Create(new Enum[]{ConnectedFanMaster.State.On}))
            );
            
            connectedFanSlave.PlayEvents(ActivateSlave, EventStateChanged(domotic.connected_fan_master.Urn, new Enum[]{ConnectedFanMaster.State.On}, TestTime));
        
            Check.That(connectedFanSlave.CurrentState).IsEqualTo(ConnectedFanSlave.State.SlaveOn);
        }
        
                
        [Test]
        public void when_subsystem_wait_already_send_state_from_another_subsystem_to_transition()
        {
            var connectedFanSlave = CreateSut(ConnectedFanSlave.State.SlaveOff, new ConnectedFanSlave());
            WithProperties(
                (domotic.connected_fan_master.Urn, EnumSequence.Create(new Enum[]{ConnectedFanMaster.State.On}))
            );
            
            var randomCommand = EventCommandRequested(domotic.automatic_store._close, default(NoArg), TestTime);
            connectedFanSlave.PlayEvents(ActivateSlave,randomCommand);
        
            Check.That(connectedFanSlave.CurrentState).IsEqualTo(ConnectedFanSlave.State.SlaveOn);
        }

        [Test]
        public void should_be_able_to_handle_commands_having_an_urn_corresponding_to_a_declared_onmessage()
        {
            var connectedFanMaster = CreateSut(ConnectedFanMaster.State.Off, new ConnectedFanMaster());
            var requested = EventCommandRequested(domotic.connected_fan_master._start, default, TestTime);
            var canExecute = connectedFanMaster.CanHandle(requested);
            Check.That(canExecute).IsTrue();
        }

        [Test]
        public void should_not_be_able_to_handle_commands_not_having_an_urn_corresponding_to_a_declared_onmessage()
        {
            var connectedFanMaster = CreateSut(ConnectedFanMaster.State.Off, new ConnectedFanMaster());
            var requested = CommandRequested.Create(CommandUrn<NoArg>.Build("not", "a", "defined", "message", "urn"), default, TestTime);
            var canExecute = connectedFanMaster.CanHandle(requested);
            Check.That(canExecute).IsFalse();
        }

        [Test]
        public void on_exit_send_command_with_param()
        {
            var sut = CreateSut(Computer.State.Booted, new Computer());
            var PowerOff = EventCommandRequested(devices.computer._powerOff, default(NoArg), TestTime);
            var resultingEvents = sut.PlayEvents(PowerOff);
            var expectedEvents = EventCommandRequested(devices.computer._buzz, Switch.On, TestTime);
            Check.That(resultingEvents).Contains(expectedEvents);
        }
    }
}