using ImpliciX.Data.ColdDb;
using ImpliciX.Data.Factory;
using ImpliciX.Data.Records;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Data.Records.HotRecords;
using ImpliciX.Language.Records;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Records;

public sealed class RecordsModule : ImpliciXModule
{
    public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
        => new RecordsModule(moduleName, rtDef);

    private RecordsModule(string id, ApplicationRuntimeDefinition rtDef) : base(id)
    {
        DefineModule(
            initDependencies: cfg => cfg.AddSettings<RecordsSettings>("Modules", Id),
            initResources: provider =>
            {
                var moduleDef = rtDef.Module<RecordsModuleDefinition>() 
                                ?? throw new InvalidOperationException("No definition found for Records module");
                var options = rtDef.Options;
                var settings = provider.GetSettings<RecordsSettings>(Id);

                var safeLoad = rtDef.Options.StartMode == StartMode.Safe;
                
                var clock = provider.GetService<IClock>();
                
                var coldRecordsDb = ColdRecordsDb.LoadOrCreate(
                        collectionUrns:moduleDef.Records.Select(m=>m.Urn).ToArray(),
                        folderPath:Path.Combine(options.LocalStoragePath, "external", "records"), 
                        safeLoad:safeLoad, 
                        rotatePolicy:new OneCompressedFileByDay<RecordsDataPoint>());
                
                var hotRecordsDb =  new HotRecordsDb(moduleDef.Records,Path.Combine(options.LocalStoragePath, "internal", "records"), "records", safeLoad);
                
                var identityGenerator = new TimeBasedIdentityGenerator(clock);
                
                var modelFactory = new ModelFactory(rtDef.ModelDefinition);
                var recordsService = new RecordsService(moduleDef.Records, coldRecordsDb, hotRecordsDb, modelFactory, identityGenerator);
                var internalBus = provider.GetService<IEventBusWithFirewall>();
                return new object[] {recordsService, internalBus, coldRecordsDb, clock, identityGenerator};
            },
            createFeature: assets =>
            {
                var service = assets.Get<RecordsService>();
                return
                    DefineFeature()
                        .Handles<PropertiesChanged>(service.HandlePropertiesChanged)
                        .Handles<CommandRequested>(service.HandleCommandRequested, service.IsWriterCmd)
                        .Create();
            },
            onModuleStart: assets =>
            {
                var internalBus = assets.Get<IEventBusWithFirewall>();
                var service = assets.Get<RecordsService>();
                var clock = assets.Get<IClock>();
                internalBus.Publish(service.PublishAllRecordsHistory(clock.Now()));
            }
        );
    }
}

public class RecordsSettings
{
}