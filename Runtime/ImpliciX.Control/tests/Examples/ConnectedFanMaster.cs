using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class ConnectedFanMaster : SubSystemDefinition<ConnectedFanMaster.State>
    {
        public enum State
        {
            On,
            Off,
        }

        public ConnectedFanMaster()
        {
            Subsystem(domotic.connected_fan_master)
                .Initial(State.Off)
                .Define(State.Off)
                    .Transitions
                        .WhenMessage(domotic.connected_fan_master._start).Then(State.On)
                .Define(State.On)
                    .Transitions
                        .WhenMessage(domotic.connected_fan_master._stop).Then(State.Off);
        }
    }


}