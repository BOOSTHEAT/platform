using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Catalog.CustomWidgets;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

public class TimeStackedBarsAndLinesItem : ItemBase
{
  private readonly MultiChart _chart;

  private readonly PropertyUrn<Flow> _yMinLeft;
  private readonly PropertyUrn<Flow> _yMaxLeft;
  
  private readonly PropertyUrn<Flow> _yMinRight;
  private readonly PropertyUrn<Flow> _yMaxRight;
  private readonly Urn[] _barSeriesUrns;
  private readonly Urn[] _lineSeriesUrns;

  public override string Title => "Show Multi Chart 1";
  public override Block Display => _chart;

  public override IEnumerable<Urn> TimeSeriesInputs => _barSeriesUrns.Concat(_lineSeriesUrns).ToArray();

  public sealed override IEnumerable<Urn> PropertyInputs => new Urn[]
  {
    _yMinLeft,
    _yMaxLeft,
    _yMinRight,
    _yMaxRight
  };

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_barSeriesUrns[0]] = TimeSeriesSimulator.MakeJson(GenerateBarRandomData()),
    [_barSeriesUrns[1]] = TimeSeriesSimulator.MakeJson(GenerateBarRandomData()),
    [_barSeriesUrns[2]] = TimeSeriesSimulator.MakeJson(GenerateBarRandomData()),
    [_lineSeriesUrns[0]] = TimeSeriesSimulator.MakeJson(GenerateLinesRandomData()),
    [_lineSeriesUrns[1]] = TimeSeriesSimulator.MakeJson(GenerateLinesRandomData())

  };
  
  private static (float, string)[] GenerateBarRandomData() =>
    StackedTimeBarsItem.GenerateRandomData("2022-10-22T14:00:00Z", TimeSpan.FromHours(1.5), 5);

  private static (float, string)[] GenerateLinesRandomData() =>
    TimeLinesItem.GenerateRandomData("2022-10-22T05:00:00Z", "2022-10-22T20:00:00Z");


  public TimeStackedBarsAndLinesItem()
  {
    var root = new RootModelNode(nameof(TimeStackedBarsAndLinesItem).ToLower());
    
    _barSeriesUrns = new[]
    {
      Urn.BuildUrn(root.Urn, "BarSeries_1"),
      Urn.BuildUrn(root.Urn, "BarSeries_2"),
      Urn.BuildUrn(root.Urn, "BarSeries_3"),
    };

    _yMinLeft = PropertyUrn<Flow>.Build(root.Urn, "y_min_left");
    _yMaxLeft = PropertyUrn<Flow>.Build(root.Urn, "y_max_left");

    var leftChart = Chart.StackedTimeBars(
        Of(_barSeriesUrns[0]).Fill(Color.LimeGreen),
        Of(_barSeriesUrns[1]).Fill(Color.Orange),
        Of(_barSeriesUrns[2]).Fill(Color.Gold)
      ).YMin(_yMinLeft).YMax(_yMaxLeft);
    
    _lineSeriesUrns = new[]
    {
      Urn.BuildUrn(root.Urn, nameof(TimeLinesItem), "LineSeries_1"),
      Urn.BuildUrn(root.Urn, nameof(TimeLinesItem), "LineSeries_2")
    };
    
    _yMinRight = PropertyUrn<Flow>.Build(root.Urn, "y_min_right");
    _yMaxRight = PropertyUrn<Flow>.Build(root.Urn, "y_max_right");

    var rightChart = Chart.TimeLines(
        Of(_lineSeriesUrns[0]).Fill(Color.Crimson),
        Of(_lineSeriesUrns[1]).Fill(Color.Blue)
      ).YMin(_yMinRight).YMax(_yMaxRight);

    _chart = Chart.Multi(
      leftChart,
      rightChart
    ).Width(700).Height(450);
  }
}