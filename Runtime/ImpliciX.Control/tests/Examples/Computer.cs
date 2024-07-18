using System;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.Functions;
using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    public class Computer : SubSystemDefinition<Computer.State>
    {
        public enum State
        {
            Shutdown,
            Booting,
            Booted,
            KernelLoading,
            MountingFS

        }

        public Computer(TimeSpan? testTime=null, bool ordered = true)
        {
            
            if (ordered)
            {
                BuildSubsystemDefinitionOrdered();
            }
            else
            {
                BuildSubsystemDefinitionUnOrdered();
            }
        }

        public void BuildSubsystemDefinitionOrdered()
        {
            Subsystem(devices.computer)
                .Initial(State.Shutdown)
                .Define(State.Shutdown)
                    .Transitions
                        .WhenMessage(devices.computer._start).Then(State.Booting)
                .Define(State.Booting)
                    .OnEntry
                        .Set(devices.computer.led._switch, Switch.On)
                    .OnState
                        .Set(devices.computer.fan._throttle, devices.computer.fan_speed)
                        .Set(devices.computer._send, Polynomial1.Func, devices.computer.compute_constant, devices.computer.variable)
                    .Transitions
                        .WhenMessage(devices.computer._mounted).Then(State.Booted)
                .Define(State.KernelLoading).AsInitialSubStateOf(State.Booting)
                    .OnState
                        .Set(devices.computer.fan2._throttle, devices.computer.fan_speed)
                    .Transitions
                        .WhenMessage(devices.computer._mount).Then(State.MountingFS)
                .Define(State.MountingFS).AsSubStateOf(State.Booting)
                    .OnEntry
                        .Set(devices.computer._buzz, Switch.On)
                .Define(State.Booted)
                    .OnEntry
                        .Set(devices.computer._buzz, Switch.Off)
                    .OnExit
                        .Set(devices.computer._buzz, Switch.On)
                    .Transitions
                        .WhenMessage(devices.computer._powerOff).Then(State.Shutdown);
        }

        public void BuildSubsystemDefinitionUnOrdered()
        {
            Subsystem(devices.computer)
                .Initial(State.Shutdown)
                .Define(State.MountingFS).AsSubStateOf(State.Booting)
                    .OnEntry
                        .Set(devices.computer._buzz, Switch.On)
                .Define(State.KernelLoading).AsInitialSubStateOf(State.Booting)
                    .OnState
                        .Set(devices.computer.fan2._throttle, devices.computer.fan_speed)
                    .Transitions
                        .WhenMessage(devices.computer._mount).Then(State.MountingFS)
                .Define(State.Booted)
                    .OnEntry
                        .Set(devices.computer._buzz, Switch.Off)
                    .OnExit
                        .Set(devices.computer._buzz, Switch.On)
                    .Transitions
                        .WhenMessage(devices.computer._powerOff).Then(State.Shutdown)
                .Define(State.Booting)
                    .OnEntry
                        .Set(devices.computer.led._switch, Switch.On)
                    .OnState
                        .Set(devices.computer.fan._throttle, devices.computer.fan_speed)
                        .Set(devices.computer._send, Polynomial1.Func, devices.computer.compute_constant, devices.computer.variable)
                    .Transitions
                        .WhenMessage(devices.computer._mounted).Then(State.Booted)
                .Define(State.Shutdown)
                    .Transitions
                        .WhenMessage(devices.computer._start).Then(State.Booting);
        }
    }
}