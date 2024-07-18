using System;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Helpers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;
using static ImpliciX.SharedKernel.FiniteStateMachine.FSMHelper;

namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    public static class Fsm
    {
        public static FSM<State, DomainEvent, DomainEvent> Create(IBoardSlave boardSlave,
            ModbusSlaveModel slaveModel, DomainEventFactory domainEventFactory, FirmwareUpdateContext context)
        {
            var fsmActions = new FsmActions(boardSlave, domainEventFactory, context);
            var stateDefinitions = new[]
            {
                State(Disabled,
                    new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Disabled);
                            return new DomainEvent[]
                            {
                                domainEventFactory.HealthyCommunicationOccured(boardSlave.DeviceNode,
                                    new CommunicationDetails(0, 0)),
                            };
                        }
                    }, new Func<DomainEvent, DomainEvent[]>[]
                    {
                        @event => @event.IsFirmwareUpdateCommand()
                            ? UpdateProgressCompleteEvent(domainEventFactory, boardSlave)
                            : Array.Empty<DomainEvent>()
                    }
                ),
                State(Working, Array.Empty<Func<DomainEvent, DomainEvent[]>>()),
                InitialSubState(Initializing, Working,
                    new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Initializing);
                            return fsmActions.Initialize();
                        }
                    },
                    new[]
                    {
                        When(@event => @event.Is<CommandRequested>()).Then(_ => fsmActions.RejectCommand()),
                        When(@event => @event.Is<SystemTicked>()).Then(_ => fsmActions.Initialize()),
                    }
                ),
                SubState(Regulation, Working,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Regulation);
                            return new DomainEvent[] { RegulationEntered.Create(boardSlave.DeviceNode) };
                        },
                    },
                    onState: new[]
                    {
                        When(@event => @event.Is<SystemTicked>())
                            .Then(@event => fsmActions.ReadMainFirmware((SystemTicked)@event)),
                        When(@event => @event.Is<CommandRequested>()).Then(@event =>
                            fsmActions.ExecuteSlaveCommand((CommandRequested)@event)),
                    },
                    onExit: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ => new DomainEvent[] { RegulationExited.Create(boardSlave.DeviceNode) }
                    }
                ),
                SubState(Updating, Working,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Updating);
                            return fsmActions.ForceReboot();
                        }
                    }),
                InitialSubState(UpdateInitializing, Updating,
                    new Func<DomainEvent, DomainEvent[]>[]
                    {
                        @event =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name,
                                UpdateInitializing);
                            return fsmActions.StorePackageContent(@event);
                        }
                    },
                    new[]
                    {
                        When(@event => @event.IsSystemTicked()).Then(_ => fsmActions.DetectActivePartition())
                    }),
                SubState(UpdateStarting, Updating,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        @event =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, UpdateStarting);
                            return fsmActions.SendFirstFrame(@event);
                        }
                    }),
                SubState(WaitingUploadReady, Updating,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name,
                                WaitingUploadReady);
                            return Array.Empty<DomainEvent>();
                        },
                    },
                    onState: new[]
                    {
                        When(@event => @event.IsSystemTicked()).Then(_ => fsmActions.DetectUpdateRunning())
                    }),
                SubState(Uploading, Updating,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        @event =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, Uploading);
                            return fsmActions.SendFirstChunk(@event);
                        }
                    },
                    new[]
                    {
                        When(@event => @event.Is<SendPreviousChunkSucceeded>())
                            .Then(@event => fsmActions.SendChunk(@event))
                    }
                ),
                SubState(UploadCompleted, Updating,
                    onEntry: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ =>
                        {
                            Log.Information("[{@slave}] enters in state: {@state}", boardSlave.Name, UploadCompleted);
                            return Array.Empty<DomainEvent>();
                        },
                    },
                    onState: new[]
                    {
                        When(@event => @event.IsCommand(slaveModel.Commit)).Then(@event => fsmActions.CommitUpdate())
                    },
                    onExit: new Func<DomainEvent, DomainEvent[]>[]
                    {
                        _ => fsmActions.ForceReboot()
                    }
                )
            };

            var transitions = new[]
            {
                Transition<State, DomainEvent>(Initializing, Regulation,
                    @event => @event.IsExitBootloaderCommandSucceeded()),
                Transition<State, DomainEvent>(Regulation, Initializing, @event => @event.IsProtocolErrorOccured()),
                Transition<State, DomainEvent>(Working, Disabled,
                    @event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Disabled)),
                Transition<State, DomainEvent>(Disabled, Working,
                    @event => @event.IsPresence(boardSlave.HardwareDevice, Presence.Enabled)),
                Transition<State, DomainEvent>(Working, Updating, @event => @event.IsFirmwareUpdateCommand()),
                Transition<State, DomainEvent>(UpdateInitializing, UpdateStarting,
                    @event => @event.Is<ActivePartitionDetected>()),
                Transition<State, DomainEvent>(UpdateStarting, WaitingUploadReady,
                    @event => @event.Is<FirstFrameSuccessfullySent>()),
                Transition<State, DomainEvent>(WaitingUploadReady, Uploading,
                    @event => @event.Is<BoardUpdateRunningDetected>()),
                Transition<State, DomainEvent>(Uploading, UploadCompleted, @event => @event.Is<SendChunksFinished>()),
                Transition<State, DomainEvent>(UploadCompleted, Initializing,
                    @event => @event.Is<SetActivePartitionSucceeded>()),
                Transition<State, DomainEvent>(Working, Initializing, @event => @event.IsCommand(slaveModel.Rollback))
            };

            return new FSM<State, DomainEvent, DomainEvent>(Working, stateDefinitions, transitions);
        }

        private static DomainEvent[] UpdateProgressCompleteEvent(DomainEventFactory domainEventFactory,
            IBoardSlave boardSlave)
        {
            return
            (
                from pc in domainEventFactory.NewEventResult(
                    boardSlave.DeviceNode.Urn,
                    boardSlave.HardwareAndSoftwareDeviceNode.update_progress,
                    Percentage.FromFloat(1f).Value)
                select new[] { pc }).GetValueOrDefault(Array.Empty<DomainEvent>()
            );
        }
    }
}