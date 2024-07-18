using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Catalog.CustomWidgets;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

public class MultiTimeLinesItem : ItemBase
{
  private readonly MultiChart _chart;

  private readonly PropertyUrn<Flow> _yMinLeft;
  private readonly PropertyUrn<Flow> _yMaxLeft;
  
  private readonly PropertyUrn<Flow> _yMinRight;
  private readonly PropertyUrn<Flow> _yMaxRight;
  private readonly Urn[] _leftLineSeriesUrns;
  private readonly Urn[] _rightLineSeriesUrns;

  public override string Title => "Show Multi Chart 2";
  public override Block Display => _chart;

  public override IEnumerable<Urn> TimeSeriesInputs => _leftLineSeriesUrns.Concat(_rightLineSeriesUrns).ToArray();

  public sealed override IEnumerable<Urn> PropertyInputs => new Urn[]
  {
    _yMinLeft,
    _yMaxLeft,
    _yMinRight,
    _yMaxRight
  };

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_leftLineSeriesUrns[0]] = TimeSeriesSimulator.MakeJson(GenerateLeftLinesRandomData()),
    [_leftLineSeriesUrns[1]] = TimeSeriesSimulator.MakeJson(GenerateLeftLinesRandomData()),
    [_leftLineSeriesUrns[2]] = TimeSeriesSimulator.MakeJson(GenerateLeftLinesRandomData()),
    [_rightLineSeriesUrns[0]] = TimeSeriesSimulator.MakeJson(GenerateRightLinesRandomData()),
    [_rightLineSeriesUrns[1]] = TimeSeriesSimulator.MakeJson(GenerateRightLinesRandomData()),
  };
  
  private static (float, string)[] GenerateLeftLinesRandomData() =>
    TimeLinesItem.GenerateRandomData("2022-10-22T12:00:00Z", "2022-10-22T17:00:00Z");

  private static (float, string)[] GenerateRightLinesRandomData() =>
    TimeLinesItem.GenerateRandomData("2022-10-22T05:00:00Z", "2022-10-22T16:30:00Z", 3000, 8000);


  public MultiTimeLinesItem()
  {
    var root = new RootModelNode(nameof(TimeStackedBarsAndLinesItem).ToLower());
    
    _leftLineSeriesUrns = new[]
    {
      Urn.BuildUrn(root.Urn, "LeftLineSeries_1"),
      Urn.BuildUrn(root.Urn, "LeftLineSeries_2"),
      Urn.BuildUrn(root.Urn, "LeftLineSeries_3"),
    };

    _yMinLeft = PropertyUrn<Flow>.Build(root.Urn, "y_min_left");
    _yMaxLeft = PropertyUrn<Flow>.Build(root.Urn, "y_max_left");

    var leftChart = Chart.TimeLines(
        Of(_leftLineSeriesUrns[0]).Fill(Color.LimeGreen),
        Of(_leftLineSeriesUrns[1]).Fill(Color.Orange),
        Of(_leftLineSeriesUrns[2]).Fill(Color.Gold)
      ).YMin(_yMinLeft).YMax(_yMaxLeft);
    
    _rightLineSeriesUrns = new[]
    {
      Urn.BuildUrn(root.Urn, nameof(TimeLinesItem), "RightLineSeries_1"),
      Urn.BuildUrn(root.Urn, nameof(TimeLinesItem), "RightLineSeries_2")
    };
    
    _yMinRight = PropertyUrn<Flow>.Build(root.Urn, "y_min_right");
    _yMaxRight = PropertyUrn<Flow>.Build(root.Urn, "y_max_right");

    var rightChart = Chart.TimeLines(
        Of(_rightLineSeriesUrns[0]).Fill(Color.Crimson),
        Of(_rightLineSeriesUrns[1]).Fill(Color.Blue)
      ).YMin(_yMinRight).YMax(_yMaxRight);

    _chart = Chart.Multi(
      leftChart,
      rightChart
    ).Width(700).Height(450);
  }
}