using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Driver;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Scheduling;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.FmuDriver
{
    public class DriverFmuModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new DriverFmuModule(moduleName, rtDef.ModelDefinition, rtDef.Module<DriverFmuModuleDefinition>());

        public DriverFmuModule(string id, Assembly modelDefinition, DriverFmuModuleDefinition moduleDefinition) : base(id)
        {
            DefineModule(
                cfg => cfg.AddSettings<FmuDriverSettings>("Modules", Id),
                provider =>
                {
                    var clock = provider.GetService<IClock>();
                    return new object[]
                    {
                        provider.GetSettings<FmuDriverSettings>(Id),
                        new ModelFactory(modelDefinition),
                        new FmuContext(clock, moduleDefinition, new FmuLogger()),
                        clock
                    };
                },
                assets =>
                {
                    var settings = assets.Get<FmuDriverSettings>();
                    var fmuContext = assets.Get<FmuContext>();
                    var modelFactory = assets.Get<ModelFactory>();
                    var clock = assets.Get<IClock>();
                    var canExecute = FmuService.CanExecuteCommand(moduleDefinition);
                    var canHandleQuery = FmuService.CanHandleQuery(fmuContext.FmuInstance);
                    return DefineFeature()
                        .Handles<Idle>(
                            FmuService.ReadState(moduleDefinition, modelFactory, fmuContext.FmuInstance, fmuContext.FmuReadVariables, clock, settings),
                            canHandleQuery)
                        .Handles<CommandRequested>(FmuService.FmuCommandHandler(moduleDefinition, modelFactory, fmuContext, clock), canExecute)
                        .Create();
                }
            );
        }
    }
}