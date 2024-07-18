using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.RuntimeFoundations.Factory
{

    public interface IDomainEventFactory
    {
        PropertiesChanged NewEvent(IEnumerable<IDataModelValue> modelValues);
        PropertiesChanged NewEvent(Urn group, IEnumerable<IDataModelValue> modelValues);
        Result<DomainEvent> NewEventResult(Urn urn, object value);
        Result<DomainEvent> NewEventResult(Urn group, Urn urn, object value);
        Result<PropertiesChanged> NewEventResult(IEnumerable<(Urn urn, object value)> properties);
        Func<TimeSpan> Clock { get; }
    }

    public class DomainEventFactory : IDomainEventFactory
    {
        private ModelFactory ModelFactory { get; }
        public Func<TimeSpan> Clock { get; }

        public DomainEventFactory(ModelFactory modelFactory, Func<TimeSpan> clock)
        {
            ModelFactory = modelFactory;
            Clock = clock;
        }

        public SlaveCommunicationOccured HealthyCommunicationOccured(DeviceNode deviceNode, CommunicationDetails communicationDetails)
        {
            return SlaveCommunicationOccured.CreateHealthy(deviceNode, Clock(), communicationDetails);
        }

        public SlaveCommunicationOccured ErrorCommunicationOccured(DeviceNode deviceNode, CommunicationDetails communicationDetails)
        {
            return SlaveCommunicationOccured.CreateError(deviceNode, Clock(), communicationDetails);
        }
        
        public DomainEvent FatalCommunicationOccured(DeviceNode deviceNode, CommunicationDetails communicationDetails)
        {
            return SlaveCommunicationOccured.CreateFatal(deviceNode, Clock(), communicationDetails);
        }
        
        public CommandRequested CommandRequested<T>(CommandUrn<T> commandUrn, T arg)
        {
            return Events.CommandRequested.Create(commandUrn, arg, Clock());
        }
        
        public DomainEvent NotifyOnTimeoutRequested(PropertyUrn<Duration> timerUrn)
        {
            return Events.NotifyOnTimeoutRequested.Create(timerUrn, Clock());
        }

        public PropertiesChanged PropertiesChanged<T>(PropertyUrn<T> urn, T value)
        {
            return Events.PropertiesChanged.Create(urn, value, Clock());
        }

        public PropertiesChanged PropertiesChanged<T>(Urn group, PropertyUrn<T> urn, T value)
        {
            return Events.PropertiesChanged.Create(group, urn, value, Clock());
        }

        public SlaveRestarted SlaveRestarted(DeviceNode deviceNode) 
        {
            return Events.SlaveRestarted.Create(deviceNode, Clock());
        }

        public Result<DomainEvent> NewEventResult(Urn urn, object value) => NewEventResult(null, urn, value);

        public Result<DomainEvent> NewEventResult(Urn gr, Urn urn, object value)
        {
            if (urn is PropertyUrn<Duration>)
                return Events.NotifyOnTimeoutRequested.Create(urn,Clock());
            
            var resultEvent =
                from mo in ModelFactory.CreateWithLog(urn, value,Clock())
                from evt in CreateDomainEvent(gr, mo, Clock())
                select evt;
                
            return resultEvent;
        }
        
        public Result<PropertiesChanged> NewEventResult(IEnumerable<(Urn urn, object value)> properties) =>
            properties.Select(prop => ModelFactory.CreateWithLog(prop.urn, prop.value, Clock()))
                .Traverse()
                .Select(modelValues => Events.PropertiesChanged.Create(modelValues.Cast<IDataModelValue>(), Clock()));

        public PropertiesChanged NewEvent(IEnumerable<IDataModelValue> modelValues)
            => Events.PropertiesChanged.Create(modelValues, Clock());

        public PropertiesChanged NewEvent(Urn group, IEnumerable<IDataModelValue> modelValues)
            => Events.PropertiesChanged.Create(group, modelValues, Clock());

        private static Result<DomainEvent> CreateDomainEvent( Urn group, object modelObject, TimeSpan currentTime)
        {
            return modelObject switch 
            {
                IModelCommand cmd => Events.CommandRequested.Create(cmd, currentTime),
                IDataModelValue prop => Events.PropertiesChanged.Create(group, new[] {prop}, currentTime),
                _ => Result<DomainEvent>.Create(new Error("",""))
            };
        }


       
    }
    
    
    public static class EventFactory
    {
        public static DomainEventFactory Create(ModelFactory modelFactory, Func<TimeSpan> clock)
        {
            return new DomainEventFactory(modelFactory, clock);
       }
  
    }
}