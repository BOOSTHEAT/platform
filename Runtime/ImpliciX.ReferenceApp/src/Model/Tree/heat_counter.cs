using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree;

public class heat_counter : HardwareAndSoftwareDeviceNode
{
    public heat_counter(string name, ModelNode parent) : base(name, parent)
    {
        energy = new MeasureNode<Energy>(nameof(energy), this);
        power = new MeasureNode<Power>(nameof(power), this);
        flow = new MeasureNode<Flow>(nameof(flow), this);
        outlet_temperature = new MeasureNode<Temperature>(nameof(outlet_temperature), this);
        inlet_temperature = new MeasureNode<Temperature>(nameof(inlet_temperature), this);
        differential_temperature = new MeasureNode<DifferentialTemperature>(nameof(differential_temperature), this);
    }

    public MeasureNode<Energy> energy { get; }
    public MeasureNode<Power> power { get; }
    public MeasureNode<Flow> flow { get; }
    public MeasureNode<Temperature> outlet_temperature { get; }
    public MeasureNode<Temperature> inlet_temperature { get; }
    public MeasureNode<DifferentialTemperature> differential_temperature { get; }
}