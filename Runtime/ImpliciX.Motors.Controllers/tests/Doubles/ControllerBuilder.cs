using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.TestsCommon;

namespace ImpliciX.Motors.Controllers.Tests.Doubles
{
    public class ControllerBuilder<T, S> where T: class, ISlaveController where S:struct
    {
        internal HardwareAndSoftwareDeviceNode DeviceNode;
        private readonly S? _currentState;
        private uint _readPeacePaceInSystemTicks;
        private List<ICommandExecutionDefinition> _commandExecutionDefinition;
        private ReadPropertiesDefinition<T,S> _readRegulationDefinition;
        private Urn[] _settingsUrns;
        private DriverStateKeeper _stateKeeper;

        private ControllerBuilder(S? currentState)
        {
            _currentState = currentState;
            _readPeacePaceInSystemTicks = 1;
            _commandExecutionDefinition = new List<ICommandExecutionDefinition>();
            _readRegulationDefinition = ReadPropertiesDefinition<T, S>.Empty(this);
        }

        public ControllerBuilder<T,S> WithReadPeaceSettings(uint readPeacePaceInSystemTicks)
        {
            _readPeacePaceInSystemTicks = readPeacePaceInSystemTicks;
            return this;
        }

        public ReadPropertiesDefinition<T, S> ReadRegulationSimulation()
        {
            _readRegulationDefinition = new ReadPropertiesDefinition<T, S>(this);
            return _readRegulationDefinition;
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

        
        public ControllerBuilder<T,S> ForSimulatedSlave(HardwareAndSoftwareDeviceNode deviceNode)
        {
            this.DeviceNode = deviceNode;
            return this;   
        }


        public T BuildSlaveController()
        {
            return CreateMotorBoardController() as T;
        }

        private SlaveController CreateMotorBoardController()
        {

            var slave = new FakeMotorBoard(DeviceNode)
            {
                ReadAndDecodeRegulationSimulation = new ReadAndDecodeSimulatedResults(_readRegulationDefinition.SimulatedResults),
                ReadPaceInSystemTicks = _readPeacePaceInSystemTicks,
                CommandExecutionSimulations = _commandExecutionDefinition.Select(def=>def.Simulation).ToList(),
                SettingsUrns = _settingsUrns,
            };
            var motorsModel = new MotorsModuleDefinition();
            motorsModel.MotorsDeviceNode = test_model.software.fake_motor_board;
            motorsModel.HeatPumpDeviceNode = test_model.software.fake_heat_pump;
            motorsModel.MotorNodes = test_model.motors.Nodes;
            motorsModel.SettingsSupplyDelayTimerUrn = test_model.motors.supply_delay;
            motorsModel.MotorsStatusUrn = test_model.motors.status;
            motorsModel.SupplyCommand = test_model.motors._supply;
            motorsModel.PowerCommand = test_model.motors._power;
            motorsModel.SwitchCommandUrn = test_model.motors._switch;

            var clock = new StubClock();
            var modelFactory = new ModelFactory(this.GetType().Assembly);
            EventsHelper.ModelFactory = modelFactory;
            var domainEventFactory = EventFactory.Create(modelFactory, () => TimeSpan.Zero);
            var driverStateKeeper = _stateKeeper ?? new DriverStateKeeper();
            var sut = new SlaveController(
                motorsModel,
                slave,
                driverStateKeeper,
                domainEventFactory,
                _currentState as State?
            );
            return sut;
        }

        public ControllerBuilder<T, S> WithSettingsUrns(Urn[] urns)
        {
            _settingsUrns = urns;
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

        public ControllerBuilder<T, S> WithExecutionError()
        {
            SimulationResult = (CommandExecutionError.Create(_controllerBuilder.DeviceNode, new IDataModelValue[]
            {
                Property<MeasureStatus>.Create(CommandNode.status,MeasureStatus.Failure, TimeSpan.Zero)
            }),new CommunicationDetails(0,1));
            return _controllerBuilder;
        }
        
        public ControllerBuilder<T, S> ReturningResult(Error executionResult)
        {
            SimulationResult = (executionResult, new CommunicationDetails(0,1));
            return _controllerBuilder;
        }

    }

    public class ReadPropertiesDefinition<T, S> where T : class, ISlaveController where S : struct
    {
        
        private readonly ControllerBuilder<T, S> _controllerBuilder;
        public List<Result2<IDataModelValue[],CommunicationDetails>> SimulatedResults { get; }
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
                    SimulatedResults.Add((result.Value,new CommunicationDetails(1,0)));
                }
                else
                {
                    SimulatedResults.Add((result.Error,new CommunicationDetails(0,1)));
                }
            }
            return this;
        }

        public ReadPropertiesDefinition<T, S> ThenReturning(Result<IDataModelValue[]> result) => Returning(result);
        
        

        public ControllerBuilder<T, S> EndSimulation()
        {
            return _controllerBuilder;
        }



        public ReadPropertiesDefinition<T, S> WithSlaveCommunicationError()
        {
            SimulatedResults.Add((SlaveCommunicationError.Create(_controllerBuilder.DeviceNode),new CommunicationDetails(0,1)));
            return this;
        }

        
    }
}