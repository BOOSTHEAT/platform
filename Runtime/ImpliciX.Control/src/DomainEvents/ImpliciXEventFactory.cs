using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control.DomainEvents
{
    public class ImpliciXEventFactory : IDomainEventFactory
    {
        private IDomainEventFactory DomainEventFactory { get; }
        public Func<TimeSpan> Clock { get; }

        public static IDomainEventFactory Create(IDomainEventFactory domainEventFactory)
            => new ImpliciXEventFactory(domainEventFactory);

        private ImpliciXEventFactory(IDomainEventFactory domainEventFactory)
        {
            DomainEventFactory = domainEventFactory;
            Clock = domainEventFactory.Clock;
        }

        public Result<DomainEvent> NewEventResult(Urn urn, object value) =>
            value switch
            {
                EnumSequence statesChain => StateChanged.Create(new DataModelValue<EnumSequence>(urn, statesChain, Clock()), Clock()),
                _ => DomainEventFactory.NewEventResult(urn, value)
            };

        public Result<DomainEvent> NewEventResult(Urn group, Urn urn, object value) =>
            value switch
            {
                EnumSequence statesChain => StateChanged.Create(new DataModelValue<EnumSequence>(urn, statesChain, Clock()), Clock()),
                _ => DomainEventFactory.NewEventResult(group, urn, value)
            };

        public PropertiesChanged NewEvent(IEnumerable<IDataModelValue> modelValues)
            => DomainEventFactory.NewEvent(modelValues);
        
        public PropertiesChanged NewEvent(Urn group, IEnumerable<IDataModelValue> modelValues)
            => DomainEventFactory.NewEvent(group, modelValues);
        
        public Result<PropertiesChanged> NewEventResult(IEnumerable<(Urn urn, object value)> properties)
            => DomainEventFactory.NewEventResult(properties);
    }
}