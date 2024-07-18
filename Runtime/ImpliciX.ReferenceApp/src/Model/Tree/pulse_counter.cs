using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree;

public class pulse_counter : HardwareAndSoftwareDeviceNode
{
    
    public MeasureNode<Volume> gas_index { get; }
    public MeasureNode<Energy> electrical_index { get; }
    public pulse_counter(string name, ModelNode parent) : base(name, parent)
    {
        gas_index = new MeasureNode<Volume>(nameof(gas_index), this);
        electrical_index = new MeasureNode<Energy>(nameof(electrical_index), this);
    }
}