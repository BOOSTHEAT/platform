using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class SwitchOnMeasure : ItemBase
{
  public SwitchOnMeasure()
  {
    _temperature = new MeasureNode<Temperature>("switch_over_300", Root);
  }

  private readonly MeasureNode<Temperature> _temperature;

  public override string Title => "Switch on measure";
  public override IEnumerable<ModelNode> MeasureInputs => new[] { _temperature };
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_temperature.Urn] = "295"
  };
  public override Block Display => Switch
    .Case(Value(_temperature) > 300, Box.Width(100).Height(100).Fill(Color.Chocolate))
    .Default(Box.Width(100).Height(100).Fill(Color.Blue));
}