using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree
{
    public class electrical : ModelNode
    {
        public MetricUrn electrical_index { get; }
        public MetricUrn heat_service_state { get; }


        public MetricUrn electrical_index_chart { get; }
        public MetricUrn heat_service_state_chart { get; }

        public electrical(string urnToken, ModelNode parent) : base(urnToken, parent)
        {
            electrical_index = MetricUrn.Build(Urn, nameof(electrical_index));
            heat_service_state = MetricUrn.Build(Urn, nameof(heat_service_state));

            electrical_index_chart = MetricUrn.Build(Urn, nameof(electrical_index_chart));
            heat_service_state_chart = MetricUrn.Build(Urn, nameof(heat_service_state_chart));
        }
    }
}