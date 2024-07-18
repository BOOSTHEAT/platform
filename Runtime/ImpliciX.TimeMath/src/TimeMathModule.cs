using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.Runtime;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.TimeMath.Access;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.TimeMath;

public class TimeMathModule : ImpliciXModule
{
  public TimeMathModule(
    string id,
    MetricInfoSet metricInfoSet,
    ApplicationOptions options
  ) : base(id)
  {
    DefineModule(
      dependency => dependency.AddSettings<TimeMathSettings>("Modules", Id),
      provider =>
      {
        var clock = provider.GetService<IClock>();
        var internalBus = provider.GetService<IEventBusWithFirewall>();

        var timeSeriesDb = new TimeSeriesDb(
          Path.Combine(options.LocalStoragePath, "internal", "time-math"),
          "computers",
          options.StartMode == StartMode.Safe);

        ITimeMathWriter timeMathWriter = new TimeBasedTimeMathWriter(timeSeriesDb);
        ITimeMathReader timeMathReader = new TimeBasedTimeMathReader(timeSeriesDb);
        var timeMathService = new TimeMathService(
          clock.Now
        );

        return new object[]
        {
          internalBus,
          timeMathWriter,
          timeMathReader,
          timeMathService
        };
      },
      assets =>
      {
        var internalBus = assets.Get<IEventBusWithFirewall>();
        var timeMathService = assets.Get<TimeMathService>();
        var timeMathWriter = assets.Get<ITimeMathWriter>();
        var timeMathReader = assets.Get<ITimeMathReader>();

        var features = DefineFeature()
          .Handles<PropertiesChanged>(timeMathService.HandlePropertiesChanged)
          .Handles<SystemTicked>(timeMathService.HandleSystemTicked)
          .Create();

        internalBus.Publish(
          timeMathService.Initialize(
            metricInfoSet,
            timeMathWriter,
            timeMathReader
          )
        );

        return features;
      }
    );
  }

  public static IImpliciXModule Create(
    string moduleName,
    ApplicationRuntimeDefinition applicationRuntimeDefinition
  )
  {
    var applicationDefinition = applicationRuntimeDefinition ??
                                throw new ArgumentNullException(nameof(applicationRuntimeDefinition));

    var timeMathModuleDefinition = applicationRuntimeDefinition.Module<TimeMathModuleDefinition>() ??
                                   throw new ArgumentNullException(nameof(TimeMathModuleDefinition));

    var metricDefinitions = timeMathModuleDefinition.Metrics ??
                            throw new ArgumentNullException(nameof(IMetricDefinition));

    var metrics = metricDefinitions.Select(
      def => def.Builder.Build<Metric<MetricUrn>>()
    ).ToArray();

    var supportedMetrics = metrics.Where(m => m.Kind != MetricKind.Communication);
    var metricInfoSet = CreateMetricInfos.Execute(supportedMetrics, applicationRuntimeDefinition.StateMachines);

    return new TimeMathModule(
      moduleName,
      metricInfoSet,
      applicationDefinition.Options);
  }
}