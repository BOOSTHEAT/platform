using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.Functions;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples
{
    public class FanController : SubSystemDefinition<FanController.State>
    {
        public enum State
        {
            Unstable,
            Stable,
            Off,
            BeforeOff,
            VeryUnstable
        }

        public FanController()
        {
            Subsystem(domotic.fancontroller)
                .Initial(State.Off)
                .Define(State.Off)
                    .Transitions
                        .WhenMessage(domotic.fancontroller._start).Then(State.Unstable)
                .Define(State.Unstable)
                    .OnEntry
                        .Set(domotic.fancontroller.starting_temperature,Identity.Func, constant.parameters.none, domotic.fancontroller.thermometer.temperature)
                    .OnState
                          .Set(domotic.fancontroller.fan3._throttle, PID.Func, 
                               domotic.fancontroller.compute_throttle_pid, 
                               domotic.fancontroller.thermometer.temperature, 
                               domotic.fancontroller.setpoint_temperature, 
                               domotic.fancontroller.fan3._throttle.measure)
                    .Transitions
                        .WhenMessage(domotic.fancontroller._stabilized).Then(State.Stable)
                        .WhenMessage(domotic.fancontroller._stop).Then(State.Off)
                .Define(State.VeryUnstable).AsInitialSubStateOf(State.Unstable)
                .OnState
                    .Set(domotic.fancontroller.delta,Substract.Func,constant.parameters.none,domotic.fancontroller.setpoint_temperature,domotic.fancontroller.thermometer.temperature)
                .Define(State.Stable)
                    .OnState
                        .Set(domotic.fancontroller.fan3._throttle, Polynomial1.Func, domotic.fancontroller.compute_throttle_polynomial, domotic.fancontroller.thermometer.temperature)
                    .Transitions
                        .WhenMessage(domotic.fancontroller._beforeStop).Then(State.BeforeOff)
                        .WhenMessage(domotic.fancontroller._stop).Then(State.Off)
                .Define(State.BeforeOff)
                    .OnState
                        .Set(domotic.fancontroller.fan3.threshold, Substract.Func, constant.parameters.none, domotic.fancontroller.thermometer.temperature, domotic.fancontroller.setpoint_temperature)
                    .Transitions
                        .WhenMessage(domotic.fancontroller._stop).Then(State.Off);
        }
    }
}