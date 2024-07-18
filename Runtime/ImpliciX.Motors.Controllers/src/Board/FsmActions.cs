using System;
using System.Linq;
using ImpliciX.Driver.Common;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.Motors.Controllers.Board
{
    public class FsmActions : FsmActionsBase
    {
        public FsmActions(MotorsModuleDefinition motorsModuleDefinition, IBoardSlave boardSlave, DomainEventFactory domainEventFactory) : base(boardSlave, domainEventFactory)
        {
            _motorsModuleDefinition = motorsModuleDefinition;
            _group = boardSlave.DeviceNode.Urn.Plus(boardSlave.Name);
        }

        private readonly Urn _group;
        private MotorsModuleDefinition _motorsModuleDefinition;

        protected override DomainEvent[] InterpretReadSuccess(IDataModelValue[] values, CommunicationDetails communicationDetails)
        {
            var hasFailedMeasures = values.Any(mv => mv.ModelValue().Equals(MeasureStatus.Failure));
            var motorsRunningPropertiesChanged = _domainEventFactory.PropertiesChanged(
                _group,
                _motorsModuleDefinition.MotorsStatusUrn,
                MotorsStatus.Running);    
            return hasFailedMeasures switch
            {
                false => SuccessOutput(values, communicationDetails, motorsRunningPropertiesChanged),
                true => SuccessOutput(values, communicationDetails),
            };
        }
        
        public  DomainEvent[] TurnPowerSupplyOn()
        {
            
            return new DomainEvent[]
            {
                _domainEventFactory.CommandRequested(_motorsModuleDefinition.PowerCommand, PowerSupply.On),
                _domainEventFactory.CommandRequested(_motorsModuleDefinition.SupplyCommand, PowerSupply.On),
                _domainEventFactory.NotifyOnTimeoutRequested(_motorsModuleDefinition.SettingsSupplyDelayTimerUrn),
                _domainEventFactory.PropertiesChanged(_motorsModuleDefinition.MotorsStatusUrn, MotorsStatus.Starting),
            };
        }
        
        public DomainEvent[] TurnPowerSupplyOff() =>
            new DomainEvent[]
            {
                _domainEventFactory.CommandRequested(_motorsModuleDefinition.SupplyCommand, PowerSupply.Off),
                _domainEventFactory.CommandRequested(_motorsModuleDefinition.PowerCommand, PowerSupply.Off),
            };

        public  DomainEvent[] ExecuteCommand(CommandRequested @event)
        {
            return _boardSlave
                .ExecuteCommand(@event.Urn, @event.Arg)
                .Match((error,details) => InterpretCommandError((CommandExecutionError)error,details),
                    (values,details) => SuccessOutput(values,details)
                );

        }
        
        public DomainEvent[] IgnoreCommand(CommandRequested @event)
        {
            Log.Debug("Command {@urn} is rejected", @event.Urn);
            return Array.Empty<DomainEvent>();
        }

        protected override DomainEvent[] InterpretCommandError(CommandExecutionError error, CommunicationDetails communicationDetails) =>
            new DomainEvent[]
            {
                _domainEventFactory.NewEvent(error.ErrorProperties),
                _domainEventFactory.FatalCommunicationOccured(_boardSlave.DeviceNode, communicationDetails),
            };

        public  DomainEvent[] PublishStoppedStatus() => 
            new DomainEvent[]
            {
                _domainEventFactory.PropertiesChanged(_motorsModuleDefinition.MotorsStatusUrn, MotorsStatus.Stopped),
                _domainEventFactory.HealthyCommunicationOccured(_boardSlave.DeviceNode, new CommunicationDetails(0,0))
            };
        
        public bool IsStartTimeoutOccured(DomainEvent @event) =>
            @event is TimeoutOccured timeoutOccured &&
            timeoutOccured.TimerUrn == _motorsModuleDefinition.SettingsSupplyDelayTimerUrn;

        public bool IsMotorsSwitchStart(DomainEvent @event) =>
            @event is CommandRequested command &&
            command.Urn == _motorsModuleDefinition.SwitchCommandUrn &&
            (MotorStates) command.Arg == MotorStates.Start;

        public bool IsMotorsSwitchStop(DomainEvent @event) =>
            @event is CommandRequested command &&
            command.Urn == _motorsModuleDefinition.SwitchCommandUrn &&
            (MotorStates) command.Arg == MotorStates.Stop;

        public bool IsHeatPumpRestarted(DomainEvent @event) =>
            @event is SlaveRestarted slaveRestarted && 
            slaveRestarted.DeviceNode.Equals(_motorsModuleDefinition.HeatPumpDeviceNode);

        
    }
}