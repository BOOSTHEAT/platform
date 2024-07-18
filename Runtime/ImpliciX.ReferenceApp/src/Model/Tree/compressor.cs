using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree
{
    public class compressor : ModelNode
    {
        public MetricUrn compressor_time { get; }
        public MetricUrn compressor_time_chart { get; }

        public compressor(string urnToken, ModelNode parent) : base(urnToken, parent)
        {
            compressor_time = MetricUrn.Build(Urn, nameof(compressor_time));
            compressor_time_chart = MetricUrn.Build(Urn, nameof(compressor_time_chart));
        }
    }
}
