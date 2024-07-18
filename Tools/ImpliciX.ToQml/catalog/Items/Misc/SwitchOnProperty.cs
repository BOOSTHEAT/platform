using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class SwitchOnProperty : ItemBase
{
  public SwitchOnProperty()
  {
    _pressure = PropertyUrn<Pressure>.Build(Root.Urn.Value, "switch_over_10");
  }

  private readonly PropertyUrn<Pressure> _pressure;

  public override string Title => "Switch on property";
  public override IEnumerable<Urn> PropertyInputs => new[] { _pressure };
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_pressure] = "8"
  };
  public override Block Display => Switch
    .Case(Value(_pressure) > 10, Box.Width(100).Height(100).Fill(Color.Chocolate))
    .Default(Box.Width(100).Height(100).Fill(Color.Blue));
}