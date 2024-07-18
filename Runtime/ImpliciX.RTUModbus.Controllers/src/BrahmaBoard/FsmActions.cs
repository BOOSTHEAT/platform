using System;
using System.Linq;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public class FsmActions : FsmActionsBase
    {
        private readonly BurnerNode _genericBurner;

        public FsmActions(IBrahmaBoardSlave boardSlave, DomainEventFactory domainEventFactory) : base(boardSlave,domainEventFactory)
        {
            _genericBurner = boardSlave.GenericBurner;
        }
        public DomainEvent[] PowerOn() => 
            ExecuteSlaveCommand(_genericBurner._private<brahma>()._power, PowerSupply.On);

        public DomainEvent[] PowerOff()
        {
            var result =
                from throttleResult in _boardSlave.ExecuteCommand(_genericBurner.burner_fan._throttle,
                    Percentage.FromFloat(0.0f).Value)
                from powerResult in _boardSlave.ExecuteCommand(_genericBurner._private<brahma>()._power, PowerSupply.Off)
                select Concatenate(throttleResult, powerResult);
            return result.Match((error,details) => InterpretCommandError(error as CommandExecutionError,details), SuccessOutput);
        }

        public DomainEvent[] FanThrottle(CommandRequested @event) => 
            ExecuteSlaveCommand(@event.Urn, @event.Arg);

        public DomainEvent[] ResetBrahma() => 
            ExecuteSlaveCommand(_genericBurner._private<brahma>()._reset, default(NoArg),_domainEventFactory.NotifyOnTimeoutRequested(_genericBurner.ignition_settings.ignition_reset_delay));
        
        public DomainEvent[] StopBrahmaReset() => ExecuteSlaveCommand(_genericBurner._private<brahma>()._stop, default(NoArg), _domainEventFactory.NotifyOnTimeoutRequested(_genericBurner.ignition_settings.ignition_reset_delay));

        public DomainEvent[] StartIgnition() => 
            ExecuteSlaveCommand(_genericBurner._private<brahma>()._start, default(NoArg),_domainEventFactory.NotifyOnTimeoutRequested(_genericBurner.ignition_settings.ignition_period));

        public DomainEvent[] StopIgnition() => ExecuteSlaveCommand(_genericBurner._private<brahma>()._stop, default(NoArg));
        public DomainEvent[] DetectStatus()
        {
            return  _boardSlave.ReadProperties(MapKind.MainFirmware)
                .Match(
                whenError: (err,details) => new DomainEvent[]{_domainEventFactory.ErrorCommunicationOccured(_boardSlave.DeviceNode, details)},
                whenSuccess: (values,details) =>
                {
                    foreach (var mv in values)
                    {
                        Log.Debug("{@slave} - {@urn} - {@modelValue}", _boardSlave.Name, mv.Urn.Value, mv.ModelValue()?.ToString());
                    }
                    
                    Log.Debug("[Burner] - Try to find value of URN : {@Urn}",_genericBurner.ignition_fault.measure.Value);
                    var ignitionStatus =
                        values.Where(it => it.Urn.Equals(_genericBurner.ignition_fault.measure))
                            .Select(it => (Fault?) it.ModelValue())
                            .FirstOrDefault();
                    if (ignitionStatus == null)
                        throw new ApplicationException(
                            $"[Burner] -no value for urn {_genericBurner.ignition_fault.measure.Value}");
                    
                    Log.Debug($"[Burner] - The ignition status is {ignitionStatus.ToString()}");
                    
                    if (ignitionStatus == Fault.NotFaulted)
                        return SuccessOutput(Array.Empty<IDataModelValue>(), details, NotFaultedDetected.Create(_boardSlave.DeviceNode, _genericBurner));
                    if (ignitionStatus== Fault.Faulted)
                        return SuccessOutput(Array.Empty<IDataModelValue>(), details,FaultedDetected.Create(_boardSlave.DeviceNode, _genericBurner));
                   
                    return SuccessOutput(Array.Empty<IDataModelValue>(), details);
                }
            );
        }
    }
}