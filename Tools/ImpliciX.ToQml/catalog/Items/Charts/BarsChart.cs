using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

public class BarsChart : ItemBase
{
  private readonly MeasureNode<Temperature> _temperatureA;
  private readonly PropertyUrn<Temperature> _temperatureB;
  private readonly PropertyUrn<Temperature> _temperatureC;
  private readonly PropertyUrn<Flow> _yMin;
  private readonly PropertyUrn<Flow> _yMax;

  public override string Title => "Bars Chart";
  public override Block Display { get; }

  public override sealed IEnumerable<Urn> PropertyInputs => new Urn[]
  {
    _temperatureB,
    _temperatureC,
    _yMin,
    _yMax
  };

  public override sealed IEnumerable<ModelNode> MeasureInputs => new[]
  {
    _temperatureA
  };

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_temperatureA.Urn] = "298.15",
    [_temperatureB] = "-15",
    [_temperatureC] = "45"
  };

  public BarsChart()
  {
    var root = new RootModelNode(nameof(BarsChart).ToLower());
    _temperatureA = new MeasureNode<Temperature>("temperature1", root);
    _temperatureB = PropertyUrn<Temperature>.Build(root.Urn, "temperature2");
    _temperatureC = PropertyUrn<Temperature>.Build(root.Urn, "temperature3");
    _yMin = PropertyUrn<Flow>.Build(root.Urn, "y_min");
    _yMax = PropertyUrn<Flow>.Build(root.Urn, "y_max");

    const int columnSize = 167;
    Display = Canvas.Layout(
      At.Origin.Put(
        Chart.Bars(
            Of(_temperatureA).Fill(Color.Green),
            Of(_temperatureB).Fill(Color.Aqua),
            Of(_temperatureC).Fill(Color.Orange)
          ).Width(600).Height(400)
          .YMin(_yMin).YMax(_yMax)
      ),
      At.Top(390).Left(69).Put(Row.Layout(
        Label("Temperature A").Width(columnSize),
        Label("Temperature B").Width(columnSize),
        Label("Temperature C").Width(columnSize)
      ))
    );
  }
}