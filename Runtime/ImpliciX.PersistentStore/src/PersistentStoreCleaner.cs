using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Language.Store;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.PersistentStore
{
    public class PersistentStoreCleaner 
    {
        private readonly ICleanStorage _storageCleaner;
        private readonly IDomainEventFactory _eventFactory;
        private readonly PersistentStoreModuleDefinition _moduleDefinition;

        public PersistentStoreCleaner(ICleanStorage storageCleaner, IDomainEventFactory eventFactory, PersistentStoreModuleDefinition moduleDefinition)
        {
            _storageCleaner = storageCleaner;
            _eventFactory = eventFactory;
            _moduleDefinition = moduleDefinition;
        }

        public DomainEventHandler<CommandRequested> HandleCommandRequested(IDictionary<Type, int> settingKinds)
            => @event =>
            {
                if (CanHandle(@event))
                {
                    Contract.Assert(settingKinds.ContainsKey(typeof(VersionSettingUrn<>)), "No db found for VersionSettings.");
                    var db = settingKinds[typeof(VersionSettingUrn<>)];
                    return (
                            from _ in _storageCleaner.FlushDb(db)
                            from pc in _eventFactory.NewEventResult(_moduleDefinition.CleanVersionSettings.status,MeasureStatus.Success)     
                            select new DomainEvent[]{pc})
                        .GetValueOrDefault(Array.Empty<DomainEvent>());
                }
                return Array.Empty<DomainEvent>();
            };

        public bool CanHandle(DomainEvent @event) =>
            @event is CommandRequested cr && cr.Urn.Equals(_moduleDefinition.CleanVersionSettings?.command);
    }
}