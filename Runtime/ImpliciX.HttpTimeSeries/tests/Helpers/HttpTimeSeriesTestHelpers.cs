using System.Text;
using ImpliciX.HttpTimeSeries.HttpApi;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Collections;
using Moq;
using PHD = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.HttpTimeSeries.Tests.Helpers;

internal static class HttpTimeSeriesTestHelpers
{
  public static void PopulateDb(DataService sut, Urn rootUrn, Dictionary<string, TimeSeriesValue[]> valuesByUrn)
  {
    foreach (var it in valuesByUrn)
    {
      it.Value.ForEach(o =>
      {
        var tsValue = PHD.CreateMetricValueProperty(it.Key, o.Value, o.At, o.At);
        var pc = PropertiesChanged.Create(rootUrn, new[] {tsValue}, o.At);
        sut.StoreSeries(pc);
      });
    }
  }

  internal static string Read(this IEndPointResult result)
  {
    using var ms = new MemoryStream();
    result.WriteTo(ms);
    return Encoding.UTF8.GetString(ms.ToArray());
  }

  public static MetricUrn MUrn(string urn) => MetricUrn.Build(urn);
  public static PropertyUrn<fake_model.PublicState> StateUrn(string urn) => PropertyUrn<fake_model.PublicState>.Build(urn);
  public static Metric<MetricUrn> BuildMetric(FluentStep fluentStep) => fluentStep.Builder.Build<Metric<MetricUrn>>();
  
  public static IDefinedSeries CreateFakeSeries(params (string Value,int Retention)[] def)
  {
    var urns = def
      .ToDictionary(a => Urn.BuildUrn(a.Value), a => TimeSpan.FromMinutes(a.Retention));
    var series = new Mock<IDefinedSeries>(MockBehavior.Strict);
    series.Setup(s => s.RootUrns).Returns(urns.Keys.ToArray());
    series.Setup(s => s.OutputUrns).Returns(urns.Keys.ToArray());
    series.Setup(s => s.RootUrnOf(It.IsAny<Urn>())).Returns<Urn>(u => u);
    series
      .Setup(s => s.StorablePropertiesForRoot(It.IsAny<Urn>()))
      .Returns<Urn>(rootUrn => (new HashSet<Urn>(new [] {rootUrn}), urns[rootUrn]));
    return series.Object;
  }

}