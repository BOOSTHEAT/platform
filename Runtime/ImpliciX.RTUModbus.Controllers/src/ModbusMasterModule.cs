using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Driver.Common.Buffer;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.Driver.Common.State;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.RTUModbus.Controllers.BHBoard;
using ImpliciX.RTUModbus.Controllers.BrahmaBoard;
using ImpliciX.RTUModbus.Controllers.Helpers;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.RTUModbus.Controllers
{
    public class ModbusMasterModule : ImpliciXModule
    {
        public static ImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef) =>
            new ModbusMasterModule(moduleName, rtDef.ModelDefinition,
                rtDef.Module<DriverModbusModuleDefinition>().ModbusSlavesManagement,
                rtDef.Module<DriverModbusModuleDefinition>().Slaves);

        public ModbusMasterModule(string id, Assembly modelDefinition, ModbusSlaveModel slaveModel,
           Func<ModbusSlaveDefinition>[] slaveDefinitions) : base(id)
        {
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<ModbusSettings>("Modules", id),
                initResources: provider =>
                {
                    var time = provider.GetService<IClock>();
                    var modelFactory = new ModelFactory(new[] { modelDefinition, typeof(microcontroller).Assembly });
                    var modbusSettings = provider.GetSettings<ModbusSettings>(Id);
                    var bus = provider.GetService<IEventBusWithFirewall>();
                    var domainEventFactory = EventFactory.Create(modelFactory, time.Now);
                    var driverStateKeeper = new DriverStateKeeper();

                    var controllers = TcpControllersCollection.Create(id, modbusSettings, slaveDefinitions, slaveModel, time, domainEventFactory,
                        driverStateKeeper);

                    return new object[]
                    {
                        time,
                        modbusSettings,
                        bus,
                        driverStateKeeper,
                        domainEventFactory,
                        controllers,
                    };
                },
                createFeature: assets =>
                {
                    var controllers = assets.Get<IControllersCollection>();
                    var clock = assets.Get<IClock>();
                    var settings = assets.Get<ModbusSettings>();
                    var eventsHandler = settings.Buffered
                        ? BufferedController.BufferedHandler(controllers.HandleDomainEvent, ModbusCommandRequestFactory.Create(), clock)
                        : controllers.HandleDomainEvent;
                    var feature = DefineFeature()
                        .Handles<CommandRequested>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<ExitBootloaderCommandSucceeded>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<ProtocolErrorOccured>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<ActivePartitionDetected>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<FirstFrameSuccessfullySent>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<BoardUpdateRunningDetected>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<SendPreviousChunkSucceeded>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<SendChunksFinished>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<SetActivePartitionSucceeded>(@event => eventsHandler(@event), controllers.CanHandle)
                        .Handles<FaultedDetected>(@event => controllers.HandleDomainEvent(@event), controllers.CanHandle)
                        .Handles<NotFaultedDetected>(@event => controllers.HandleDomainEvent(@event), controllers.CanHandle)
                        .Handles<TimeoutOccured>(@event => controllers.HandleDomainEvent(@event), controllers.CanHandle)
                        .Handles<RegulationEntered>(@event => controllers.HandleDomainEvent(@event), controllers.CanHandle)
                        .Handles<RegulationExited>(@event => controllers.HandleDomainEvent(@event), controllers.CanHandle)
                        .Handles<PropertiesChanged>(@event => controllers.HandleDomainEvent(@event), controllers.CanHandle)
                        .Handles<SystemTicked>(@event => eventsHandler(@event))
                        .Create();
                    return feature;
                }
            );

            DefineSchedulingUnit(
                assets => schedulingUnit =>
                {
                    var controllers = assets.Get<IControllersCollection>();
                    var bus = assets.Get<IEventBusWithFirewall>();
                    bus.Publish(controllers.Activate());
                },
                _ => __ => { }
            );
        }
    }
}