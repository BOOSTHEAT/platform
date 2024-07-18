using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.Language.Core;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Doubles;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Language.Model.MeasureStatus;
using static ImpliciX.RTUModbus.Controllers.BHBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<ImpliciX.RTUModbus.Controllers.BHBoard.Controller, ImpliciX.RTUModbus.Controllers.BHBoard.State>;
using static ImpliciX.TestsCommon.EventsHelper;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;


namespace ImpliciX.RTUModbus.Controllers.Tests.BHBoard
{
    [TestFixture]
    public class FirmwareUpdateTests
    {
        private static HardwareAndSoftwareDeviceNode _deviceNode = test_model.software.fake_daughter_board;
        private static microcontroller _microcontroller_node = test_model.software.fake_daughter_board._private<microcontroller>();
        private static firmware_update _firmware_update_node = test_model.software.fake_daughter_board._private<firmware_update>();

        [Test]
        public void should_reboot_the_daughter_board_and_store_package_content_when_receives_update_command()
        {
            var slaveController = 
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ExecuteCommandSimulation(test_model.software.fake_daughter_board._update)
                            .ReturningSuccessResult()
                        .ExecuteCommandSimulation(_microcontroller_node._reset)
                            .ReturningSuccessResult()
                .BuildSlaveController(out var controllerContext);

            var resultedEvents = slaveController.HandleDomainEvent(GeneralUpdateCommand);
            var expected = new DomainEvent[]
            {
                RegulationExited.Create(test_model.software.fake_daughter_board),
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (_microcontroller_node._reset.measure,ResetArg.Reset),
                    (_microcontroller_node._reset.status,Success))  
            };
            Check.That(slaveController.CurrentState).IsEqualTo(UpdateInitializing);
            Check.That(resultedEvents).ContainsExactly(expected);
            Check.That(controllerContext.Contains<PackageContent>()).IsTrue();
        }
        
        [Test]
        public void should_send_update_progress_at_100_percent_if_slave_is_disabled()
        {
            var slaveController = 
                DefineControllerInState(Disabled)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ExecuteCommandSimulation(test_model.software.fake_daughter_board._update)
                    .ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);

