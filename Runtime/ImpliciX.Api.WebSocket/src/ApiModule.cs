using System;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Api.WebSocket
{
  public class ApiModule : ImpliciXModule
  {
    public static ImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
      => new ApiModule(moduleName, rtDef);

    public ApiModule(string id, ApplicationRuntimeDefinition rtDef) : base(id)
    {
      DefineModule(
        initDependencies: configurator => configurator.AddSettings<ApiSettings>("Modules", Id),
        initResources: (provider) =>
        {
          var settings = provider.GetSettings<ApiSettings>(Id);
          var eventBus = provider.GetService<IEventBusWithFirewall>();
          var clock = provider.GetService<IClock>();

          IApiListener apiListener =
            settings.Version switch
            {
              2 => new ApiListener(settings, eventBus, rtDef, clock.Now),
              _ => throw new NotSupportedException()
            };

          return new object[]
          {
            apiListener
          };
        }, createFeature: assets =>
        {
          var apiListener = assets.Get<IApiListener>();
          return DefineFeature()
            .Handles<PropertiesChanged>(apiListener.HandlePropertiesChanged)
            .Handles<TimeSeriesChanged>(apiListener.HandleTimeSeriesChanged)
            .Create();
        });
    }
  }
}