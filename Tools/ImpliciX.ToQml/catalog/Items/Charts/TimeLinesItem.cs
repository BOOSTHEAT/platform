using System.Drawing;
using System.Numerics;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Catalog.CustomWidgets;
using DateTime = System.DateTime;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

internal sealed class TimeLinesItem : ItemBase
{
  private readonly Urn[] _timeSeriesUrns;
  private readonly TimeLines _chart;
  private readonly PropertyUrn<Flow> _yMin;
  private readonly PropertyUrn<Flow> _yMax;
  public override string Title => "Show TimeLines Chart";
  public override Block Display => _chart;

  public override IEnumerable<Urn> TimeSeriesInputs => _timeSeriesUrns;

  public override IEnumerable<Urn> PropertyInputs => new Urn[]
  {
    _yMin,
    _yMax
  };


  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_timeSeriesUrns[0]] = TimeSeriesSimulator.MakeJson(GenerateRandomData()),
    [_timeSeriesUrns[1]] = TimeSeriesSimulator.MakeJson(GenerateRandomData())
  };

  private static readonly Random random = new();

  private static (float, string)[] GenerateRandomData() =>
    GenerateRandomData("2022-10-22T00:00:00Z", "2023-10-22T00:59:59Z");

  public static (float, string)[] GenerateRandomData(string startDate, string endDate, int minValue = 0, int maxValue = 100)
  {
    const int numberOfPoints = 100;

    var start = DateTime.Parse(startDate);
    var end = DateTime.Parse(endDate);
    var timeSpan = end - start;
    var interval = timeSpan.TotalMilliseconds / (numberOfPoints - 1);

    var result = new List<(float, string)>();
    for (var i = 0; i < numberOfPoints; i++)
    {
      var randomValue = (float)(random.NextDouble() * (maxValue - minValue) + minValue);
      var timeSpanToAdd = TimeSpan.FromMilliseconds(interval * i);
      var date = start + timeSpanToAdd;
      var timestamp = date.ToString("o");
      result.Add((randomValue, timestamp));
    }

    return result.ToArray();
  }

  public TimeLinesItem()
  {
    var root = new RootModelNode(nameof(TimeLinesItem).ToLower());
    _timeSeriesUrns = new[]
    {
      Urn.BuildUrn(root.Urn, nameof(TimeLinesItem), "MyTimeSeries_1"),
      Urn.BuildUrn(root.Urn, nameof(TimeLinesItem), "MyTimeSeries_2")
    };

    _yMin = PropertyUrn<Flow>.Build(root.Urn, "y_min");
    _yMax = PropertyUrn<Flow>.Build(root.Urn, "y_max");

    _chart = Chart.TimeLines(
        Of(_timeSeriesUrns[0]).Fill(Color.Crimson),
        Of(_timeSeriesUrns[1]).Fill(Color.Blue)
      ).Width(700).Height(450)
      .YMin(_yMin).YMax(_yMax);
  }
}