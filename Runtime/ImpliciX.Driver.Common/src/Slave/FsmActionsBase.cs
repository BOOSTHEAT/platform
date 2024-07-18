using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.Errors;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.Driver.Common.Slave
{
    public abstract class FsmActionsBase
    {
        protected readonly IBoardSlave _boardSlave;
        protected readonly DomainEventFactory _domainEventFactory;
        public Urn Group { get; }

        public FsmActionsBase(IBoardSlave boardSlave, DomainEventFactory domainEventFactory)
        {
            _boardSlave = boardSlave;
            _domainEventFactory = domainEventFactory;
            Group = _boardSlave.DeviceNode.Urn.Plus(_boardSlave.Name);
        }
        
        public DomainEvent[] ReadMainFirmware(SystemTicked systemTicked)
        {
            if (systemTicked.TickCount % _boardSlave.ReadPaceInSystemTicks != 0)
                return Array.Empty<DomainEvent>();

            return _boardSlave.ReadProperties(MapKind.MainFirmware).Match(
                InterpretReadError, 
                InterpretReadSuccess);
        }

     

        protected virtual DomainEvent[] InterpretReadSuccess(IDataModelValue[] values, CommunicationDetails communicationDetails) => SuccessOutput(values, communicationDetails);

        protected virtual DomainEvent[] InterpretReadError(Error error, CommunicationDetails communicationDetails)
        {
            return new DomainEvent[] {_domainEventFactory.ErrorCommunicationOccured((DeviceNode)_boardSlave.DeviceNode, communicationDetails)};
        }
        protected virtual DomainEvent[] InterpretCommandError(CommandExecutionError error, CommunicationDetails communicationDetails)
        {
            return new[]
            {
                _domainEventFactory.NewEvent(error?.ErrorProperties),
                _domainEventFactory.FatalCommunicationOccured(_boardSlave.DeviceNode, communicationDetails),
            };
        }

        public DomainEvent[] ExecuteSlaveCommand(CommandRequested @event) =>
            ExecuteSlaveCommand(@event.Urn, @event.Arg);
            
        
        protected internal DomainEvent[] ExecuteSlaveCommand(Urn commandRequestedUrn, object commandRequestedArg, params DomainEvent[] additionalEvents)
        {
            return _boardSlave
                .ExecuteCommand(commandRequestedUrn, commandRequestedArg)
                .Match((error,details) => InterpretCommandError(error as CommandExecutionError,details),
                    (values,details) => SuccessOutput(values, details, additionalEvents)
                );
        }
        
        public DomainEvent[] RejectCommand() => 
            new DomainEvent[] {_domainEventFactory.FatalCommunicationOccured(_boardSlave.DeviceNode, new CommunicationDetails(0,0))};
        
        protected DomainEvent[] SuccessOutput(IEnumerable<IDataModelValue> values, CommunicationDetails communicationDetails)
        {
            if (values.Any())
            {
                return new DomainEvent[]
                {
                    _domainEventFactory.NewEvent(Group, values),
                    _domainEventFactory.HealthyCommunicationOccured(_boardSlave.DeviceNode,communicationDetails)
                };
            }
           
            return new DomainEvent[]
            {
                _domainEventFactory.HealthyCommunicationOccured(_boardSlave.DeviceNode, communicationDetails)
            };
        }

        protected DomainEvent[] SuccessOutput(IEnumerable<IDataModelValue> values, CommunicationDetails communicationDetails, params DomainEvent[] additionalEvents)
        {   
            return SuccessOutput(values, communicationDetails).Concat(additionalEvents).ToArray();
        }

        protected (IDataModelValue[], CommunicationDetails) Concatenate(
            params (IDataModelValue[], CommunicationDetails)[] results)
        {
            return (
                results.SelectMany(i => i.Item1).ToArray(),
                results.Select(i => i.Item2).Aggregate((a,b) => a+b)
            );
        }
    }
}