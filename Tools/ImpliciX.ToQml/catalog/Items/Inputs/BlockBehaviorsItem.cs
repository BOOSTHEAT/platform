using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items.Inputs;

internal sealed class BlockBehaviorsItem : ItemBase
{
  public override string Title => "Multiple behaviors on Blocks";
  public override IEnumerable<Urn> PropertyInputs => new Urn[] {ScreenBlockBehaviors.pressure, ScreenBlockBehaviors._pressure2};
  public override Block Display => Canvas.Layout(ScreenBlockBehaviors.Blocks);

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [ScreenBlockBehaviors.pressure] = "100",
    [ScreenBlockBehaviors._pressure2] = "200"
  };
}