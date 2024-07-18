using System;
using System.Linq;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using Moq;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Data.Tests.Metrics.MetricsHelpers;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;

namespace ImpliciX.Data.Tests.Metrics;

public class GetOutputUrnsTests
{
  [Test]
  public void GivenAListOfGauge_WhenIExecute()
  {
    var metrics = new[]
    {
      CreateMetricGauge("foo:g1"),
      CreateMetricGauge("foo:g2"),
      CreateMetricGauge("foo:g3")
    };

    var expected = new Urn[]
    {
      "foo:g1",
      "foo:g2",
      "foo:g3"
    };

    var metricInfoSet = CreateMetricInfos.Execute(metrics,_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenGauge_WithGroups_WhenIExecute()
  {
    var inputUrn = Urn.BuildUrn("foo:inputUrn");
    var outputUrn = MetricUrn.Build("foo:outputUrn");

    var metric = MFactory
      .CreateGaugeMetric(outputUrn, inputUrn, 5)
      .Group.Every(45).Seconds
      .Group.Hourly
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "foo:outputUrn",
      "foo:outputUrn:_45Seconds",
      "foo:outputUrn:_1Hours"
    };

    var metricInfoSet = CreateMetricInfos.Execute(new[] {metric},_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenAListOfAccumulator_WhenIExecute()
  {
    var metrics = new[]
    {
      CreateMetricAccumulator("foo:a1"),
      CreateMetricAccumulator("foo:a2"),
      CreateMetricAccumulator("foo:a3")
    };

    var expected = new Urn[]
    {
      MetricUrn.BuildAccumulatedValue("foo:a1"),
      MetricUrn.BuildSamplesCount("foo:a1"),
      MetricUrn.BuildAccumulatedValue("foo:a2"),
      MetricUrn.BuildSamplesCount("foo:a2"),
      MetricUrn.BuildAccumulatedValue("foo:a3"),
      MetricUrn.BuildSamplesCount("foo:a3")
    };

    var metricInfoSet = CreateMetricInfos.Execute(metrics,_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).HasSize(expected.Length);
    Check.That(urns).Contains(expected);
  }

  [Test]
  public void GivenAccumulator_WithGroups_WhenIExecute()
  {
    var inputUrn = Urn.BuildUrn("foo:inputUrn");
    var outputUrn = MetricUrn.Build("foo:outputUrn");

    var metric = MFactory
      .CreateAccumulatorMetric(outputUrn, inputUrn, 5)
      .Group.Every(7).Days
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "foo:outputUrn:accumulated_value",
      "foo:outputUrn:samples_count",
      "foo:outputUrn:_7Days:accumulated_value",
      "foo:outputUrn:_7Days:samples_count",
      "foo:outputUrn:_1Minutes:accumulated_value",
      "foo:outputUrn:_1Minutes:samples_count"
    };

    var metricInfoSet = CreateMetricInfos.Execute(new[] {metric},_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenAListOfVariation_WhenIExecute()
  {
    var metrics = new[]
    {
      CreateMetricVariation("foo:v1"),
      CreateMetricVariation("foo:v2"),
      CreateMetricVariation("foo:v3")
    };

    var expected = new Urn[]
    {
      "foo:v1",
      "foo:v2",
      "foo:v3"
    };

    var metricInfoSet = CreateMetricInfos.Execute(metrics,_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenVariation_WithGroups_WhenIExecute()
  {
    var metric = MFactory
      .CreateVariationMetric(MetricUrn.Build("foo:outputUrn"), "foo:inputUrn", 5)
      .Group.Every(30).Days
      .Group.Every(18).Minutes
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "foo:outputUrn",
      "foo:outputUrn:_30Days",
      "foo:outputUrn:_18Minutes"
    };

    var metricInfoSet = CreateMetricInfos.Execute(new[] {metric},_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenAListOfStateMonitoring_WhenIExecute()
  {
    var stateInput = PropertyUrn<PubState>.Build("state_input");
    var metrics = new[]
    {
      CreateMetricStateMonitoring("foo:s1", stateInput),
      CreateMetricStateMonitoring("foo:s2", stateInput),
      CreateMetricStateMonitoring("foo:s3", stateInput)
    };

    var expected = new Urn[]
    {
      MetricUrn.BuildOccurence("foo:s1", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s1", PubState.Disabled.ToString()),
      MetricUrn.BuildOccurence("foo:s2", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s2", PubState.Disabled.ToString()),
      MetricUrn.BuildOccurence("foo:s3", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s3", PubState.Disabled.ToString()),

      MetricUrn.BuildOccurence("foo:s1", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s1", PubState.Active.ToString()),
      MetricUrn.BuildOccurence("foo:s2", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s2", PubState.Active.ToString()),
      MetricUrn.BuildOccurence("foo:s3", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s3", PubState.Active.ToString())
    };

    var metricInfoSet = CreateMetricInfos.Execute(metrics,_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenStateMonitoring_WithMetricsIncluded_WhenIExecute()
  {
    var stateInput = PropertyUrn<PubState>.Build("state_input");

    var metric = MFactory
      .CreateStateMonitoringOfMetric(MetricUrn.Build("foo:s1"), stateInput, 40)
      .Including("electrical_delta").As.VariationOf("e_index")
      .Including("supply_temp").As.AccumulatorOf("i_supply_temp")
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "foo:s1:Disabled:occurrence",
      "foo:s1:Disabled:duration",

      "foo:s1:Disabled:supply_temp:accumulated_value",
      "foo:s1:Disabled:supply_temp:samples_count",
      "foo:s1:Disabled:electrical_delta",

      "foo:s1:Active:occurrence",
      "foo:s1:Active:duration",

      "foo:s1:Active:supply_temp:accumulated_value",
      "foo:s1:Active:supply_temp:samples_count",
      "foo:s1:Active:electrical_delta"
    };

    var metricInfoSet = CreateMetricInfos.Execute(new[] {metric},_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenStateMonitoring_WithMetricsIncluded_AndGroup_WhenIExecute()
  {
    var metric = MetricsDSL.Metric(ToMUrn("analytics:service:heating:public_state"))
      .Is
      .Every(5).Minutes
      .StateMonitoringOf(PropertyUrn<PubState>.Build("service:heating:public_state"))
      .Including("gas_index_delta").As.VariationOf("instrumentation:gas_index:measure")
      .Including("supply_temperature").As.AccumulatorOf("production:main_circuit:supply_temperature:measure")
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "analytics:service:heating:public_state:Disabled:occurrence",
      "analytics:service:heating:public_state:Disabled:duration",
      "analytics:service:heating:public_state:Disabled:gas_index_delta",
      "analytics:service:heating:public_state:Disabled:supply_temperature:accumulated_value",
      "analytics:service:heating:public_state:Disabled:supply_temperature:samples_count",
      "analytics:service:heating:public_state:_1Minutes:Disabled:occurrence",
      "analytics:service:heating:public_state:_1Minutes:Disabled:duration",
      "analytics:service:heating:public_state:_1Minutes:Disabled:gas_index_delta",
      "analytics:service:heating:public_state:_1Minutes:Disabled:supply_temperature:accumulated_value",
      "analytics:service:heating:public_state:_1Minutes:Disabled:supply_temperature:samples_count",
      "analytics:service:heating:public_state:Active:occurrence",
      "analytics:service:heating:public_state:Active:duration",
      "analytics:service:heating:public_state:Active:gas_index_delta",
      "analytics:service:heating:public_state:Active:supply_temperature:accumulated_value",
      "analytics:service:heating:public_state:Active:supply_temperature:samples_count",
      "analytics:service:heating:public_state:_1Minutes:Active:occurrence",
      "analytics:service:heating:public_state:_1Minutes:Active:duration",
      "analytics:service:heating:public_state:_1Minutes:Active:gas_index_delta",
      "analytics:service:heating:public_state:_1Minutes:Active:supply_temperature:accumulated_value",
      "analytics:service:heating:public_state:_1Minutes:Active:supply_temperature:samples_count"
    };

    var metricInfoSet = CreateMetricInfos.Execute(new[] {metric},_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenStateMonitoringOnStateMachine_WithMetricsIncluded_AndGroup_WhenIExecute()
  {
    var stateMachine = new MyStateMachine();
    
    var metric = MetricsDSL.Metric(ToMUrn("analytics:service:heating:state"))
      .Is
      .Every(5).Minutes
      .StateMonitoringOf(stateMachine.StateUrn)
      .Including("gas_index_delta").As.VariationOf("instrumentation:gas_index:measure")
      .Including("supply_temperature").As.AccumulatorOf("production:main_circuit:supply_temperature:measure")
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "analytics:service:heating:state:Disabled:occurrence",
      "analytics:service:heating:state:Disabled:duration",
      "analytics:service:heating:state:Disabled:gas_index_delta",
      "analytics:service:heating:state:Disabled:supply_temperature:accumulated_value",
      "analytics:service:heating:state:Disabled:supply_temperature:samples_count",
      "analytics:service:heating:state:_1Minutes:Disabled:occurrence",
      "analytics:service:heating:state:_1Minutes:Disabled:duration",
      "analytics:service:heating:state:_1Minutes:Disabled:gas_index_delta",
      "analytics:service:heating:state:_1Minutes:Disabled:supply_temperature:accumulated_value",
      "analytics:service:heating:state:_1Minutes:Disabled:supply_temperature:samples_count",
      "analytics:service:heating:state:Active:occurrence",
      "analytics:service:heating:state:Active:duration",
      "analytics:service:heating:state:Active:gas_index_delta",
      "analytics:service:heating:state:Active:supply_temperature:accumulated_value",
      "analytics:service:heating:state:Active:supply_temperature:samples_count",
      "analytics:service:heating:state:_1Minutes:Active:occurrence",
      "analytics:service:heating:state:_1Minutes:Active:duration",
      "analytics:service:heating:state:_1Minutes:Active:gas_index_delta",
      "analytics:service:heating:state:_1Minutes:Active:supply_temperature:accumulated_value",
      "analytics:service:heating:state:_1Minutes:Active:supply_temperature:samples_count"
    };

    var metricInfoSet = CreateMetricInfos.Execute(
      new[] {metric},
      new [] {stateMachine}
      );
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  class MyStateMachine : SubSystemDefinition<PubState>
  {
    public MyStateMachine() =>
      SubSystemNode = new SubSystemNode("heating", new RootModelNode("service"));
  }

  [Test]
  public void GivenAListOfMultipleAllMetricKinds_WhenIExecute()
  {
    var stateInput = PropertyUrn<PubState>.Build("state_input");
    var metrics = new[]
    {
      CreateMetricAccumulator("foo:a1"),
      CreateMetricVariation("foo:v1"),
      CreateMetricStateMonitoring("foo:s1", stateInput),
      CreateMetricGauge("foo:g1"),
      CreateMetricVariation("foo:v2"),
      CreateMetricGauge("foo:g2"),
      CreateMetricVariation("foo:v3"),
      CreateMetricAccumulator("foo:a2"),
      CreateMetricAccumulator("foo:a3"),
      CreateMetricStateMonitoring("foo:s2", stateInput)
    };

    var expected = new Urn[]
    {
      "foo:g1",
      "foo:g2",

      MetricUrn.BuildAccumulatedValue("foo:a1"),
      MetricUrn.BuildSamplesCount("foo:a1"),
      MetricUrn.BuildAccumulatedValue("foo:a2"),
      MetricUrn.BuildSamplesCount("foo:a2"),
      MetricUrn.BuildAccumulatedValue("foo:a3"),
      MetricUrn.BuildSamplesCount("foo:a3"),

      "foo:v1",
      "foo:v2",
      "foo:v3",

      MetricUrn.BuildOccurence("foo:s1", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s1", PubState.Disabled.ToString()),
      MetricUrn.BuildOccurence("foo:s2", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s2", PubState.Disabled.ToString()),

      MetricUrn.BuildOccurence("foo:s1", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s1", PubState.Active.ToString()),
      MetricUrn.BuildOccurence("foo:s2", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s2", PubState.Active.ToString())
    };

    var metricInfoSet = CreateMetricInfos.Execute(metrics,_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  [Test]
  public void GivenAListOfAllMetricKinds_WithGroups_WhenIExecute()
  {
    const string inputUrn = "foo:inputUrn";
    var stateInput = PropertyUrn<PubState>.Build("state_input");

    var gauge = MFactory
      .CreateGaugeMetric(MetricUrn.Build("foo:g1"), inputUrn, 10)
      .Group.Daily
      .Group.Every(20).Minutes
      .Builder.Build<Metric<MetricUrn>>();

    var accumulator = MFactory
      .CreateAccumulatorMetric(MetricUrn.Build("foo:a1"), inputUrn, 20)
      .Group.Hourly
      .Group.Every(21).Seconds
      .Builder.Build<Metric<MetricUrn>>();

    var variation = MFactory
      .CreateVariationMetric(MetricUrn.Build("foo:v1"), inputUrn, 30)
      .Group.Minutely
      .Group.Every(15).Days
      .Builder.Build<Metric<MetricUrn>>();

    var stateMonitoring = MFactory
      .CreateStateMonitoringOfMetric(MetricUrn.Build("foo:s1"), stateInput, 40)
      .Group.Every(10).Hours
      .Group.Every(27).Hours
      .Builder.Build<Metric<MetricUrn>>();

    var expected = new Urn[]
    {
      "foo:g1",
      "foo:g1:_1Days",
      "foo:g1:_20Minutes",

      MetricUrn.BuildAccumulatedValue("foo:a1"),
      MetricUrn.BuildSamplesCount("foo:a1"),
      MetricUrn.BuildAccumulatedValue("foo:a1:_1Hours"),
      MetricUrn.BuildSamplesCount("foo:a1:_1Hours"),
      MetricUrn.BuildAccumulatedValue("foo:a1:_21Seconds"),
      MetricUrn.BuildSamplesCount("foo:a1:_21Seconds"),

      "foo:v1",
      "foo:v1:_1Minutes",
      "foo:v1:_15Days",

      MetricUrn.BuildOccurence("foo:s1", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s1", PubState.Disabled.ToString()),
      MetricUrn.BuildOccurence("foo:s1", "_10Hours", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s1", "_10Hours", PubState.Disabled.ToString()),
      MetricUrn.BuildOccurence("foo:s1", "_27Hours", PubState.Disabled.ToString()),
      MetricUrn.BuildDuration("foo:s1", "_27Hours", PubState.Disabled.ToString()),

      MetricUrn.BuildOccurence("foo:s1", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s1", PubState.Active.ToString()),
      MetricUrn.BuildOccurence("foo:s1", "_10Hours", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s1", "_10Hours", PubState.Active.ToString()),
      MetricUrn.BuildOccurence("foo:s1", "_27Hours", PubState.Active.ToString()),
      MetricUrn.BuildDuration("foo:s1", "_27Hours", PubState.Active.ToString()),
    };

    var metricInfoSet = CreateMetricInfos.Execute(new[] {gauge, accumulator, variation, stateMonitoring},_noStateMachines);
    var urns = metricInfoSet.GetOutputUrns().ToArray();
    Check.That(urns).Contains(expected);
    Check.That(urns).HasSize(expected.Length);
  }

  private readonly ISubSystemDefinition[] _noStateMachines = Array.Empty<ISubSystemDefinition>();
}