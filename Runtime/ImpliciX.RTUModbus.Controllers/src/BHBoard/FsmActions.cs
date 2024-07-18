using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Helpers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    internal class FsmActions : FsmActionsBase
    {

        public DomainEvent[] Initialize() =>
            (
                from read in _boardSlave.ReadProperties(MapKind.Bootloader)
                from exit in _boardSlave.ExecuteCommand(_boardSlave.DeviceNode._private<microcontroller>().bootloader._switch, MCUBootloader.Exit)
                select (read.Item1.Concat(exit.Item1).ToArray() , read.Item2+exit.Item2)
            )
            .Match(
                whenError: (_,details) => new DomainEvent[] {_domainEventFactory.FatalCommunicationOccured(_boardSlave.DeviceNode,details)},
                whenSuccess: (properties,details) => new DomainEvent[]
                {
                    _domainEventFactory.NewEvent(properties),
                    ExitBootloaderCommandSucceeded.Create(_boardSlave.DeviceNode),
                    _domainEventFactory.HealthyCommunicationOccured(_boardSlave.DeviceNode,details),
                    _domainEventFactory.SlaveRestarted(_boardSlave.DeviceNode)
                }
            );
        
        public DomainEvent[] ForceReboot() => 
            ExecuteSlaveCommand(_boardSlave.DeviceNode._private<microcontroller>()._reset, ResetArg.Reset);
    
        public DomainEvent[] StorePackageContent(DomainEvent @event) =>
            (
                from updateRequested in SafeCast<CommandRequested>(@event)
                from packageContent in SafeCast<PackageContent>(updateRequested.Arg)
                let _ = _context.Set(packageContent)
                select Array.Empty<DomainEvent>()
            ).GetValueOrDefault(Array.Empty<DomainEvent>());
         
        protected override DomainEvent[] InterpretReadSuccess(IDataModelValue[] values,CommunicationDetails communicationDetails)
        {
            if (IsUnexpectedBootDetected(values))
            {
                return new DomainEvent[]
                {
                    _domainEventFactory.FatalCommunicationOccured(_boardSlave.DeviceNode, communicationDetails),
                    ProtocolErrorOccured.Create(_boardSlave.DeviceNode)
                };
            }
            return SuccessOutput(values,communicationDetails);
        }

        protected override DomainEvent[] InterpretReadError(Error error, CommunicationDetails communicationDetails)
        {
            return error switch
            {
                ReadProtocolError _ => new DomainEvent[]
                {
                    _domainEventFactory.FatalCommunicationOccured(_boardSlave.DeviceNode,communicationDetails),
                    ProtocolErrorOccured.Create( _boardSlave.DeviceNode)
                },
                _ => new DomainEvent[] {_domainEventFactory.ErrorCommunicationOccured(_boardSlave.DeviceNode,communicationDetails)},
            };
        }
        private bool IsUnexpectedBootDetected(IDataModelValue[] values)
        {
            return values.Any(mv => mv.Urn.Equals(_boardSlave.DeviceNode._private<microcontroller>().board_state.measure) && !mv.ModelValue().Equals(BoardState.RegulationStarted));
        }

        public DomainEvent[] DetectActivePartition()
        {
           return  _boardSlave.ReadProperties(MapKind.Bootloader).Match(
                whenError: (err,details) => new DomainEvent[]{_domainEventFactory.ErrorCommunicationOccured(_boardSlave.DeviceNode, details)},
                whenSuccess: (values,details) =>
                {
                    var activePartition =
                        values.Where(it => it.Urn.Equals(_boardSlave.DeviceNode._private<microcontroller>().active_partition.measure))
                            .Select(it => (Partition?) it.ModelValue())
                            .FirstOrDefault();
                    if (activePartition.HasValue)
                    {
                        return SuccessOutput(values, details,ActivePartitionDetected.Create(_boardSlave.DeviceNode, activePartition.Value));
                    }

                    return SuccessOutput(values,details);
                }
            );
        }
        
        public DomainEvent[] DetectUpdateRunning()
        {
            return _boardSlave.ReadProperties(MapKind.Common).Match(
                whenError: (err,details) => new DomainEvent[] {_domainEventFactory.ErrorCommunicationOccured(_boardSlave.DeviceNode, details)},
                whenSuccess: (values,details) =>
                {
                    var boardState =
                        values.Where(it => it.Urn.Equals(_boardSlave.DeviceNode._private<microcontroller>().board_state.measure))
                            .Select(it => (BoardState?) it.ModelValue())
                            .FirstOrDefault();
                    if (boardState is BoardState.UpdateRunning)
                    {
                        return SuccessOutput(values, details,BoardUpdateRunningDetected.Create(_boardSlave.DeviceNode));
                    }

                    return SuccessOutput(values, details);
                });
        }

        public DomainEvent[] SendFirstFrame(DomainEvent @event)
        {
            var sendFirstFrame_urn = _boardSlave.DeviceNode._private<firmware_update>()._send_first_frame;
            var createFirsFrame =
                from partitionDetected in SafeCast<ActivePartitionDetected>(@event)
                let activePartition = partitionDetected.Partition
                from packageContent in _context.TryGet<PackageContent>()
                from firstFrame in CreateFirstFrame(packageContent)
                select firstFrame;

            return createFirsFrame.Match(
                err => InterpretCommandError(err as CommandExecutionError, new CommunicationDetails(0, 0)), 
                    firstFrame => SendFirstFrame(sendFirstFrame_urn, firstFrame));

        }

        private DomainEvent[] SendFirstFrame(CommandNode<FirstFrame> sendFirstFrame_urn, FirstFrame firstFrame)
        {
            return (from sendResult in _boardSlave.ExecuteCommand(sendFirstFrame_urn, firstFrame)
                    select sendResult)
                .Match((err, details) => InterpretCommandError(err as CommandExecutionError, details),
                (values, details) => SuccessOutput(values, details, FirstFrameSuccessfullySent.Create(_boardSlave.DeviceNode)));
        }

        private Result<FirstFrame> CreateFirstFrame(PackageContent packageContent) =>
            from contentInfo in UpdateTools.ComputeCrc(packageContent.Bytes)
            let partitionToUpdate = Partition.One
            let _ = _context.Set(partitionToUpdate)
            select FirstFrame.Create(partitionToUpdate, contentInfo.size, contentInfo.crc, packageContent.Revision);
        
        public DomainEvent[] SendFirstChunk(DomainEvent @event)
        {
            return (
                    from packageContent in _context.TryGet<PackageContent>()
                    let chunks = UpdateTools.ComputeChunks(packageContent.Bytes)
                    let _ = _context.Set(chunks)
                    select SendChunk(@event)
                ).GetValueOrDefault();
        }

        public DomainEvent[] SendChunk(DomainEvent @event)
        {
            if (!(@event.Is<BoardUpdateRunningDetected>() || @event.Is<SendPreviousChunkSucceeded>())) return Array.Empty<DomainEvent>();
            var chunks = _context.GetOrDefault(new Queue<Chunk>());
            var hasNextChunk = chunks.TryDequeue(out var chunk);
            if (!hasNextChunk)
            {
                return Array.Empty<DomainEvent>();
            }
            var send_chunk_command = _boardSlave.DeviceNode._private<firmware_update>()._send_chunk.command;
            return 
                _boardSlave.ExecuteCommand(send_chunk_command, chunk)
                .Match(
                        (error,details) =>
                        {
                            Log.Information("Sending chunk #{@chunkIndex} failed. The update process can't continue. Please try again.", chunk.Index);
                            return InterpretCommandError(error as CommandExecutionError,details);
                        },
                    (values,details) =>
                    {
                        WaitForMcuToWriteChunk();
                        var update_progress = Property<Percentage>.Create(_boardSlave.HardwareAndSoftwareDeviceNode.update_progress, Percentage.FromFloat(chunk.Progress).Value, TimeSpan.Zero);
                        var additionalEvents = new List<DomainEvent>() {SendPreviousChunkSucceeded.Create(_boardSlave.DeviceNode)};
                        if (chunk.IsLast())
                        {
                            additionalEvents.Add(SendChunksFinished.Create(_boardSlave.DeviceNode));
                        }
                        return SuccessOutput(values.Append(update_progress), details, additionalEvents.ToArray());
                    });
        }

        public DomainEvent[] CommitUpdate()
        { 
            var setActivePartitionCommand = _boardSlave.DeviceNode._private<microcontroller>()._set_active_partition.command;
           return (
                from updatedPartition in _context.TryGet<Partition>().ToResult2(new CommunicationDetails(0,0))
                from executionResult in _boardSlave.ExecuteCommand(setActivePartitionCommand, updatedPartition.Item1)
                select executionResult).Match(
                (error,details) => InterpretCommandError(error as CommandExecutionError,details),
                (values,details) => SuccessOutput(values, details,SetActivePartitionSucceeded.Create(_boardSlave.DeviceNode))
            );
        }

        private static void WaitForMcuToWriteChunk()
        {
            Thread.Sleep(100);
        }

        public FsmActions(IBoardSlave boardSlave, DomainEventFactory domainEventFactory, FirmwareUpdateContext context) : base(boardSlave, domainEventFactory)
        {
            _context = context;
        }
        
        private readonly FirmwareUpdateContext _context;

    }
}