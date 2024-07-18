using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;
using static ImpliciX.Language.Control.Condition;
    
namespace ImpliciX.Control.Tests.Examples
{
    public class ConnectedFanSlave : SubSystemDefinition<ConnectedFanSlave.State>
    {
        public enum State
        {
            SlaveOn,
            SlaveOff,
        }


        public ConnectedFanSlave()
        {
            Subsystem(domotic.connected_fan_slave)
                .Initial(State.SlaveOff)
                .Define(State.SlaveOff)
                    .Transitions
                        .WhenMessage(domotic.connected_fan_slave._start).Then(State.SlaveOn)
                        .When(InState(domotic.connected_fan_master,ConnectedFanMaster.State.On)).Then(State.SlaveOn)
                .Define(State.SlaveOn)
                    .Transitions
                        .WhenMessage(domotic.connected_fan_slave._stop).Then(State.SlaveOff)
                        .When(InState(domotic.connected_fan_master, ConnectedFanMaster.State.Off)).Then(State.SlaveOff);

        }
    }
}