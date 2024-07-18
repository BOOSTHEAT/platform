using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog;

public class StartupScreen : ItemBase
{
  public override string Title => "Select widget...";
  public override IEnumerable<ModelNode> MeasureInputs => Enumerable.Empty<ModelNode>();
  public override IEnumerable<Urn> PropertyInputs => Enumerable.Empty<Urn>();
  public override IEnumerable<Urn> TimeSeriesInputs => Enumerable.Empty<Urn>();
  public override Block Display => Label("Use the combobox on the right\nto select a widget demo").With(Font.ExtraBold.Size(24));
}