using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class ThermometerAsMeasureDataDrivenImage : ItemBase
{
  public ThermometerAsMeasureDataDrivenImage()
  {
    _temperature = new MeasureNode<Temperature>("temperature_between_255_and_325", Root);
  }

  private readonly MeasureNode<Temperature> _temperature;

  public override string Title => "DataDriven Image (measure)"; 
  public override IEnumerable<ModelNode> MeasureInputs => new[] { _temperature };
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_temperature.Urn] = "295"
  };
  public override Block Display => Image("assets/thermometer.gif").DataDriven(_temperature, 253.15, 5);
}