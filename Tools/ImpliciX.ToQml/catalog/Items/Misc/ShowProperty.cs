using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class ShowProperty : ItemBase
{
  public ShowProperty()
  {
    _pressure = PropertyUrn<Pressure>.Build(Root.Urn.Value, "pressure");
  }

  private readonly PropertyUrn<Pressure> _pressure;

  public override string Title => "Show property";
  public override IEnumerable<Urn> PropertyInputs => new[] { _pressure };
  public override Block Display => Show(_pressure);
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_pressure] = "1250"
  };
}