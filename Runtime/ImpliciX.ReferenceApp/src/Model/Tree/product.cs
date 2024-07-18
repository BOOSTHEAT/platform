using ImpliciX.Language.Model;
using ImpliciX.ReferenceApp.Model.Enums;

namespace ImpliciX.ReferenceApp.Model.Tree;

public class product : HardwareAndSoftwareDeviceNode
{
    public product(string name, ModelNode parent) : base(name, parent)
    {
        room_temperature = new MeasureNode<Temperature>(nameof(room_temperature), this);
        outdoor_temperature = new MeasureNode<Temperature>(nameof(outdoor_temperature), this);
        compressor_running_time = new MeasureNode<Duration>(nameof(compressor_running_time), this);
        dhw_state = new MeasureNode<States>(nameof(dhw_state), this);
        heating_service_state = new MeasureNode<States>(nameof(heating_service_state), this);
        heat_pump_state = new MeasureNode<States>(nameof(heat_pump_state), this);
        condensing_boiler_state = new MeasureNode<States>(nameof(condensing_boiler_state), this);
    }

    public MeasureNode<Temperature> room_temperature { get; }
    public MeasureNode<Temperature> outdoor_temperature { get; }
    public MeasureNode<Duration> compressor_running_time { get; }
    public MeasureNode<States> dhw_state { get; }
    public MeasureNode<States> heating_service_state { get; }
    public MeasureNode<States> heat_pump_state { get; }
    public MeasureNode<States> condensing_boiler_state { get; }
}