using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree
{
    public class heat : ModelNode
    {
        public MetricUrn heating_power { get; }
        public MetricUrn dhw_power { get; }
        public PropertyUrn<Power> power { get; }

        public MetricUrn heating_energy { get; }
        public MetricUrn dhw_energy { get; }
        public PropertyUrn<Energy> energy { get; }

        public MetricUrn heating_power_chart { get; }
        public MetricUrn dhw_power_chart { get; }
        public PropertyUrn<Power> power_chart { get; }

        public MetricUrn heating_energy_chart { get; }
        public MetricUrn dhw_energy_chart { get; }
        public PropertyUrn<Energy> energy_chart { get; }

        public heat(string urnToken, ModelNode parent) : base(urnToken, parent)
        {
            heating_power = MetricUrn.Build(Urn, nameof(heating_power));
            dhw_power = MetricUrn.Build(Urn, nameof(dhw_power));
            power = PropertyUrn<Power>.Build(Urn, nameof(power));

            heating_energy = MetricUrn.Build(Urn, nameof(heating_energy));
            dhw_energy = MetricUrn.Build(Urn, nameof(dhw_energy));
            energy = PropertyUrn<Energy>.Build(Urn, nameof(energy));

            heating_power_chart = MetricUrn.Build(Urn, nameof(heating_power_chart));
            dhw_power_chart = MetricUrn.Build(Urn, nameof(dhw_power_chart));
            power_chart = PropertyUrn<Power>.Build(Urn, nameof(power_chart));

            heating_energy_chart = MetricUrn.Build(Urn, nameof(heating_energy_chart));
            dhw_energy_chart = MetricUrn.Build(Urn, nameof(dhw_energy_chart));
            energy_chart = PropertyUrn<Energy>.Build(Urn, nameof(energy_chart));
        }
    }
}
