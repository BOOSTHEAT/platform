using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

// ReSharper disable once CheckNamespace
namespace ImpliciX.ToQml.Catalog.Items;

public class PieChartItem : ItemBase
{
  private readonly PropertyUrn<float>[] _slicePropertyUrns;
  private readonly PieChart _pieChart;
  public override string Title => "Pie Chart";
  public override Block Display => _pieChart;
  public override IEnumerable<Urn> PropertyInputs => _slicePropertyUrns;
  public override string UserInfoMessage => "Write value in some text box and press Enter key affect pie slice";

  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_slicePropertyUrns[0]] = "50",
    [_slicePropertyUrns[1]] = "28",
    [_slicePropertyUrns[2]] = "68",
    [_slicePropertyUrns[3]] = "22",
    [_slicePropertyUrns[4]] = "32"
  };

  public PieChartItem()
  {
    var root = new RootModelNode(nameof(PieChartItem).ToLower());
    var sliceMetricUrns = new[]
    {
      MetricUrn.Build(root.Urn, "MyMetric_1"),
      MetricUrn.Build(root.Urn, "MyMetric_2"),
      MetricUrn.Build(root.Urn, "MyMetric_3"),
      MetricUrn.Build(root.Urn, "MyMetric_4"),
      MetricUrn.Build(root.Urn, "MyMetric_5")
    };

    _pieChart = Chart.Pie(
      Of(sliceMetricUrns[0]).Fill(Color.Green).With(Font.ExtraBold.Size(22).Color(Color.Chocolate)),
      Of(sliceMetricUrns[1]).Fill(Color.Yellow),
      Of(sliceMetricUrns[2]).Fill(Color.Orange),
      Of(sliceMetricUrns[3]).Fill(Color.Aqua),
      Of(sliceMetricUrns[4]).Fill(Color.Red)
    ).Width(600).Height(400);

    _slicePropertyUrns = sliceMetricUrns
      .Select(urn => PropertyUrn<float>.Build(urn.Value))
      .ToArray();
  }
}