using System;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class SystemViewDesignModel : SystemViewModel
{
  private static readonly NamedTree FirstEntry = new (new NamedModel("first"));
  private static readonly string ContextTitle = "DataContext";

  private static readonly ILightConcierge
    Concierge = IConcierge.Create(new User(ContextTitle)); // new LightConcierge();

  private static readonly NamedModel Root = new ("root");

  private static readonly NamedTree FirstBranch = new (
    Root,
    FirstEntry
  );

  private static readonly string[] Urns = { "device", "heater" };

  private static readonly IMetric Metric = new Metric<Temperature>(
    MetricKind.Gauge,
    new Temperature(),
    new MetricUrn("temperature"),
    PropertyUrn<float>.Build(Urns),
    new TimeSpan(TimeSpan.TicksPerSecond)
  );

  private static readonly NamedTreeLeaf FirstLeaf = new (new MetricViewModel(Metric));

  private static readonly NamedTree[] DefaultTree =
  {
    FirstBranch,
    FirstLeaf
  };

  public SystemViewDesignModel(
  ) : base(
    Concierge,
    DefaultTree,
    SubsystemViewModelFactory
  )
  {
  }

  private static SubSystemViewModel SubsystemViewModelFactory(
    ISubSystemDefinition arg
  )
  {
    throw new NotImplementedException();
  }
}
