using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.TestsCommon;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using BHBoardState = ImpliciX.RTUModbus.Controllers.BHBoard.State;
using OtherBoardState = ImpliciX.RTUModbus.Controllers.VendorBoard.State;
using BrahmaBoardState = ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public class ControllerBuilder<T, S> where T: class, ISlaveController where S:struct
    {
        internal HardwareAndSoftwareDeviceNode DeviceNode;
        private readonly S? _currentState;
        private uint _readPeacePaceInSystemTicks;
        private List<ICommandExecutionDefinition> _commandExecutionDefinition;
        private ReadPropertiesDefinition<T, S> _readBootloaderDefinition;
        private ReadPropertiesDefinition<T,S> _readMainFirmwareDefinition;
        private ReadPropertiesDefinition<T,S> _readCommonDefinition;
        private Urn[] _settingsUrns = new Urn []{};
        private DriverStateKeeper _stateKeeper;
        private BurnerNode _genericBurner;

        private ControllerBuilder(S? currentState)
        {
            _currentState = currentState;
            _readPeacePaceInSystemTicks = 1;
            _commandExecutionDefinition = new List<ICommandExecutionDefinition>();
            _readBootloaderDefinition = ReadPropertiesDefinition<T, S>.Empty(this);
            _readMainFirmwareDefinition = ReadPropertiesDefinition<T, S>.Empty(this);
            _readCommonDefinition = ReadPropertiesDefinition<T, S>.Empty(this);
        }

        public ControllerBuilder<T,S> WithReadPeaceSettings(uint readPeacePaceInSystemTicks)
        {
            _readPeacePaceInSystemTicks = readPeacePaceInSystemTicks;
            return this;
        }

        public ReadPropertiesDefinition<T, S> ReadMainFirmwareSimulation()
        {
            _readMainFirmwareDefinition = new ReadPropertiesDefinition<T, S>(this);
            return _readMainFirmwareDefinition;
        }
        public ReadPropertiesDefinition<T,S> ReadBootloaderSimulation()
        {
            _readBootloaderDefinition = new ReadPropertiesDefinition<T,S>(this);
            return _readBootloaderDefinition;
        }
        
        public ReadPropertiesDefinition<T,S> ReadCommonSimulation()
        {
            _readCommonDefinition = new ReadPropertiesDefinition<T,S>(this);
            return _readCommonDefinition;
        }


        public CommandExecutionDefinition<T,S, Arg> ExecuteCommandSimulation<Arg>(CommandNode<Arg> urn)
        {
            var def = new CommandExecutionDefinition<T,S, Arg>(this, urn);
            _commandExecutionDefinition.Add(def);
            return def;
        }



        public static ControllerBuilder<T, S> DefineController()
        {
            return new ControllerBuilder<T, S>(null);
        }

        public static ControllerBuilder<T, S> DefineControllerInState(S currentState)
        {
            return new ControllerBuilder<T, S>(currentState);
        }

        
        public ControllerBuilder<T,S> ForSimulatedSlave(HardwareAndSoftwareDeviceNode deviceNode, BurnerNode genericBurner=null)
        {
            DeviceNode = deviceNode;
            _genericBurner = genericBurner;
            return this;   
        }


        public T BuildSlaveController()
        {
            if (typeof(T) == typeof(Controllers.BHBoard.Controller))
            {
                var (controller, _) = CreateBHBoardController();
                return controller as T;
            }

            if (typeof(T) == typeof(Controllers.BrahmaBoard.Controller))
            {
                var (controller, _) = CreateBrahmaBoardController();
                return controller as T;
            }
            
            if (typeof(T) == typeof(Controllers.VendorBoard.Controller))
            {
                var (controller, _) = CreateOtherBoardController();
                return controller as T;
            }

            throw new NotSupportedException();
        }

        public T BuildSlaveController(out FirmwareUpdateContext ctx)
        {
            if (typeof(T) == typeof(Controllers.BHBoard.Controller))
            {
                var (controller, context) = CreateBHBoardController();
                ctx = context;
                return controller as T;
            }
            if (typeof(T) == typeof(Controllers.BrahmaBoard.Controller))
            {
                var (controller, context) = CreateBrahmaBoardController();
                ctx = context;
                return controller as T;
            }

            if (typeof(T) == typeof(Controllers.VendorBoard.Controller))
            {
                var (controller, context) = CreateOtherBoardController();
                ctx = context;
                return controller as T;
            }

            throw new NotSupportedException();
        }

        private (Controllers.BHBoard.Controller sut, FirmwareUpdateContext controllerContext) CreateBHBoardController()
        {
            
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            EventsHelper.ModelFactory = modelFactory;
            var domainEventFactory = EventFactory.Create(modelFactory, () => TimeSpan.Zero);
            var driverStateKeeper = _stateKeeper ?? new DriverStateKeeper();
            var slave = new FakeBoard(DeviceNode)
            {
                ReadAndDecodeMainFirmwareSimulation = new ReadAndDecodeSimulatedResults(_readMainFirmwareDefinition.SimulatedResults),
                ReadPaceInSystemTicks = _readPeacePaceInSystemTicks,
                CommandExecutionSimulations = _commandExecutionDefinition.Select(def=>def.Simulation).ToList(),
                ReadAndDecodeBootloaderSimulation = new ReadAndDecodeSimulatedResults(_readBootloaderDefinition.SimulatedResults),
                ReadAndDecodeCommonSimulation = new ReadAndDecodeSimulatedResults(_readCommonDefinition.SimulatedResults),
                SettingsUrns = _settingsUrns,
            };
            
            var controllerContext = new FirmwareUpdateContext(slave.DeviceNode);
            var slaveModel = new ModbusSlaveModel
                {Commit = test_model._commit_update, Rollback = test_model._rollback_update};

            var sut = new Controllers.BHBoard.Controller(
                slave,
                slaveModel,
                controllerContext,
                domainEventFactory,
                driverStateKeeper,
                _currentState as BHBoardState?
            );
            return (sut, controllerContext);
        }
        
        private (Controllers.BrahmaBoard.Controller sut, FirmwareUpdateContext controllerContext) CreateBrahmaBoardController()
        {
            var modelFactory = new ModelFactory(new []{this.GetType().Assembly});
            EventsHelper.ModelFactory = modelFactory;
            var domainEventFactory = EventFactory.Create(modelFactory, () => TimeSpan.Zero);
            var driverStateKeeper = _stateKeeper ?? new DriverStateKeeper();
            var slave = new FakeBoard(DeviceNode, _genericBurner)
            {
                ReadAndDecodeMainFirmwareSimulation = new ReadAndDecodeSimulatedResults(_readMainFirmwareDefinition.SimulatedResults),
                ReadPaceInSystemTicks = _readPeacePaceInSystemTicks,
                CommandExecutionSimulations = _commandExecutionDefinition.Select(def=>def.Simulation).ToList(),
                ReadAndDecodeBootloaderSimulation = new ReadAndDecodeSimulatedResults(_readBootloaderDefinition.SimulatedResults),
                ReadAndDecodeCommonSimulation = new ReadAndDecodeSimulatedResults(_readCommonDefinition.SimulatedResults),
                SettingsUrns = _settingsUrns,
            };
            
            var controllerContext = new FirmwareUpdateContext(slave.DeviceNode);

            var sut =  new Controllers.BrahmaBoard.Controller(slave, domainEventFactory, driverStateKeeper, _currentState as BrahmaBoardState?); 
            return (sut, controllerContext);
        }

        private (Controllers.VendorBoard.Controller sut, FirmwareUpdateContext controllerContext) CreateOtherBoardController()
        {
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            EventsHelper.ModelFactory = modelFactory;
            var domainEventFactory = EventFactory.Create(modelFactory, () => TimeSpan.Zero);
            var driverStateKeeper = _stateKeeper ?? new DriverStateKeeper();
            var slave = new FakeBoard(DeviceNode)
            {
                ReadAndDecodeMainFirmwareSimulation = new ReadAndDecodeSimulatedResults(_readMainFirmwareDefinition.SimulatedResults),
                ReadPaceInSystemTicks = _readPeacePaceInSystemTicks,
                CommandExecutionSimulations = _commandExecutionDefinition.Select(def=>def.Simulation).ToList(),
                SettingsUrns = _settingsUrns
            };

            var controllerContext = new FirmwareUpdateContext(DeviceNode);
            var sut = new Controllers.VendorBoard.Controller(
                slave,
                domainEventFactory,
                driverStateKeeper,
                _currentState as OtherBoardState?
            );
            return (sut, controllerContext);            
        }

        public ControllerBuilder<T, S> WithSettingsUrns(Urn[] settingsUrns)
        {
            _settingsUrns = settingsUrns; 
            return this;
        }

        public ControllerBuilder<T, S> WithStateKeeper(DriverStateKeeper stateKeeper)
        {
            _stateKeeper = stateKeeper;
            return this;
        }


    }

    public interface ICommandExecutionDefinition
    {
        IExecuteCommandSimulation Simulation { get; }
    }
    public class CommandExecutionDefinition<T,S, Arg> : ICommandExecutionDefinition where T : class, ISlaveController where S : struct
    {
        public CommandNode<Arg> CommandNode { get; }

        public Result2<IDataModelValue[],CommunicationDetails> SimulationResult { get; private set; }
        public IExecuteCommandSimulation Simulation => new ExecuteCommandSimulation<Arg>(CommandNode) {SimulationResult = SimulationResult};

        private readonly ControllerBuilder<T, S> _controllerBuilder;

        public CommandExecutionDefinition(ControllerBuilder<T, S> controllerBuilder, CommandNode<Arg> urn)
        {
            CommandNode = urn;
            _controllerBuilder = controllerBuilder;
        }

        public ControllerBuilder<T, S> ReturningSuccessResult()
        {
            SimulationResult = default;
            return _controllerBuilder;
        }
        
        
        public ControllerBuilder<T, S> ReturningResult(params IDataModelValue[] executionResult)
        {
            SimulationResult = (executionResult,new CommunicationDetails(1,0));
            return _controllerBuilder;
        }

        public ControllerBuilder<T, S> WithExecutionError(CommunicationDetails communicationDetails=null)
        {
            SimulationResult = (CommandExecutionError.Create(_controllerBuilder.DeviceNode, new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(CommandNode.status,MeasureStatus.Failure, TimeSpan.Zero)
            }),communicationDetails??new CommunicationDetails(0,1));
            return _controllerBuilder;
        }
    }

    public class ReadPropertiesDefinition<T, S> where T : class, ISlaveController where S : struct
    {
        
        private readonly ControllerBuilder<T, S> _controllerBuilder;
        public List<Result2<IDataModelValue[], CommunicationDetails>> SimulatedResults { get; }
        public static ReadPropertiesDefinition<T, S> Empty(ControllerBuilder<T, S> controllerBuilder) =>
            new ReadPropertiesDefinition<T, S>(controllerBuilder)
                .Returning(new IDataModelValue[0]);
        public ReadPropertiesDefinition(ControllerBuilder<T, S> controllerBuilder)
        {
            _controllerBuilder = controllerBuilder;
            SimulatedResults = new List<Result2<IDataModelValue[],CommunicationDetails>>();
        }

        public ReadPropertiesDefinition<T, S> Returning(Result<IDataModelValue[]> result, int times=1)
        {
            for (int i = 0; i < times; i++)
            {
                if (result.IsSuccess)
                {
                    SimulatedResults.Add((result.Value,Healthy_CommunicationDetails));
                }
                else
                {
                    SimulatedResults.Add((result.Error,Error_CommunicationDetails));
                }
            }
            return this;
        }
        
        public ReadPropertiesDefinition<T, S> ThenReturning(Result<IDataModelValue[]> values)
        {
            return Returning(values);
        }


        public ControllerBuilder<T, S> EndSimulation()
        {
            return _controllerBuilder;
        }

        public ReadPropertiesDefinition<T, S> WithReadProtocolError(CommunicationDetails communicationDetails=null)
        {
            SimulatedResults.Add((ReadProtocolError.Create(_controllerBuilder.DeviceNode),communicationDetails??Error_CommunicationDetails));
            return this;
        }

        public ReadPropertiesDefinition<T, S> WithSlaveCommunicationError(CommunicationDetails communicationDetails=null)
        {
            SimulatedResults.Add((SlaveCommunicationError.Create(_controllerBuilder.DeviceNode),communicationDetails??Error_CommunicationDetails));
            return this;
        }

        
    }
}