using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.TestsCommon
{
    public static class EventsHelper
    {
        private static ModelFactory _modelFactory;

        public static DomainEventFactory DomainEventFactory(TimeSpan at) =>
            EventFactory.Create(ModelFactory, () => at);

        public static ModelFactory ModelFactory
        {
            get
            {
                if (_modelFactory == null)
                    throw new Exception("Missing Model Factory");
                return _modelFactory;
            }
            set => _modelFactory = value;
        }

        public static NotifyOnTimeoutRequested EventNotifyOnTimeoutRequested(Urn urn, TimeSpan at) => NotifyOnTimeoutRequested.Create(urn, at);
        
        public static TimeoutOccured EventTimeoutOccured(Urn urn, TimeSpan at) => TimeoutOccured.Create(urn, at, Guid.Empty);
        
        public static TimeoutOccured EventTimeoutOccured(Urn urn, TimeSpan at, Guid requestId) => TimeoutOccured.Create(urn, at, requestId);

        public static CommandRequested EventCommandRequested(Urn urn, object arg, TimeSpan at)
        {
            if (urn is CommandUrn<NoArg> && arg is null) arg = default(NoArg);
            var result = DomainEventFactory(at).NewEventResult(urn, arg);
            if (result.IsError)
            {
                throw new Exception(result.Error.Message);
            }
            return (CommandRequested) result.GetValueOrDefault();
        }

        public static PropertiesChanged EventPropertyChanged((Urn urn, object value)[] properties, TimeSpan at)
        {
            return PropertiesChanged.Create(properties.Select(c => CreateProperty(c.urn, c.value, at)), at);
        }
        public static PropertiesChanged EventPropertyChanged(TimeSpan at, params (Urn urn, object value)[] properties )
        {
            return PropertiesChanged.Create(properties.Select(c => CreateProperty(c.urn, c.value, at)), at);
        }
        
        public static PropertiesChanged EventPropertyChanged(TimeSpan at,Urn group, params (Urn urn, object value)[] properties )
        {
            return PropertiesChanged.Create(group,properties.Select(c => CreateProperty(c.urn, c.value, at)), at);
        }
        
        public static PropertiesChanged EventPropertyChanged(IDataModelValue[] properties, TimeSpan at)
        {
            return PropertiesChanged.Create(properties, at);
        }

        public static PropertiesChanged EventPropertyChanged(Urn group, IDataModelValue[] properties, TimeSpan at)
        {
            return PropertiesChanged.Create(group, properties, at);
        }

        public static PersistentChangeRequest EventPersistentChangeRequested((Urn urn, object value)[] properties, TimeSpan at)
        {
            return PersistentChangeRequest.Create(properties.Select(c => CreateProperty(c.urn, c.value, at)), at);
        }

        public static SystemTicked EventSystemTicked(ushort baseFrequencyMs, TimeSpan at)
        {
            return SystemTicked.Create(baseFrequencyMs, 0);
        }

        public static PropertiesChanged EventPropertyChanged(Urn urn, object value, TimeSpan at)
        {
            var property = CreateProperty(urn, value, at);
            return PropertiesChanged.Create(new[] {property}, at);
        }

        private static IDataModelValue CreateProperty(Urn urn, object value, TimeSpan at)
        {
            var modelObjectResult = ModelFactory.Create(urn, value, at);
            var property = (IDataModelValue) modelObjectResult.Value;
            Contract.Assert(property != null, $"{urn} : {modelObjectResult.Error?.Message}");
            return property;
        }

        public static IEnumerable<IDataModelValue> CollectProperties(this IEnumerable<DomainEvent> source)
        {
            return source.FilterEvents<PropertiesChanged>().SelectMany(pc => pc.ModelValues);
        }
        public static IEnumerable<T> FilterEvents<T>(this IEnumerable<DomainEvent> source, params Urn[] urns)
            where T : DomainEvent
        {
            if (typeof(T) == typeof(PropertiesChanged) && urns.Length > 0)
            {
                return source.Where(s => s is PropertiesChanged)
                    .Cast<PropertiesChanged>()
                    .Where(pc => pc.ModelValues.Any(mv => urns.Contains(mv.Urn))).Cast<T>();
            }

            if (typeof(T) == typeof(PropertiesChanged) && urns.Length == 0)
            {
                return source.Where(s => s is PropertiesChanged)
                    .Cast<PropertiesChanged>()
                    .Cast<T>();
            }

            if (typeof(T) == typeof(CommandRequested) && urns.Length > 0)
            {
                return source.Where(s => s is CommandRequested)
                    .Cast<CommandRequested>().Where(c => urns.Contains(c.Urn)).Cast<T>();
            }

            return source.Where(s => s is T).Cast<T>();
        }
    }
}