using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Control;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;

namespace ImpliciX.Control
{
    public class ControlModule : ImpliciXModule
    {
        public static ImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new ControlModule(moduleName, rtDef.StateMachines, rtDef.ModelDefinition);
        
        public ControlModule(string id, ISubSystemDefinition[] stateMachines, Assembly modelDefinition) : base(id)
        {
          DefineModule(
                initDependencies: cfg => cfg.AddSettings<ControlSettings>("Modules",Id),
                initResources: provider =>
                {
                    var settings = provider.GetSettings<ControlSettings>(Id);
                    var eventBus = provider.GetService<IEventBusWithFirewall>();
                    var clock = provider.GetService<IClock>();    
                    return new object[]{settings, eventBus, clock};
                },
                createFeature: assets =>
                {
                    var clock = assets.Get<IClock>();
                    var domainEventFactory = EventFactory.Create(new ModelFactory(modelDefinition), clock.Now);
                    return ModuleFactory.CreateFeature(
                        assets.Get<IEventBusWithFirewall>(), 
                        domainEventFactory,
                        f => new UserDefinedControlSystem(f, stateMachines));
                });
        }
    }
}