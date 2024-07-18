using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class ThermometerAsPropertyDataDrivenImage : ItemBase
{
  public ThermometerAsPropertyDataDrivenImage()
  {
    _temperature = PropertyUrn<Temperature>.Build(Root.Urn.Value, "temperature_between_255_and_325_");
  }

  private readonly PropertyUrn<Temperature> _temperature;

  public override string Title => "DataDriven Image (property)"; 
  public override IEnumerable<Urn> PropertyInputs => new[] { _temperature };
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_temperature] = "295"
  };
  public override Block Display => Image("assets/thermometer.gif").DataDriven(_temperature, 253.15, 5);
}