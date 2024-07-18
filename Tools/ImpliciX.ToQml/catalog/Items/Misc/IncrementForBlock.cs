using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

public class IncrementForBlock : ItemBase
{
  private readonly PropertyUrn<Pressure> _pressure;
  public override string Title => "Increment for block";
  public override IEnumerable<Urn> PropertyInputs => new Urn[] {_pressure};
  public override Block Display => Show(_pressure).Increment(_pressure, 1.0);
  public override string UserInfoMessage => "Click on text label to increment";

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_pressure] = "150"
  };

  public IncrementForBlock()
  {
    var rootUrn = nameof(IncrementForBlock).ToLower();
    _pressure = PropertyUrn<Pressure>.Build(rootUrn, "pressure");
  }
}