            var resultedEvents = slaveController.HandleDomainEvent(GeneralUpdateCommand);
            var expected = new DomainEvent[]
            {
                EventPropertyChanged(TimeSpan.Zero, test_model.software.fake_daughter_board.Urn,
                    (test_model.software.fake_daughter_board.update_progress, Percentage.FromFloat(1f).Value))  
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Disabled);
            Check.That(resultedEvents).ContainsExactly(expected);
        }

        [Test]
        public void should_send_fatal_communication_when_error_occurs_when_sending_reset_command()
        {
            var slaveController =
                DefineControllerInState(Regulation)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                        .ExecuteCommandSimulation(_microcontroller_node._reset).WithExecutionError()
                    .BuildSlaveController();
           
            var resultedEvents = slaveController.HandleDomainEvent(GeneralUpdateCommand);
            var expectedEvents = new DomainEvent[]
            {
                RegulationExited.Create(test_model.software.fake_daughter_board),
                SlaveCommunicationOccured.CreateFatal(test_model.software.fake_daughter_board,TimeSpan.Zero, Error_CommunicationDetails), 
                EventPropertyChanged(_microcontroller_node._reset.status, Failure, TimeSpan.Zero),
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void should_read_bootloader_properties_and_detect_the_active_partition()
        {
            var bootLoaderProperties1 = new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(_microcontroller_node.active_partition.status, Failure, TimeSpan.Zero),
            };
   
            var bootLoaderProperties2 = new IDataModelValue[]
            {
                Property<Partition>.Create(_microcontroller_node.active_partition.measure, Partition.One, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node.active_partition.status, Success, TimeSpan.Zero),
            };
            
            var slaveController =
                DefineControllerInState(UpdateInitializing)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ReadBootloaderSimulation()
                        .Returning(bootLoaderProperties1)
                        .ThenReturning(bootLoaderProperties2)
                    .EndSimulation()
                .BuildSlaveController(out var controllerContext);
            
            
            var resultedEvents = slaveController.ReadMany(2);
            var expectedEvents = new DomainEvent[][]
            {
                new DomainEvent[]
                {
                    SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails),
                    EventPropertyChanged(slaveController.Group, bootLoaderProperties1,TimeSpan.Zero),
                },
                new DomainEvent[]
                {
                    SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                    ActivePartitionDetected.Create(_deviceNode,Partition.One),
                   EventPropertyChanged(slaveController.Group, bootLoaderProperties2,TimeSpan.Zero)
                }, 
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void should_send_communication_error_when_error_occurs_when_detecting_the_active_partition()
        {
            var slaveController =
                DefineControllerInState(UpdateInitializing)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .WithReadPeaceSettings(1)
                        .ReadBootloaderSimulation().WithSlaveCommunicationError().EndSimulation()
               .BuildSlaveController();


            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateError(test_model.software.fake_daughter_board,TimeSpan.Zero, Error_CommunicationDetails), 
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_send_first_frame_when_the_active_partition_is_detected()
        {
            var slaveController = 
                DefineControllerInState(UpdateInitializing)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ExecuteCommandSimulation(_firmware_update_node._send_first_frame)
                        .ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);
            controllerContext.Set(PackageContentForTest(test_model.software.fake_daughter_board, "2.3.3.101"));
                
            var trigger = ActivePartitionDetected.Create(_deviceNode,Partition.One);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expected = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                FirstFrameSuccessfullySent.Create(_deviceNode), 
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group, 
                    (_firmware_update_node._send_first_frame.measure,FirstFrame.Create(Partition.One,500,"76053427","2.3.3.101")),
                    (_firmware_update_node._send_first_frame.status,Success)),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(UpdateStarting);
            Check.That(resultedEvents).ContainsExactly(expected);
            Check.That(controllerContext.Contains<Partition>()).IsTrue();
            Check.That(controllerContext.TryGet<Partition>()).IsEqualTo(Result<Partition>.Create(Partition.One));
        }

        [Test]
        public void should_send_fatal_communication_when_error_occurs_when_sending_first_frame()
        {
            var slaveController =
                DefineControllerInState(UpdateInitializing)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                        .ExecuteCommandSimulation(_firmware_update_node._send_first_frame).WithExecutionError()
               .BuildSlaveController(out var controllerContext);
            
            controllerContext.Set(PackageContentForTest(test_model.software.fake_daughter_board, "2.3.3.101"));
                
            var trigger = ActivePartitionDetected.Create(_deviceNode,Partition.One);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(test_model.software.fake_daughter_board,TimeSpan.Zero, Error_CommunicationDetails), 
                EventPropertyChanged(_firmware_update_node._send_first_frame.status, Failure, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(UpdateStarting);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void should_transition_to_waiting_upload_ready_when_send_first_frame_succeeded()
        {
            var slaveController =
                DefineControllerInState(UpdateStarting)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .BuildSlaveController();
            var trigger = FirstFrameSuccessfullySent.Create(_deviceNode);
            slaveController.HandleDomainEvent(trigger);
            Check.That(slaveController.CurrentState).IsEqualTo(WaitingUploadReady);
        }

        [Test]
        public void should_send_BoardUpdateRunningDetected_when_board_enters_in_the_state_UpdateRunning()
        {
            var boardStateProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller_node.board_state.measure, BoardState.UpdateRunning, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node.board_state.status, Success, TimeSpan.Zero),
            };
            
            var slaveController =
                DefineControllerInState(WaitingUploadReady)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ReadCommonSimulation()
                        .Returning(boardStateProperties)
                    .EndSimulation()
                .BuildSlaveController();

            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, TimeSpan.Zero, Healthy_CommunicationDetails),
                BoardUpdateRunningDetected.Create(_deviceNode),
                EventPropertyChanged(slaveController.Group, boardStateProperties,TimeSpan.Zero)
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [TestCase(BoardState.RegulationStarted)]
        [TestCase(BoardState.WaitingForStart)]
        public void should_not_send_BoardUpdateRunningDetected_when_board_do_not_enters_in_the_state_UpdateRunning(BoardState currentBoardState)
        {
            var boardStateProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller_node.board_state.measure,currentBoardState, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node.board_state.status, Success, TimeSpan.Zero),
            };
            
            var slaveController =
                DefineControllerInState(WaitingUploadReady)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ReadCommonSimulation()
                    .Returning(boardStateProperties)
                    .EndSimulation()
                    .BuildSlaveController();

            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, TimeSpan.Zero, Healthy_CommunicationDetails),
                EventPropertyChanged(slaveController.Group, boardStateProperties,TimeSpan.Zero)
            };
            Check.That(resultedEvents).Not.Contains(BoardUpdateRunningDetected.Create(_deviceNode));
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void should_send_communication_error_when_error_occurs_when_detecting_the_board_state()
        {
            var slaveController =
                DefineControllerInState(WaitingUploadReady)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ReadCommonSimulation().WithSlaveCommunicationError().EndSimulation()
                    .BuildSlaveController();


            var trigger = SystemTicked.Create(1000, 1);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateError(test_model.software.fake_daughter_board,TimeSpan.Zero, Error_CommunicationDetails), 
            };
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_send_first_chunk_when_the_board_entered_in_the_state_UpdateRunning()
        {
            var slaveController = 
                DefineControllerInState(WaitingUploadReady)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ExecuteCommandSimulation(_firmware_update_node._send_chunk)
                    .ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);
            controllerContext.Set(PackageContentForTest(test_model.software.fake_daughter_board, "2.3.3.101"));
                
            var trigger = BoardUpdateRunningDetected.Create(_deviceNode);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                SendPreviousChunkSucceeded.Create(_deviceNode), 
                EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                    (_firmware_update_node._send_chunk.measure, ExpectedAllChunks[0]),
                    (_firmware_update_node._send_chunk.status, MeasureStatus.Success),
                    (test_model.software.fake_daughter_board.update_progress, Percentage.FromFloat(0.0625f).Value))
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Uploading);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_send_next_chunks_when_the_previous_chunk_is_successful()
        {
            var slaveController =
                DefineControllerInState(Uploading)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ExecuteCommandSimulation(_firmware_update_node._send_chunk)
                    .ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);
            controllerContext.Set(TestChunksExceptFirst);


            var trigger = SendPreviousChunkSucceeded.Create(_deviceNode);
            var resultedEvents = slaveController.HandleMany(trigger, 3);
            var expectedEvents =
                new DomainEvent[][]
                {
                    new DomainEvent[]
                    {
                        SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                        SendPreviousChunkSucceeded.Create(_deviceNode), 
                        EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                            (_firmware_update_node._send_chunk.measure, ExpectedAllChunks[1]),
                            (_firmware_update_node._send_chunk.status, MeasureStatus.Success),
                            (test_model.software.fake_daughter_board.update_progress, Percentage.FromFloat(0.125f).Value))
                    },
                    new DomainEvent[]
                    {
                        SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                        SendPreviousChunkSucceeded.Create(_deviceNode), 
                        EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                            (_firmware_update_node._send_chunk.measure, ExpectedAllChunks[2]),
                            (test_model.software.fake_daughter_board.update_progress, Percentage.FromFloat(0.1875f).Value))
                    },
                    new DomainEvent[]
                    {
                        SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                        SendPreviousChunkSucceeded.Create(_deviceNode), 
                        EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                            (_firmware_update_node._send_chunk.measure, ExpectedAllChunks[3]),
                            (test_model.software.fake_daughter_board.update_progress, Percentage.FromFloat(0.25f).Value))
                    },
                    new DomainEvent[]
                    {
                        SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board,TimeSpan.Zero, Healthy_CommunicationDetails), 
                        SendPreviousChunkSucceeded.Create(_deviceNode),
                        SendChunksFinished.Create(_deviceNode),
                        EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                            (_firmware_update_node._send_chunk.measure, ExpectedAllChunks[15]),
                            (test_model.software.fake_daughter_board.update_progress, Percentage.FromFloat(1f).Value))
                    }
                    
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Uploading);
            Check.That(resultedEvents[0]).Contains(expectedEvents[0]);
            Check.That(resultedEvents[1]).Contains(expectedEvents[1]);
            Check.That(resultedEvents[2]).Contains(expectedEvents[2]);
        }

        [Test]
        public void should_send_fatal_communication_when_error_occurs_when_sending_chunks()
        {
            var slaveController =
                DefineControllerInState(Uploading)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ExecuteCommandSimulation(_firmware_update_node._send_chunk)
                    .WithExecutionError()
                    .BuildSlaveController(out var controllerContext);
            
            controllerContext.Set(TestChunksExceptFirst);
                
            var trigger = SendPreviousChunkSucceeded.Create(_deviceNode);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(test_model.software.fake_daughter_board,TimeSpan.Zero, Error_CommunicationDetails), 
                EventPropertyChanged(_firmware_update_node._send_chunk.status, Failure, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(Uploading);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [Test]
        public void should_transition_to_upload_completed()
        {
            var slaveController =
                DefineControllerInState(Uploading)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ExecuteCommandSimulation(_firmware_update_node._send_chunk).ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);
            controllerContext.Set(TestChunksExceptFirst);


            var trigger = SendChunksFinished.Create(_deviceNode);
            slaveController.HandleDomainEvent(trigger);
            Check.That(slaveController.CurrentState).IsEqualTo(UploadCompleted);
        }

        [Test]
        public void should_set_active_partition_when_receives_commit_command()
        {
            var slaveController =
                DefineControllerInState(UploadCompleted)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ExecuteCommandSimulation(_microcontroller_node._set_active_partition).ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);
                controllerContext.Set(Partition.Two);    
            
                var trigger = EventCommandRequested(test_model._commit_update, default(NoArg), TimeSpan.Zero);
                var resultedEvents = slaveController.HandleDomainEvent(trigger);
                var expectedEvents = new DomainEvent[]
                {
                    SlaveCommunicationOccured.CreateHealthy(test_model.software.fake_daughter_board, TimeSpan.Zero, Healthy_CommunicationDetails),
                    SetActivePartitionSucceeded.Create(_deviceNode),
                    EventPropertyChanged(TimeSpan.Zero, slaveController.Group,
                        (_microcontroller_node._set_active_partition.measure, Partition.Two),
                        (_microcontroller_node._set_active_partition.status, Success))
                };
                Check.That(slaveController.CurrentState).IsEqualTo(UploadCompleted);
                Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }
        
        [Test]
        public void should_force_reboot_the_mcu_then_transition_to_initializing_when_active_partition_was_set_successfully()
        {
            var bootLoaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller_node.board_state.measure, BoardState.WaitingForStart, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node.board_state.status, Success, TimeSpan.Zero),
            };

            var slaveController =
                DefineControllerInState(UploadCompleted)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                        .ReadBootloaderSimulation().Returning(bootLoaderProperties).EndSimulation()
                        .ExecuteCommandSimulation(_microcontroller_node.bootloader._switch).ReturningSuccessResult()
                        .ExecuteCommandSimulation(_microcontroller_node._reset).ReturningSuccessResult()
                    .BuildSlaveController(out var controllerContext);

            var trigger = SetActivePartitionSucceeded.Create(_deviceNode);
            
            var resultedEvents = slaveController.HandleDomainEvent(trigger).FilterEvents<PropertiesChanged>().CollectProperties();
            var expected = new IDataModelValue[]
            {
                Property<ResetArg>.Create(_microcontroller_node._reset.measure,ResetArg.Reset, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node._reset.status,Success, TimeSpan.Zero),
            };
            Check.That(resultedEvents).Contains(expected);
            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
        }
        
        
        [Test]
        public void should_send_fatal_communication_when_error_occurs_when_setting_active_partition()
        {
            var slaveController =
                DefineControllerInState(UploadCompleted)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .WithReadPeaceSettings(1)
                    .ExecuteCommandSimulation(_microcontroller_node._set_active_partition).WithExecutionError()
                    .BuildSlaveController(out var controllerContext);
            controllerContext.Set(Partition.Two);    
            controllerContext.Set(TestChunksExceptFirst);
                
            var trigger = EventCommandRequested(test_model._commit_update, default(NoArg), TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger);
            var expectedEvents = new DomainEvent[]
            {
                SlaveCommunicationOccured.CreateFatal(test_model.software.fake_daughter_board,TimeSpan.Zero, Error_CommunicationDetails), 
                EventPropertyChanged(_microcontroller_node._set_active_partition.status, Failure, TimeSpan.Zero),
            };
            Check.That(slaveController.CurrentState).IsEqualTo(UploadCompleted);
            Check.That(resultedEvents).ContainsExactly(expectedEvents);
        }

        [TestCase(UpdateInitializing)]
        [TestCase(UpdateStarting)]
        [TestCase(WaitingUploadReady)]
        [TestCase(Uploading)]
        [TestCase(UploadCompleted)]
        public void should_reboot_the_mcu_then_transition_to_initializing_when_rollback_command_is_received(State currentState)
        {
            var bootLoaderProperties = new IDataModelValue[]
            {
                Property<BoardState>.Create(_microcontroller_node.board_state.measure, BoardState.WaitingForStart, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node.board_state.status, Success, TimeSpan.Zero),
            };

            var slaveController =
                DefineControllerInState(UploadCompleted)
                    .ForSimulatedSlave(test_model.software.fake_daughter_board)
                    .ReadBootloaderSimulation().Returning(bootLoaderProperties).EndSimulation()
                    .ExecuteCommandSimulation(_microcontroller_node.bootloader._switch)
                    .ReturningSuccessResult()
                    .ExecuteCommandSimulation(_microcontroller_node._reset).ReturningSuccessResult()
                .BuildSlaveController(out var controllerContext);

            var trigger = CommandRequested.Create(test_model._rollback_update, default(NoArg), TimeSpan.Zero);
            var resultedEvents = slaveController.HandleDomainEvent(trigger).FilterEvents<PropertiesChanged>().CollectProperties();
            var expected = new IDataModelValue[]
            {
                Property<ResetArg>.Create(_microcontroller_node._reset.measure,ResetArg.Reset, TimeSpan.Zero),
                Property<MeasureStatus>.Create(_microcontroller_node._reset.status,Success, TimeSpan.Zero),
            };
            Check.That(resultedEvents).Contains(expected);
            Check.That(slaveController.CurrentState).IsEqualTo(Initializing);
        }
        
        private static Queue<Chunk> TestChunksExceptFirst =>
            new Queue<Chunk>(ExpectedAllChunks[1..]);
        private static Chunk[] ExpectedAllChunks =>
            new[]
            {
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 0, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 1, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 2, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 3, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 4, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 5, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 6, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 7, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 8, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 9, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 10, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 11, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 12, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 13, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 16).ToArray(), 14, 16),
                Chunk.Create(Enumerable.Repeat<ushort>(0xcaca, 10).ToArray(), 15, 16),
            };
        private static PackageContent PackageContentForTest(HardwareAndSoftwareDeviceNode deviceNode, string revision)
            => new PackageContent(deviceNode, revision, Enumerable.Repeat<byte>(0xca, 500).ToArray());

        private CommandRequested GeneralUpdateCommand => CreateBasicSoftwareUpdateCommand(test_model.software.fake_daughter_board);

        private CommandRequested CreateBasicSoftwareUpdateCommand(HardwareAndSoftwareDeviceNode deviceNode)
        {
            return SfwUpdReq(deviceNode, "revision", Enumerable.Repeat<byte>(0xca, 500).ToArray());
        }

        private CommandRequested SfwUpdReq(HardwareAndSoftwareDeviceNode deviceNode, string revision, byte[] bytes)
        {
            var content = new PackageContent(deviceNode, revision, bytes);
            var cmd = CommandRequested.Create(deviceNode._update, content, TimeSpan.Zero);
            return cmd;
        }
    }
}