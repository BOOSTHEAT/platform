using ImpliciX.Api.TcpModbus.Infrastructure;
using ImpliciX.Language;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Api.TcpModbus
{
    public class ApiTcpModbusModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new ApiTcpModbusModule(moduleName, rtDef.Module<TcpModbusApiModuleDefinition>());

        public ApiTcpModbusModule(string id, TcpModbusApiModuleDefinition moduleDefinition) : base(id)
        {
          DefineModule(
                initDependencies: configurator => configurator.AddSettings<ModbusTcpSettings>("Modules",Id),
                initResources: (provider) =>
                {
                    var clock = provider.GetService<IClock>();
                    var settings = provider.GetSettings<ModbusTcpSettings>(Id);
                    return new object[] { new ApiTcpModbusService(
                        new ModbusMapping(moduleDefinition),
                        new ModbusTcpSlaveAdapter(settings),
                        new System.Func<System.TimeSpan>(
                            () => clock.Now()
                            )
                        )
                    };
                },
                createFeature: assets =>
                {
                    var propertyListener = assets.Get<ApiTcpModbusService>();
                    return DefineFeature()
                        .Handles<PropertiesChanged>(propertyListener.HandlePropertiesChanged)
                        .Handles<SystemTicked>(propertyListener.HandleSystemTicked)
                        .Create();
                });
        }
    }
}