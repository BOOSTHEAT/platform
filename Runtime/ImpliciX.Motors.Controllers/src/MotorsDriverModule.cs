using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Driver.Common.Buffer;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Buffer;
using ImpliciX.Motors.Controllers.Infrastructure;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;
using MotorsDriverSettings = ImpliciX.Motors.Controllers.Settings.MotorsDriverSettings;

namespace ImpliciX.Motors.Controllers
{
    public class MotorsDriverModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new MotorsDriverModule(moduleName, rtDef.ModelDefinition, rtDef.Module<MotorsModuleDefinition>());

        public MotorsDriverModule(string id, Assembly modelDefinition, MotorsModuleDefinition motorsModuleDefinition) : base(id)
        {
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<MotorsDriverSettings>("Modules", Id),
                initResources: provider =>
                {
                    var settings = provider.GetSettings<MotorsDriverSettings>(Id);
                    var driverStateKeeper = new DriverStateKeeper();
                    var time = provider.GetService<IClock>();
                    var modelFactory = new ModelFactory(new[] { modelDefinition });
                    var domainEventFactory = EventFactory.Create(modelFactory, time.Now);
                    var bus = provider.GetService<IEventBusWithFirewall>();
                    var motorsSlave = new MotorsSlave(
                        motorsModuleDefinition,
                        MotorsInfrastructure.Create(settings), time, settings
                    );

                    var controller = new SlaveController(motorsModuleDefinition, motorsSlave, driverStateKeeper, domainEventFactory);
                    return new object[]
                    {
                        bus,
                        time,
                        settings,
                        driverStateKeeper,
                        controller
                    };
                },
                createFeature: assets =>
                {
                    var controller = assets.Get<SlaveController>();
                    var time = assets.Get<IClock>();
                    var bufferedHandler = BufferedController.BufferedHandler(
                        controller.HandleDomainEvent,
                        MotorCommandRequestFactory.Create(),
                        time);

                    return DefineFeature()
                        .Handles<CommandRequested>(@event => bufferedHandler(@event), controller.CanHandle)
                        .Handles<TimeoutOccured>(@event => controller.HandleDomainEvent(@event), controller.CanHandle)
                        .Handles<SystemTicked>(@event => bufferedHandler(@event))
                        .Handles<SlaveRestarted>(@event => controller.HandleDomainEvent(@event))
                        .Handles<PropertiesChanged>(@event => controller.HandleDomainEvent(@event), controller.CanHandle)
                        .Create();
                });
        }
    }
}