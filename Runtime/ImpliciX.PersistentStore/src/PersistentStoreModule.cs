using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.Language.Store;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.PersistentStore
{
    public class PersistentStoreModule : ImpliciXModule
    {
        public static ImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new PersistentStoreModule(moduleName, rtDef.ModelFactory,
                rtDef.Module<PersistentStoreModuleDefinition>(), rtDef.Options);

        public PersistentStoreModule(string id,
            ModelFactory modelFactory,
            PersistentStoreModuleDefinition persistentStoreModuleDefinition,
            ApplicationOptions options) : base(id)
        {
            var settingKinds = new Dictionary<Type, int>()
            {
                { typeof(UserSettingUrn<>), 1 },
                { typeof(VersionSettingUrn<>), 2 },
                { typeof(FactorySettingUrn<>), 3 },
                { typeof(PersistentCounterUrn<>), 4 },
            };
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<PersistentStoreSettings>("Modules", Id),
                initResources: provider =>
                {
                    var settings = provider.GetSettings<PersistentStoreSettings>(Id);
                    var internalBus = provider.GetService<IEventBusWithFirewall>();
                    var storage = PersistentStore.Create(settings, options.LocalStoragePath, modelFactory);
                    var clock = provider.GetService<IClock>();
                    var domainEventFactory = new DomainEventFactory(modelFactory, clock.Now);
                    PersistentStoreInitializer.AddMissingDefaultValues(settingKinds, storage.Writer, storage.Reader, persistentStoreModuleDefinition.DefaultUserSettings, clock.Now());
                    PersistentStoreInitializer.InsertDefaultValuesIfEmpty(settingKinds, storage.Writer, storage.Reader, persistentStoreModuleDefinition.DefaultVersionSettings, clock.Now());
                    var persistentStoreUpdater = new PersistentStoreUpdater(storage.Writer);
                    var persistentStoreCleaner = new PersistentStoreCleaner(storage.Cleaner, domainEventFactory, persistentStoreModuleDefinition);
                    var modelInstanceBuilder = new ModelInstanceBuilder(modelFactory);
                    var persistentStorePublishers = settingKinds
                        .Select(ki => new PersistentStorePublisher(ki.Key, ki.Value, internalBus.Publish, storage.Reader,
                            modelInstanceBuilder.Create, storage.Listener, clock.Now)).ToArray();
                    return new object[]
                    {
                        persistentStorePublishers, persistentStoreUpdater, settings, internalBus, storage.Listener, modelFactory, storage.Reader,
                        persistentStoreCleaner
                    };
                },
                createFeature: assets =>
                {
                    var firewallHandler = CreateFirewallHandler(assets.Get<IEventBusWithFirewall>(), persistentStoreModuleDefinition.StartFirewall,
                        persistentStoreModuleDefinition.StopFirewall, persistentStoreModuleDefinition.FirewallRules);
                    var persistentStoreUpdater = assets.Get<PersistentStoreUpdater>();
                    var persistentStoreCleaner = assets.Get<PersistentStoreCleaner>();
                    var persistentStorePublishers = assets.Get<PersistentStorePublisher[]>();
                    foreach (var persistentStorePublisher in persistentStorePublishers)
                        persistentStorePublisher.Run();
                    return DefineFeature()
                        .Handles(persistentStoreUpdater.HandlePersistentChangeRequested(settingKinds))
                        .Handles(firewallHandler.Item1, firewallHandler.Item2)
                        .Handles(persistentStoreCleaner.HandleCommandRequested(settingKinds), persistentStoreCleaner.CanHandle)
                        .Create();
                });
        }

        public static (DomainEventHandler<CommandRequested>, Func<CommandRequested, bool>) CreateFirewallHandler(
            IEventBusWithFirewall bus,
            CommandUrn<Literal> startFirewall,
            CommandUrn<NoArg> stopFirewall,
            IDictionary<string, IEnumerable<FirewallRule>> firewallRules)
        {
            var emptyRules = new List<FirewallRuleImplementation>();

            List<FirewallRuleImplementation> GetRules(string ruleSet)
                => firewallRules[ruleSet]
                    .Select(rule
                        => rule.Urns.Length switch
                        {
                            1 =>  new FirewallRuleImplementation(rule.ModuleId, rule.Direction, rule.Decision, @event
                                => @event is CommandRequested commandRequested && commandRequested.Urn == rule.Urns[0]),
                            _ =>  new FirewallRuleImplementation(rule.ModuleId, rule.Direction, rule.Decision, _ => true)
                        })
                    .ToList();

            DomainEventHandler<CommandRequested> labModeHandler =
                evt =>
                {
                    var ruleset = evt.Urn switch
                    {
                        var u when u == startFirewall => GetRules(evt.Arg.ToString()),
                        var v when v == stopFirewall => emptyRules,
                        _ => emptyRules
                    };
                    bus.AddFirewallRuleSet(ruleset);
                    return Array.Empty<DomainEvent>();
                };
            return (labModeHandler, requested => requested.Urn == startFirewall || requested.Urn == stopFirewall);
        }
    }
}