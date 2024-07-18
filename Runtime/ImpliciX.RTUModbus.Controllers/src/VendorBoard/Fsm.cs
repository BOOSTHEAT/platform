using System;
using ImpliciX.Driver.Common;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Helpers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using static ImpliciX.SharedKernel.FiniteStateMachine.FSMHelper;
using static ImpliciX.RTUModbus.Controllers.VendorBoard.State;

namespace ImpliciX.RTUModbus.Controllers.VendorBoard
{
    public class Fsm
    {
        public static FSM<State, DomainEvent, DomainEvent> Create(IBoardSlave boardSlave, DomainEventFactory domainEventFactory)
        {
            var fsmActions = new FsmActions(boardSlave, domainEventFactory);
            var stateDefinitions = new[]
            {
                State(Disabled, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        LogStateOnEntry(boardSlave, Disabled);
                        return new DomainEvent[]
                        {
                            domainEventFactory.HealthyCommunicationOccured(boardSlave.DeviceNode, new CommunicationDetails(0,0)),
                        };
                    },
                }),
                State(Working, new Func<DomainEvent, DomainEvent[]>[]{}),
                InitialSubState(Regulation, Working,
                    new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ => LogStateOnEntry(boardSlave, Regulation),
                    },
                    new Func<DomainEvent, DomainEvent[]>[] 
                    {
                        When(@event => @event.Is<SystemTicked>())
                            .Then(@event => fsmActions.ReadMainFirmware((SystemTicked)@event)),
                        When(@event => @event.Is<CommandRequested>())
                            .Then(@event => fsmActions.ExecuteSlaveCommand((CommandRequested)@event))
                    })
            };

            var transitions = new Transition<State, DomainEvent>[]
            {
                Transition<State, DomainEvent>(Working, Disabled,@event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Disabled)),
                Transition<State, DomainEvent>(Disabled, Working,@event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Enabled)),
            };

            return new FSM<State, DomainEvent, DomainEvent>(Working, stateDefinitions, transitions);
        } 
        
        private static DomainEvent[] LogStateOnEntry(IBoardSlave slave, State state)
        {
            Log.Information("[{@slave}] enters in state: {@state}", slave.Name, state);
            return Array.Empty<DomainEvent>();
        }
    }
}