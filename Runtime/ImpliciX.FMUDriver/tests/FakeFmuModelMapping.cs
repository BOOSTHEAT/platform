using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;

namespace ImpliciX.FmuDriver.Tests
{
  public class FakeDriverFmuModuleDefinition : DriverFmuModuleDefinition
  {
    public FakeDriverFmuModuleDefinition()
    {
      StartSimulation = fake.simulation_start;
      StopSimulation = fake.simulation_stop;
      FmuPackage = "Fake.fmu";

      ReadVariables = new (Urn, Urn, string)[]
      {
        (fake.service_dhw_bottom_temperature.measure, fake.service_dhw_bottom_temperature.status, "T_bot_Tank"),
        (fake.production_auxiliary_burner_fan_speed.measure, string.Empty, "V_vent")
      };

      WriteVariables = new (Urn, string)[]
      {
        (fake._power, "OnOff_Pompe_ECS"),
        (fake._throttle, "Signal_Aux")
      };
    }
  }
}