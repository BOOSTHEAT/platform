using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Catalog.CustomWidgets;
using DateTime = System.DateTime;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

public class StackedTimeBarsItem : ItemBase
{
  private readonly Urn[] _timeSeriesUrns;
  private readonly TimeBars _chart;
  private readonly PropertyUrn<Flow> _yMin;
  private readonly PropertyUrn<Flow> _yMax;

  public override string Title => "Show StackedTimeBars Chart";
  public override Block Display => _chart;

  public override IEnumerable<Urn> TimeSeriesInputs => _timeSeriesUrns;

  public override sealed IEnumerable<Urn> PropertyInputs => new Urn[]
  {
    _yMin,
    _yMax
  };

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_timeSeriesUrns[0]] = TimeSeriesSimulator.MakeJson(GenerateRandomData()),
    [_timeSeriesUrns[1]] = TimeSeriesSimulator.MakeJson(GenerateRandomData()),
    [_timeSeriesUrns[2]] = TimeSeriesSimulator.MakeJson(GenerateRandomData())
  };
  
  private static (float, string)[] GenerateRandomData() =>
    GenerateRandomData("0001-01-01T16:00:00Z", TimeSpan.FromHours(1), 3);
  
  public static (float, string)[] GenerateRandomData(string startDate, TimeSpan period, int numberOfValues)
  {
    const int minValue = 3;
    const int maxValue = 20;
    const int numberOfPoints = 100;

    var start = DateTime.Parse(startDate);
    var interval = period.TotalMilliseconds;

    var result = new List<(float, string)>();
    for (var i = 0; i < numberOfValues; i++)
    {
      var randomValue = (float)(random.NextDouble() * (maxValue - minValue) + minValue);
      var timeSpanToAdd = TimeSpan.FromMilliseconds(interval * i);
      var date = start + timeSpanToAdd;
      var timestamp = date.ToString("o");
      result.Add((randomValue, timestamp));
    }

    return result.ToArray();
  }
  
  private static readonly Random random = new();


  public StackedTimeBarsItem()
  {
    var root = new RootModelNode(nameof(StackedTimeBarsItem).ToLower());
    var timeSeriesUrns = new[]
    {
      Urn.BuildUrn(root.Urn, "MyTimeSeries_1"),
      Urn.BuildUrn(root.Urn, "MyTimeSeries_2"),
      Urn.BuildUrn(root.Urn, "MyTimeSeries_3"),
    };

    _yMin = PropertyUrn<Flow>.Build(root.Urn, "y_min");
    _yMax = PropertyUrn<Flow>.Build(root.Urn, "y_max");

    _chart = Chart.StackedTimeBars(
        Of(timeSeriesUrns[0]).Fill(Color.Crimson),
        Of(timeSeriesUrns[1]).Fill(Color.Blue),
        Of(timeSeriesUrns[2]).Fill(Color.Gold)
      ).Width(600).Height(400)
      .YMin(_yMin).YMax(_yMax);

    _timeSeriesUrns = timeSeriesUrns;
  }
}