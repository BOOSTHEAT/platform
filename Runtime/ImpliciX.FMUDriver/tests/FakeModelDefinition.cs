using ImpliciX.Language.Model;

namespace ImpliciX.FmuDriver.Tests
{
  public class fake : RootModelNode
  {
    public static CommandUrn<NoArg> simulation_start { get; }
    public static CommandUrn<NoArg> simulation_stop { get; }
    public static CommandNode<PowerSupply> _power { get; }
    public static CommandNode<Percentage> _throttle { get; }
    public static CommandUrn<NoArg> _something { get; }
    public static MeasureNode<Temperature> service_dhw_bottom_temperature { get; }
    public static MeasureNode<RotationalSpeed> production_auxiliary_burner_fan_speed { get; }


    public fake() : base(nameof(fake))
    {
    }

    static fake()
    {
      var root = new fake();
      simulation_start = CommandUrn<NoArg>.Build(root.Urn, "START");
      simulation_stop = CommandUrn<NoArg>.Build(root.Urn, "STOP");
      _power = CommandNode<PowerSupply>.Create("POWER", root);
      _throttle = CommandNode<Percentage>.Create("THROTTLE", root);
      _something = CommandUrn<NoArg>.Build(root.Urn, "SOMETHING");
      service_dhw_bottom_temperature =
        new MeasureNode<Temperature>(Urn.BuildUrn(nameof(service_dhw_bottom_temperature)), root);
      production_auxiliary_burner_fan_speed =
        new MeasureNode<RotationalSpeed>(Urn.BuildUrn(nameof(production_auxiliary_burner_fan_speed)), root);
    }
  }
}