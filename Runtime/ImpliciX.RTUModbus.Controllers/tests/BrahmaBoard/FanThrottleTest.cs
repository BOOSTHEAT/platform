using System;
using ImpliciX.Language.Model;
using ImpliciX.RTUModbus.Controllers.BrahmaBoard;
using ImpliciX.RTUModbus.Controllers.Tests.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Tools;
using NFluent;
using NUnit.Framework;
using static ImpliciX.RTUModbus.Controllers.BrahmaBoard.State;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.ControllerBuilder<
  ImpliciX.RTUModbus.Controllers.BrahmaBoard.Controller, ImpliciX.RTUModbus.Controllers.BrahmaBoard.State>;
using static ImpliciX.RTUModbus.Controllers.Tests.Doubles.TestEnv;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.RTUModbus.Controllers.Tests.BrahmaBoard
{
  public class FanThrottleTest
  {
    private static readonly TestBurner Burner = test_model.burner;
    private static readonly HardwareAndSoftwareDeviceNode DeviceNode = test_model.software.fake_daughter_board;

    [TestCase(Resetting)]
    [TestCase(CheckReadiness)]
    [TestCase(WaitingIgnition)]
    [TestCase(Igniting)]
    [TestCase(Ignited)]
    [TestCase(Faulted)]
    public void should_handle_fan_throttle_commands_when_supplied(State currentState)
    {
      var slaveController =
        DefineControllerInState(currentState)
          .ForSimulatedSlave(DeviceNode, Burner)
          .ExecuteCommandSimulation(Burner.fan._throttle).ReturningSuccessResult()
          .BuildSlaveController();
      var trigger = EventCommandRequested(Burner.fan._throttle, Percentage.FromFloat(0.2f).Value, TimeSpan.Zero);

      var resultingEvents = slaveController.HandleDomainEvent(trigger);
      var expectedEvents = new DomainEvent[]
      {
        SlaveCommunicationOccured.CreateHealthy(DeviceNode, TimeSpan.Zero, Healthy_CommunicationDetails),
        EventPropertyChanged(
          TimeSpan.Zero,
          slaveController.Group,
          (Burner.fan._throttle.measure, Percentage.FromFloat(0.2f).Value),
          (Burner.fan._throttle.status, MeasureStatus.Success)
        ),
      };

      Check.That(resultingEvents).ContainsExactly(expectedEvents);
    }
  }
}