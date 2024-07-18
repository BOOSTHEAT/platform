using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class ShowMeasure : ItemBase
{
  public ShowMeasure()
  {
    _temperature = new MeasureNode<Temperature>("temperature", Root);
  }

  private readonly MeasureNode<Temperature> _temperature;

  public override string Title => "Show measure"; 
  public override IEnumerable<ModelNode> MeasureInputs => new[] { _temperature };
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_temperature.Urn] = "295"
  };
  public override Block Display => Show(_temperature);
}