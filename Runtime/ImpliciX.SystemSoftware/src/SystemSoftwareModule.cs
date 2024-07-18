using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImpliciX.Data;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SystemSoftware.States;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.SystemSoftware
{
    public class SystemSoftwareModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef) =>
            new SystemSoftwareModule(moduleName, rtDef.Module<SystemSoftwareModuleDefinition>(),
                rtDef.ModelDefinition);

        public SystemSoftwareModule(string id, SystemSoftwareModuleDefinition moduleDefinition, Assembly modelAssembly) : base(id)
        {
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<SystemSoftwareSettings>("Modules", Id),
                initResources: provider =>
                {
                    var mf = new ModelFactory(modelAssembly);
                    var clock = provider.GetService<IClock>();
                    var settings = provider.GetSettings<SystemSoftwareSettings>(Id);
                    var internalBus = provider.GetService<IEventBusWithFirewall>();
                    var context = new Context(moduleDefinition.SoftwareMap)
                    {
                        SupportedForUpdate = settings.SupportedForUpdate.Select(s => moduleDefinition.SoftwareMap[s]).ToHashSet(),
                        FallbackReleaseManifestPath = settings.FallbackReleaseManifestPath,
                        CurrentReleaseManifestPath = settings.CurrentReleaseManifestPath,
                        UpdateManifestPath = settings.UpdateManifestFilePath,
                        AlwaysUpdate = settings.AlwaysUpdate.Select(s => moduleDefinition.SoftwareMap[s]).ToHashSet()
                    };
                    var runner = SystemSoftwareRunner.Create(moduleDefinition, context, PackageLoader.Load, new DomainEventFactory(mf, clock.Now));
                    return new object[]
                    {
                        settings,
                        clock,
                        internalBus,
                        runner
                    };
                },
                createFeature: assets =>
                {
                    var internalBus = assets.Get<IEventBusWithFirewall>();
                    var runner = assets.Get<Runner<Context>>();
                    var events = runner.Activate();
                    PublishEvents(internalBus, events);
                    return DefineFeature()
                        .Handles<PropertiesChanged>(runner.Handle, runner.CanHandle)
                        .Handles<CommandRequested>(runner.Handle, runner.CanHandle)
                        .Handles<StartingCompleted>(runner.Handle, runner.CanHandle)
                        .Handles<UpdateCompleted>(runner.Handle, runner.CanHandle)
                        .Handles<UpdateCanceled>(runner.Handle, runner.CanHandle)
                        .Create();
                }
            );
        }


        private static void PublishEvents(IEventBus internalBus, IEnumerable<DomainEvent> events)
        {
            foreach (var @event in events)
            {
                internalBus.Publish(@event);
            }
        }
    }
}