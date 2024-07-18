using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree
{
    public class gas : ModelNode
    {
        public MetricUrn gas_index { get; }

        public MetricUrn gas_index_chart { get; }

        public gas(string urnToken, ModelNode parent) : base(urnToken, parent)
        {
            gas_index = MetricUrn.Build(Urn, nameof(gas_index));
            gas_index_chart = MetricUrn.Build(Urn, nameof(gas_index_chart));
        }
    }
}
