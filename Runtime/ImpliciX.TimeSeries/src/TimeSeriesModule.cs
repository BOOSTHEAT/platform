using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using InfluxDB.Client.Writes;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.TimeSeries
{
    public class TimeSeriesModule : ImpliciXModule
    {
        public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
            => new TimeSeriesModule(moduleName);

        public TimeSeriesModule(string id) : base(id)
        {
            DefineModule(
                initDependencies: cfg => cfg.AddSettings<TimeSeriesSettings>("Modules", Id),
                initResources: provider =>
                {
                    var settings = provider.GetSettings<TimeSeriesSettings>(Id);
                    var clock = provider.GetService<IClock>();
                    var influxDb = new InfluxDbAdapterDirectHttp(settings.Storage.URL,
                        settings.Storage.Bucket,
                        settings.Storage.RetentionPolicy,
                        settings.Storage.HttpBatchSizeLimit);

                    var influxDbWithDP = new DisasterPrevention(settings.Storage.MaxErrorsBeforeDeactivation, influxDb);
                    return new object[] { influxDbWithDP, clock, settings };
                },
                createFeature: assets =>
                {
                    var settings = assets.Get<TimeSeriesSettings>();
                    return DefineFeature()
                        .Handles<PropertiesChanged>(PropertiesChangedEventHandler(
                            assets.Get<IInfluxDbAdapter>(), settings.MetricsOnly))
                        .Create();
                });
        }

        private DomainEventHandler<PropertiesChanged> PropertiesChangedEventHandler(IInfluxDbAdapter influxDb, bool metricsOnly)
        {
            return propertiesChanged =>
            {
                var measures = ModelValuesConvertor.ToDataPoints(propertiesChanged.ModelValues, metricsOnly);
                if (!measures.Any()) return Array.Empty<DomainEvent>();
                PushMeasures(influxDb, measures);
                return new DomainEvent[] { };
            };
        }

        private bool PushMeasures(IInfluxDbAdapter influxDb, IEnumerable<PointData> points) =>
            influxDb.WritePoints(points);
    }
}