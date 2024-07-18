using System;
using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.MmiHost.DBusProxies;
using ImpliciX.MmiHost.Services;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using Tmds.DBus;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;
using static ImpliciX.Language.Core.SideEffect;
using static ImpliciX.MmiHost.Constants;

namespace ImpliciX.MmiHost
{
    public class MmiHostModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new MmiHostModule(moduleName, rtDef.Module<MmiHostModuleDefinition>(), rtDef.ModelDefinition);

        public MmiHostModule(string id, MmiHostModuleDefinition moduleDefinition, Assembly modelDefinition) : base(id)
        {
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<MmiHostSettings>("Modules", Id),
                initResources: provider =>
                {
                    var settings = provider.GetSettings<MmiHostSettings>(Id);
                    var modelFactory = new ModelFactory(new[]{modelDefinition});
                    var time = provider.GetService<IClock>();
                    var domainEventFactory = EventFactory.Create(modelFactory, time.Now);
                    var proxyRauc = Connection.System.CreateProxy<IRaucInstallProxy>(IRaucInstallProxy.ServiceName, IRaucInstallProxy.Path);
                    var bspService = new BspService(moduleDefinition, domainEventFactory, proxyRauc);
                    var bus = provider.GetService<IEventBusWithFirewall>();
                    return new object[] { settings, domainEventFactory, bspService, bus };
                },
                createFeature: assets =>
                {
                    var settings = assets.Get<MmiHostSettings>();
                    var domainEventFactory = assets.Get<IDomainEventFactory>();
                    var bspService = assets.Get<BspService>();
                    return DefineFeature()
                        .Handles<PropertiesChanged>(PropertiesChangedEventHandler(moduleDefinition, settings))
                        .Handles<CommandRequested>(StartUpdateEventHandler(moduleDefinition, settings, domainEventFactory), @event => moduleDefinition.IsApplicationUpdate(@event.Urn))
                        .Handles<CommandRequested>(CommitUpdateEventHandler(), @event => moduleDefinition.IsCommitCommand(@event.Urn))
                        .Handles<CommandRequested>(RestartBoilerAppEventHandler(), @event => moduleDefinition.IsRestartBoilerAppCommand(@event.Urn))
                        .Handles<CommandRequested>(RebootEventHandler(), @event => moduleDefinition.IsRebootCommand(@event.Urn))
                        .Handles<CommandRequested>(bspService.Handle, bspService.CanHandle)
                        .Handles<SystemTicked>(bspService.Handle, bspService.CanHandle)
                        .Create();
                },
                onModuleStart: assets =>
                {
                    var bus = assets.Get<IEventBusWithFirewall>();
                    var bspService = assets.Get<BspService>();
                    RaucService.MarkAsGood(FORCE_RO_FILE);
                    bspService.BspVersions().Tap(@evt => bus.Publish(evt));
                });
        }

        private DomainEventHandler<CommandRequested> RestartBoilerAppEventHandler()
        {
            return commandRequested =>
            {
                SystemDService.RestartBoilerAppUnit();
                return Array.Empty<DomainEvent>();
            };
        }
        
        private DomainEventHandler<CommandRequested> RebootEventHandler()
        {
            return commandRequested =>
            {
                SystemService.RestartSystem();
                return Array.Empty<DomainEvent>();
            };
        }
        
        
        private DomainEventHandler<CommandRequested> CommitUpdateEventHandler()
        {
            return commandRequested =>
            {
                Log.Debug("MmiHostModule receive commit update.");
                RaucService.ChangeSlot();
                return Array.Empty<DomainEvent>();
            };
        }
        
        private DomainEventHandler<CommandRequested> StartUpdateEventHandler(MmiHostModuleDefinition moduleDefinition,
            MmiHostSettings settings, IDomainEventFactory domainEventFactory)
        {
            return @event =>
            {
                Log.Debug($"MmiHostModel Start Update {@event.Urn}");
                var updateResult =  
                    from packageContent in SafeCast<PackageContent>(@event.Arg)
                    from softwareName in moduleDefinition.IdentifyTargetedSoftware(packageContent.DeviceNode)
                    from activePartition in RaucService.GetActivePartition()
                    let targetPartition = RaucService.GetOppositePartition(activePartition)
                    let softwareUpdate = new SoftwareUpdate(softwareName,packageContent.Revision,packageContent.ContentFile, SOFTWARE_INSTALLATION_PATH, BOOT_FS_PATH, targetPartition)
                    let _ = AppsUpdateService.LaunchUpdate(softwareUpdate)
                    from pct in domainEventFactory.NewEventResult(packageContent.DeviceNode.update_progress, Percentage.FromFloat(1f).Value)
                    select new DomainEvent[] { pct };

                return updateResult.Match(error =>
                {
                    Log.Error("MmiHostModel update error : {@msg}", error.Message);
                    return Array.Empty<DomainEvent>();
                }, events => events);
            };
        }

        private DomainEventHandler<PropertiesChanged> PropertiesChangedEventHandler(MmiHostModuleDefinition moduleDefinition, MmiHostSettings settings) =>
            propertiesChanged =>
            {
                var brightnessSettings = propertiesChanged.ModelValues
                    .SingleOrDefault(mv => mv.Urn.ToString() == moduleDefinition.Brightness);
                if (brightnessSettings != null) BrightnessService.SetBrightness((Percentage)brightnessSettings.ModelValue(), BACKLIGHT_FILE_PATH);

                return Array.Empty<DomainEvent>();
            };
        
    }
}