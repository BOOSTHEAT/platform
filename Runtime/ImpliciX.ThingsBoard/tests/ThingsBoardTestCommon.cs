using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.ThingsBoard.Tests
{
  public class test_model : RootModelNode
  {
    static test_model()
    {
      temperature = PropertyUrn<Temperature>.Build("root", nameof(temperature));
      pressure = PropertyUrn<Pressure>.Build("root", nameof(pressure));
      energy = PropertyUrn<Energy>.Build("root", nameof(energy));
      burner_status = PropertyUrn<GasBurnerStatus>.Build("root", nameof(burner_status));
      metric1_simple = MetricUrn.Build("root", nameof(metric1_simple));
      metric2_simple = MetricUrn.Build("root", nameof(metric2_simple));
      metric3_composite = MetricUrn.Build("root", nameof(metric3_composite));
      metric4_composite = MetricUrn.Build("root", nameof(metric4_composite));
    }

    public static PropertyUrn<Temperature> temperature { get; }
    public static PropertyUrn<Pressure> pressure { get; }
    public static PropertyUrn<Energy> energy { get; }
    public static PropertyUrn<GasBurnerStatus> burner_status { get; }
    public static MetricUrn metric1_simple { get; }
    public static MetricUrn metric2_simple { get; }
    public static MetricUrn metric3_composite { get; }
    public static MetricUrn metric4_composite { get; }

    public test_model(string urnToken) : base(urnToken)
    {
    }
  }

  public static class DomainEventTestExtension
  {
    public static Type[] GetTypes(this DomainEvent[] domainEvents)
    {
      return domainEvents.Select(@event => @event.GetType()).ToArray();
    }
  }
}