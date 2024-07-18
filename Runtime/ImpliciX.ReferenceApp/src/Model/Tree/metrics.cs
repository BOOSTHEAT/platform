using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree
{
    public class metrics : ModelNode
    {
        public heat heat { get; }
        public gas gas { get; }
        public electrical electrical { get; }
        public compressor compressor { get; }
        public PropertyUrn<Energy> consumption { get; }
        public metrics(string urnToken, ModelNode parent) : base(urnToken, parent)
        {
            heat = new heat(nameof(heat), this);
            gas = new gas(nameof(gas), this);
            compressor = new compressor(nameof(compressor), this);
            electrical = new electrical(nameof(electrical), this);

            consumption = PropertyUrn<Energy>.Build(Urn, nameof(consumption));
        }
    }
}
