using System;
using ImpliciX.Language.Metrics;
using ImpliciX.ReferenceApp.Model;
using static ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.ReferenceApp.App
{
    internal static class AllMetrics
    {
        public static readonly TimeSpan SnapshotInterval = TimeSpan.FromSeconds(5);

        public static readonly IMetricDefinition[] Declarations =
        {
            Metric(system.metrics.heat.dhw_power).Is.Every(5).Seconds.GaugeOf(monitoring.dhw.power.measure),
            Metric(system.metrics.heat.heating_power).Is.Every(5).Seconds.GaugeOf(monitoring.heating.power.measure),

            Metric(system.metrics.heat.dhw_energy).Is.Every(5).Seconds.GaugeOf(monitoring.dhw.energy.measure)
                .Group.Minutely.Over.ThePast(1).Hours,

            Metric(system.metrics.heat.heating_energy).Is.Every(5).Seconds.GaugeOf(monitoring.heating.energy.measure)
                .Group.Minutely.Over.ThePast(1).Hours,

            Metric(system.metrics.gas.gas_index).Is.Every(5).Seconds.GaugeOf(monitoring.pulse_counter.gas_index.measure)
                .Group.Minutely.Over.ThePast(1).Hours,

            Metric(system.metrics.electrical.electrical_index).Is.Every(5).Seconds.GaugeOf(monitoring.pulse_counter.electrical_index.measure)
                .Group.Minutely.Over.ThePast(1).Hours,

            Metric(system.metrics.compressor.compressor_time).Is.Every(5).Seconds.GaugeOf(monitoring.product.compressor_running_time.measure)
                .Group.Every(5).Seconds.Over.ThePast(10).Minutes,

            Metric(system.metrics.electrical.heat_service_state).Is.Every(5).Seconds.StateMonitoringOf(monitoring.product.heating_service_state.measure)
                .Including("electrical_index").As.VariationOf(monitoring.pulse_counter.electrical_index.measure)
                .Group.Every(10).Minutes.Over.ThePast(1).Hours,

            // Metrics d'affichage

            Metric(system.metrics.heat.dhw_power_chart).Is.Every(5).Seconds.GaugeOf(monitoring.dhw.power.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.heat.heating_power_chart).Is.Every(5).Seconds.GaugeOf(monitoring.heating.power.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.heat.dhw_energy_chart).Is.Every(5).Seconds.GaugeOf(monitoring.dhw.energy.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.heat.heating_energy_chart).Is.Every(5).Seconds.GaugeOf(monitoring.heating.energy.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.gas.gas_index_chart).Is.Every(5).Seconds.GaugeOf(monitoring.pulse_counter.gas_index.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.electrical.electrical_index_chart).Is.Every(5).Seconds.GaugeOf(monitoring.pulse_counter.electrical_index.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.compressor.compressor_time_chart).Is.Every(5).Seconds.GaugeOf(monitoring.product.compressor_running_time.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            Metric(system.metrics.electrical.heat_service_state_chart).Is.Every(5).Seconds.StateMonitoringOf(monitoring.product.heating_service_state.measure)
                .Including("electrical_index").As.VariationOf(monitoring.pulse_counter.electrical_index.measure)
                .Group.Minutely.Over.ThePast(10).Minutes,

            //system.metrics.electrical.heat_service_state.Failure.electrical_index

            /*
            Metric(system.metrics.electrical.electrical_index).Is.Minutely.StateMonitoringOf(monitoring.product.heating_service_state.measure)
                .Including("electrical_index").As.VariationOf(monitoring.pulse_counter.electrical_index.measure)
                .Group.Hourly,
            */
        };
    }
}

//system.metrics.heat.heating_power IS MINUTELY GAUGE OF system.monitoring.heating.power.measurement (addr 54 heating pollustat)