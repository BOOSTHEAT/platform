using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Helpers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.SharedKernel.Tools;
using static ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;
using static ImpliciX.SharedKernel.FiniteStateMachine.FSMHelper;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public static class Fsm
    {
        public static FSM<State, DomainEvent, DomainEvent> Create(IBrahmaBoardSlave boardSlave,
            DomainEventFactory domainEventFactory)
        {
            var gasBurner = boardSlave.GenericBurner;
            var fsmActions = new FsmActions(boardSlave, domainEventFactory);
            var stateDefinitions = new[]
            {
                State(DisabledNotAvailable, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, DisabledNotAvailable);
                        return new DomainEvent[] { };
                    }
                }),
                State(DisabledAvailable, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, DisabledAvailable);
                        return new DomainEvent[] { };
                    }
                }),
                State(EnabledAvailable, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, EnabledAvailable);
                        return new DomainEvent[] { };
                    }
                }, new[]
                {
                    When(@event => @event.IsSystemTicked())
                        .Then(@event => fsmActions.ReadMainFirmware((SystemTicked)@event))
                }),
                State(EnabledNotAvailable, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, EnabledNotAvailable);
                        return new DomainEvent[] { };
                    }
                }),
                InitialSubState(StandBy, EnabledAvailable, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, StandBy);
                        return new DomainEvent[]
                            { domainEventFactory.PropertiesChanged(boardSlave.DeviceNode.Urn.Plus(boardSlave.Name), gasBurner.status, GasBurnerStatus.NotSupplied) };
                    }
                }),
                SubState(Running, EnabledAvailable, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Running);
                        return new DomainEvent[] { };
                    }
                }, onExit: new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ => fsmActions.PowerOff()
                }),
                InitialSubState(Supplying, Running, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Supplying);
                        return new[]
                        {
                            domainEventFactory.NotifyOnTimeoutRequested(gasBurner.ignition_settings
                                .ignition_supplying_delay)
                        };
                    },
                    _ => fsmActions.PowerOn()
                }),
                SubState(Supplied, Running, Array.Empty<Func<DomainEvent, DomainEvent[]>>(),
                    new[]
                    {
                        When(@event => @event.IsFanThrottle(gasBurner))
                            .Then(@event => fsmActions.FanThrottle((CommandRequested)@event))
                    }),
                InitialSubState(Resetting, Supplied, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Resetting);
                        return fsmActions.ResetBrahma();
                    }
                }),
                SubState(ResetPerformed, Supplied, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, ResetPerformed);
                        return fsmActions.StopBrahmaReset();
                    }
                }),

                SubState(Ready, Supplied, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Ready);
                        return new DomainEvent[]
                        {
                        };
                    }
                }, new[]
                {
                    When(@event => @event.Is<SystemTicked>()).Then(_ => fsmActions.DetectStatus())
                }),

                SubState(Faulted, Supplied, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Faulted);
                        return new DomainEvent[]
                        {
                            domainEventFactory.PropertiesChanged(gasBurner.status, GasBurnerStatus.Faulted)
                        };
                    }
                }),

                InitialSubState(CheckReadiness, Ready, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, CheckReadiness);
                        return new DomainEvent[]
                        {
                        };
                    }
                }),

                SubState(WaitingIgnition, Ready, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, WaitingIgnition);
                        return new DomainEvent[]
                        {
                            domainEventFactory.PropertiesChanged(boardSlave.DeviceNode.Urn.Plus(boardSlave.Name), gasBurner.status, GasBurnerStatus.Ready)
                        };
                    }
                }),
                SubState(Igniting, Ready, new Func<DomainEvent, DomainEvent[]>[]
                {
                    _ =>
                    {
                        Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Igniting);
                        return fsmActions.StartIgnition();
                    }
                }),
                SubState(Ignited, Ready, new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Ignited);
                            return new DomainEvent[]
                            {
                                domainEventFactory.PropertiesChanged(gasBurner.status, GasBurnerStatus.Ignited)
                            };
                        }
                    },
                    onExit: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ => fsmActions.StopIgnition()
                    }
                )
            };

            var transitions = new[]
            {
                Transition<State, DomainEvent>(EnabledAvailable, DisabledAvailable,
                    @event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Disabled)),
                Transition<State, DomainEvent>(DisabledAvailable, EnabledAvailable,
                    @event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Enabled)),
                Transition<State, DomainEvent>(EnabledAvailable, EnabledNotAvailable,
                    @event => @event.IsRegulationExited()),
                Transition<State, DomainEvent>(DisabledAvailable, DisabledNotAvailable,
                    @event => @event.IsRegulationExited()),
                Transition<State, DomainEvent>(DisabledNotAvailable, EnabledNotAvailable,
                    @event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Enabled)),
                Transition<State, DomainEvent>(EnabledNotAvailable, DisabledNotAvailable,
                    @event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Disabled)),
                Transition<State, DomainEvent>(EnabledNotAvailable, EnabledAvailable,
                    @event => @event.IsRegulationEntered()),
                Transition<State, DomainEvent>(DisabledNotAvailable, DisabledAvailable,
                    @event => @event.IsRegulationEntered()),

                Transition<State, DomainEvent>(StandBy, Running, @event => @event.IsSupply(gasBurner, PowerSupply.On)),
                Transition<State, DomainEvent>(Running, StandBy, @event => @event.IsSupply(gasBurner, PowerSupply.Off)),
                Transition<State, DomainEvent>(Supplying, Supplied,
                    @event => @event.IsTimeout(gasBurner.ignition_settings.ignition_supplying_delay)),
                Transition<State, DomainEvent>(Resetting, ResetPerformed,
                    @event => @event.IsTimeout(gasBurner.ignition_settings.ignition_reset_delay)),
                Transition<State, DomainEvent>(ResetPerformed, Ready,
                    @event => @event.IsTimeout(gasBurner.ignition_settings.ignition_reset_delay)),
                Transition<State, DomainEvent>(Faulted, Resetting, @event => @event.IsManualResetting(gasBurner)),
                Transition<State, DomainEvent>(Ready, Faulted, @event => @event.IsFaultedDetected(gasBurner)),
                Transition<State, DomainEvent>(CheckReadiness, WaitingIgnition,
                    @event => @event.IsNotFaultedDetected(gasBurner)),
                Transition<State, DomainEvent>(WaitingIgnition, Igniting, @event => @event.IsStartIgnition(gasBurner)),
                Transition<State, DomainEvent>(Igniting, Ignited,
                    @event => @event.IsTimeout(gasBurner.ignition_settings.ignition_period)),
                Transition<State, DomainEvent>(Ignited, WaitingIgnition, @event => @event.IsStopIgnition(gasBurner)),
            };

            return new FSM<State, DomainEvent, DomainEvent>(EnabledAvailable, stateDefinitions, transitions);
        }
    }
}