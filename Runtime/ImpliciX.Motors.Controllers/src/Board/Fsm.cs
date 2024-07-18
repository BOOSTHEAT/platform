using System;
using ImpliciX.Driver.Common;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using static ImpliciX.SharedKernel.FiniteStateMachine.FSMHelper;
using static ImpliciX.Motors.Controllers.Board.State;

namespace ImpliciX.Motors.Controllers.Board
{
    public class Fsm : FSM<State, DomainEvent, DomainEvent[]>
    {
        public static FSM<State, DomainEvent, DomainEvent> Create(IBoardSlave boardSlave, FsmActions fsmActions)
        {
            var stateDefinitions = new[]
            {
                State(Disabled, onEntry: new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ => LogStateOnEntry(boardSlave, Disabled),
                    _ => fsmActions.PublishStoppedStatus(),
                    _ => fsmActions.TurnPowerSupplyOff()
                }),
                State(Enabled, new Func<DomainEvent, DomainEvent[]>[0] { }),
                InitialSubState(Stopped, Enabled,new Func<DomainEvent, DomainEvent[]>[]
                {
                    _=> fsmActions.PublishStoppedStatus()
                }),
                InitialSubState(StoppedNominal, Stopped,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ => LogStateOnEntry(boardSlave,StoppedNominal),
                        _ => fsmActions.TurnPowerSupplyOff()
                    },
                    onState: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        When(@event => @event.Is<CommandRequested>()).Then(@event => fsmActions.IgnoreCommand((CommandRequested)@event))
                    }),
                SubState(PowerFailure, Stopped,new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ => LogStateOnEntry(boardSlave,PowerFailure),
                }),

                SubState(Starting, Enabled, onEntry: new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ => LogStateOnEntry(boardSlave,Starting),
                    _ => fsmActions.TurnPowerSupplyOn(),
                }, onState: new[]
                {
                    When(@event => @event.IsSlaveCommand(boardSlave)).Then(@event => fsmActions.IgnoreCommand((CommandRequested)@event))
                }),

                SubState(Started, Enabled,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ => LogStateOnEntry(boardSlave,Started)
                    },
                    onState: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        When(@event => @event.Is<SystemTicked>()).Then(@event => fsmActions.ReadMainFirmware((SystemTicked) @event)),
                        When(@event => @event.IsSlaveCommand(boardSlave)).Then(@event => fsmActions.ExecuteCommand((CommandRequested) @event)),
                    }
                )
            };

            var transitions = new[]
            {
                Transition<State, DomainEvent>(Stopped, Starting, fsmActions.IsMotorsSwitchStart),
                Transition<State, DomainEvent>(Enabled, StoppedNominal, fsmActions.IsMotorsSwitchStop),
                Transition<State, DomainEvent>(Starting, Started, fsmActions.IsStartTimeoutOccured),
                Transition<State, DomainEvent>(Enabled, PowerFailure, fsmActions.IsHeatPumpRestarted),
                Transition<State, DomainEvent>(Disabled, Enabled, @event => @event.isPresence(Presence.Enabled, boardSlave.HardwareDevice.presence)),
                Transition<State, DomainEvent>(Enabled, Disabled, @event => @event.isPresence(Presence.Disabled, boardSlave.HardwareDevice.presence)),
            };

            return new FSM<State, DomainEvent, DomainEvent>(StoppedNominal, stateDefinitions, transitions);
        }



        private Fsm(State initialState, StateDefinition<State, DomainEvent, DomainEvent[]>[] states, Transition<State, DomainEvent>[] transitions) : base(initialState, states, transitions)
        {
        }
        
        private static DomainEvent[] LogStateOnEntry(IBoardSlave slave, State state)
        {
            Log.Information("[{@slave}] enters in state: {@state}", slave.Name, state);
            return Array.Empty<DomainEvent>();
        }
    }
}