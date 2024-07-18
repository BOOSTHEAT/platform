using System;
using System.Collections.Generic;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using MFactory = ImpliciX.TestsCommon.MetricFactoryHelper;
using MetricsDSL = ImpliciX.Language.Metrics.Metrics;
using static ImpliciX.Data.Tests.Metrics.MetricsHelpers;

namespace ImpliciX.Data.Tests.Metrics;

public class FactoryStateMonitoringInfoTests
{
  private readonly PropertyUrn<PubState> _inputUrn = PropertyUrn<PubState>.Build("inputUrn");
  private readonly MetricUrn _outputUrn = MetricUrn.Build("outputUrn");
  private readonly TimeHelper T = TimeHelper.Minutes();
  private readonly Dictionary<Urn, Type> _stateTypes = new();


  [Test]
  public void GivenWrongMetricKind_WhenICreate_ThenIGetAnError()
  {
    var metric = MFactory
      .CreateVariationMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var ex = Check.ThatCode(() => MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes))
      .Throws<InvalidOperationException>()
      .Value;

    Check.That(ex.Message).Contains("Metric kind must be 'State', but this metric has kind is 'Variation'");
  }

  [Test]
  public void GivenSimpleOne_WhenICreate()
  {
    var metric = MFactory
      .CreateStateMonitoringOfMetric(_outputUrn, _inputUrn, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes);

    Check.That(info.PublicationPeriod).IsEqualTo(T._5);
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.States).HasSize(2);
    Check.That(info.States[PubState.Disabled].Occurrence).IsEqualTo(ToMUrn("outputUrn:Disabled:occurrence"));
    Check.That(info.States[PubState.Disabled].Duration).IsEqualTo(ToMUrn("outputUrn:Disabled:duration"));
    Check.That(info.States[PubState.Disabled].Accumulators).IsEmpty();
    Check.That(info.States[PubState.Disabled].Variations).IsEmpty();
    Check.That(info.States[PubState.Active].Occurrence).IsEqualTo(ToMUrn("outputUrn:Active:occurrence"));
    Check.That(info.States[PubState.Active].Duration).IsEqualTo(ToMUrn("outputUrn:Active:duration"));
    Check.That(info.States[PubState.Active].Accumulators).IsEmpty();
    Check.That(info.States[PubState.Active].Variations).IsEmpty();
    Check.That(info.WindowRetention.IsSome).IsFalse();
  }

  [Test]
  public void GivenComplexOne_WhenICreate()
  {
    var metric = MFactory
      .CreateStateMonitoringOfMetric(_outputUrn, _inputUrn, 5)
      .Including("gaz_delta").As.VariationOf("g_index")
      .Including("electrical_delta").As.VariationOf("e_index")
      .Including("supply_temp").As.AccumulatorOf("i_supply_temp")
      .Including("temperature").As.AccumulatorOf("i_home_temp")
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes);

    //--- PubState.Disabled

    Check.That(info.PublicationPeriod).IsEqualTo(T._5);
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.States).HasSize(2);

    var state = info.States[PubState.Disabled];

    Check.That(state.RootUrn).IsEqualTo(ToMUrn("outputUrn:Disabled"));
    Check.That(state.Occurrence).IsEqualTo(ToMUrn("outputUrn:Disabled:occurrence"));
    Check.That(state.Duration).IsEqualTo(ToMUrn("outputUrn:Disabled:duration"));

    Check.That(state.Variations).HasSize(2);
    Check.That(state.Variations[0].InputUrn).IsEqualTo(ToUrn("g_index"));
    Check.That(state.Variations[0].OutputUrn).IsEqualTo(ToMUrn("outputUrn:Disabled:gaz_delta"));

    Check.That(state.Variations[1].InputUrn).IsEqualTo(ToUrn("e_index"));
    Check.That(state.Variations[1].OutputUrn).IsEqualTo(ToMUrn("outputUrn:Disabled:electrical_delta"));

    Check.That(state.Accumulators).HasSize(2);
    Check.That(state.Accumulators[0].InputUrn).IsEqualTo(ToUrn("i_supply_temp"));
    Check.That(state.Accumulators[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:Disabled:supply_temp"));
    Check.That(state.Accumulators[0].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:Disabled:supply_temp:accumulated_value"));
    Check.That(state.Accumulators[0].SamplesCount).IsEqualTo(ToMUrn("outputUrn:Disabled:supply_temp:samples_count"));

    Check.That(state.Accumulators[1].InputUrn).IsEqualTo(ToUrn("i_home_temp"));
    Check.That(state.Accumulators[1].RootUrn).IsEqualTo(ToMUrn("outputUrn:Disabled:temperature"));
    Check.That(state.Accumulators[1].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:Disabled:temperature:accumulated_value"));
    Check.That(state.Accumulators[1].SamplesCount).IsEqualTo(ToMUrn("outputUrn:Disabled:temperature:samples_count"));

    //--- PubState.Active

    state = info.States[PubState.Active];

    Check.That(state.RootUrn).IsEqualTo(ToMUrn("outputUrn:Active"));
    Check.That(state.Occurrence).IsEqualTo(ToMUrn("outputUrn:Active:occurrence"));
    Check.That(state.Duration).IsEqualTo(ToMUrn("outputUrn:Active:duration"));

    Check.That(state.Variations).HasSize(2);
    Check.That(state.Variations[0].InputUrn).IsEqualTo(ToUrn("g_index"));
    Check.That(state.Variations[0].OutputUrn).IsEqualTo(ToMUrn("outputUrn:Active:gaz_delta"));

    Check.That(state.Variations[1].InputUrn).IsEqualTo(ToUrn("e_index"));
    Check.That(state.Variations[1].OutputUrn).IsEqualTo(ToMUrn("outputUrn:Active:electrical_delta"));

    Check.That(state.Accumulators).HasSize(2);
    Check.That(state.Accumulators[0].InputUrn).IsEqualTo(ToUrn("i_supply_temp"));
    Check.That(state.Accumulators[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:Active:supply_temp"));
    Check.That(state.Accumulators[0].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:Active:supply_temp:accumulated_value"));
    Check.That(state.Accumulators[0].SamplesCount).IsEqualTo(ToMUrn("outputUrn:Active:supply_temp:samples_count"));

    Check.That(state.Accumulators[1].InputUrn).IsEqualTo(ToUrn("i_home_temp"));
    Check.That(state.Accumulators[1].RootUrn).IsEqualTo(ToMUrn("outputUrn:Active:temperature"));
    Check.That(state.Accumulators[1].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:Active:temperature:accumulated_value"));

    Check.That(state.Accumulators[1].SamplesCount).IsEqualTo(ToMUrn("outputUrn:Active:temperature:samples_count"));

    Check.That(info.WindowRetention.IsSome).IsFalse();
  }

  [Test]
  public void GivenWithTwoGroups_OneIncluding_WhenICreate()
  {
    var metric = MetricsDSL.Metric(_outputUrn)
      .Is
      .Every(5).Minutes
      .StateMonitoringOf(_inputUrn)
      .Including("supply_temp").As.AccumulatorOf("i_supply_temp")
      .Group.Every(7).Days
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(5));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(metric.TargetUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();

    Check.That(info.Groups).HasSize(2);

    //--- PubState.Disabled

    var group_0 = info.Groups[0].States[PubState.Disabled];
    Check.That(info.Groups[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:_7Days"));
    Check.That(group_0.RootUrn).IsEqualTo(ToMUrn("outputUrn:_7Days:Disabled"));
    Check.That(group_0.Occurrence).IsEqualTo(ToMUrn("outputUrn:_7Days:Disabled:occurrence"));
    Check.That(group_0.Duration).IsEqualTo(ToMUrn("outputUrn:_7Days:Disabled:duration"));

    Check.That(group_0.Accumulators).HasSize(1);
    Check.That(group_0.Accumulators[0].InputUrn).IsEqualTo(ToUrn("i_supply_temp"));
    Check.That(group_0.Accumulators[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:_7Days:Disabled:supply_temp"));
    Check.That(group_0.Accumulators[0].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:_7Days:Disabled:supply_temp:accumulated_value"));
    Check.That(group_0.Accumulators[0].SamplesCount).IsEqualTo(ToMUrn("outputUrn:_7Days:Disabled:supply_temp:samples_count"));

    var group_1 = info.Groups[1].States[PubState.Disabled];
    Check.That(info.Groups[1].RootUrn).IsEqualTo(ToMUrn("outputUrn:_1Minutes"));
    Check.That(group_1.RootUrn).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Disabled"));
    Check.That(group_1.Occurrence).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Disabled:occurrence"));
    Check.That(group_1.Duration).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Disabled:duration"));

    Check.That(group_1.Accumulators).HasSize(1);
    Check.That(group_1.Accumulators[0].InputUrn).IsEqualTo(ToUrn("i_supply_temp"));
    Check.That(group_1.Accumulators[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Disabled:supply_temp"));
    Check.That(group_1.Accumulators[0].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Disabled:supply_temp:accumulated_value"));
    Check.That(group_1.Accumulators[0].SamplesCount).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Disabled:supply_temp:samples_count"));

    //--- PubState.Active

    group_0 = info.Groups[0].States[PubState.Active];
    Check.That(info.Groups[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:_7Days"));
    Check.That(group_0.RootUrn).IsEqualTo(ToMUrn("outputUrn:_7Days:Active"));
    Check.That(group_0.Occurrence).IsEqualTo(ToMUrn("outputUrn:_7Days:Active:occurrence"));
    Check.That(group_0.Duration).IsEqualTo(ToMUrn("outputUrn:_7Days:Active:duration"));

    Check.That(group_0.Accumulators).HasSize(1);
    Check.That(group_0.Accumulators[0].InputUrn).IsEqualTo(ToUrn("i_supply_temp"));
    Check.That(group_0.Accumulators[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:_7Days:Active:supply_temp"));
    Check.That(group_0.Accumulators[0].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:_7Days:Active:supply_temp:accumulated_value"));
    Check.That(group_0.Accumulators[0].SamplesCount).IsEqualTo(ToMUrn("outputUrn:_7Days:Active:supply_temp:samples_count"));

    group_1 = info.Groups[1].States[PubState.Active];
    Check.That(info.Groups[1].RootUrn).IsEqualTo(ToMUrn("outputUrn:_1Minutes"));
    Check.That(group_1.RootUrn).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Active"));
    Check.That(group_1.Occurrence).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Active:occurrence"));
    Check.That(group_1.Duration).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Active:duration"));

    Check.That(group_1.Accumulators).HasSize(1);
    Check.That(group_1.Accumulators[0].InputUrn).IsEqualTo(ToUrn("i_supply_temp"));
    Check.That(group_1.Accumulators[0].RootUrn).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Active:supply_temp"));
    Check.That(group_1.Accumulators[0].AccumulatedValue).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Active:supply_temp:accumulated_value"));
    Check.That(group_1.Accumulators[0].SamplesCount).IsEqualTo(ToMUrn("outputUrn:_1Minutes:Active:supply_temp:samples_count"));

    Check.That(info.WindowRetention.IsSome).IsFalse();
  }

  [Test]
  public void GivenWithOneGroups_TwoIncluding_WhenICreate()
  {
    var metric = MetricsDSL.Metric(ToMUrn("analytics:service:heating:public_state"))
      .Is
      .Every(5).Minutes
      .StateMonitoringOf(PropertyUrn<PubState>.Build("service:heating:public_state"))
      .Including("gas_index_delta").As.VariationOf("instrumentation:gas_index:measure")
      .Including("supply_temperature").As.AccumulatorOf("production:main_circuit:supply_temperature:measure")
      .Group.Minutely
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes);
    
    // @formatter:off
    var expected = new (object Challenger, object ExpectedValue)[]
    {
      (info.PublicationPeriod,                                                    TimeSpan.FromMinutes(5)),
      (info.InputUrn,                                                             "service:heating:public_state"),
      (info.RootUrn,                                                              "analytics:service:heating:public_state"),
      
      // ---- PubState.Disabled State
      (info.States[PubState.Disabled].RootUrn,                                    "analytics:service:heating:public_state:Disabled"),
      (info.States[PubState.Disabled].Occurrence,                                 "analytics:service:heating:public_state:Disabled:occurrence"),
      (info.States[PubState.Disabled].Duration,                                   "analytics:service:heating:public_state:Disabled:duration"),
      
      (info.States[PubState.Disabled].Variations.Length,                          1),
      (info.States[PubState.Disabled].Variations[0].InputUrn,                     "instrumentation:gas_index:measure"),
      (info.States[PubState.Disabled].Variations[0].OutputUrn,                    "analytics:service:heating:public_state:Disabled:gas_index_delta"),
      
      (info.States[PubState.Disabled].Accumulators.Length,                        1),
      (info.States[PubState.Disabled].Accumulators[0].InputUrn,                   "production:main_circuit:supply_temperature:measure"),
      (info.States[PubState.Disabled].Accumulators[0].RootUrn,                    "analytics:service:heating:public_state:Disabled:supply_temperature"),
      (info.States[PubState.Disabled].Accumulators[0].AccumulatedValue,           "analytics:service:heating:public_state:Disabled:supply_temperature:accumulated_value"),
      (info.States[PubState.Disabled].Accumulators[0].SamplesCount,               "analytics:service:heating:public_state:Disabled:supply_temperature:samples_count"),
      
      (info.Groups.Length,                                                        1),
      (info.Groups[0].PublicationPeriod,                                          TimeSpan.FromMinutes(1)),
      (info.Groups[0].RootUrn,                                                    "analytics:service:heating:public_state:_1Minutes"),
      (info.Groups[0].States[PubState.Disabled].RootUrn,                          "analytics:service:heating:public_state:_1Minutes:Disabled"),
      (info.Groups[0].States[PubState.Disabled].Occurrence,                       "analytics:service:heating:public_state:_1Minutes:Disabled:occurrence"),
      (info.Groups[0].States[PubState.Disabled].Duration,                         "analytics:service:heating:public_state:_1Minutes:Disabled:duration"),
      (info.Groups[0].States[PubState.Disabled].Variations.Length,                1),
      (info.Groups[0].States[PubState.Disabled].Variations[0].InputUrn,           "instrumentation:gas_index:measure"),
      (info.Groups[0].States[PubState.Disabled].Variations[0].OutputUrn,          "analytics:service:heating:public_state:_1Minutes:Disabled:gas_index_delta"),
      (info.Groups[0].States[PubState.Disabled].Accumulators.Length,              1),
      (info.Groups[0].States[PubState.Disabled].Accumulators[0].InputUrn,         "production:main_circuit:supply_temperature:measure"),
      (info.Groups[0].States[PubState.Disabled].Accumulators[0].RootUrn,          "analytics:service:heating:public_state:_1Minutes:Disabled:supply_temperature"),
      (info.Groups[0].States[PubState.Disabled].Accumulators[0].AccumulatedValue, "analytics:service:heating:public_state:_1Minutes:Disabled:supply_temperature:accumulated_value"),
      (info.Groups[0].States[PubState.Disabled].Accumulators[0].SamplesCount,     "analytics:service:heating:public_state:_1Minutes:Disabled:supply_temperature:samples_count"),
      
      // ---- PubState.Active State
      (info.States[PubState.Active].RootUrn,                                    "analytics:service:heating:public_state:Active"),
      (info.States[PubState.Active].Occurrence,                                 "analytics:service:heating:public_state:Active:occurrence"),
      (info.States[PubState.Active].Duration,                                   "analytics:service:heating:public_state:Active:duration"),
      
      (info.States[PubState.Active].Variations.Length,                          1),
      (info.States[PubState.Active].Variations[0].InputUrn,                     "instrumentation:gas_index:measure"),
      (info.States[PubState.Active].Variations[0].OutputUrn,                    "analytics:service:heating:public_state:Active:gas_index_delta"),
      
      (info.States[PubState.Active].Accumulators.Length,                        1),
      (info.States[PubState.Active].Accumulators[0].InputUrn,                   "production:main_circuit:supply_temperature:measure"),
      (info.States[PubState.Active].Accumulators[0].RootUrn,                    "analytics:service:heating:public_state:Active:supply_temperature"),
      (info.States[PubState.Active].Accumulators[0].AccumulatedValue,           "analytics:service:heating:public_state:Active:supply_temperature:accumulated_value"),
      (info.States[PubState.Active].Accumulators[0].SamplesCount,               "analytics:service:heating:public_state:Active:supply_temperature:samples_count"),
      
      (info.Groups.Length,                                                      1),
      (info.Groups[0].PublicationPeriod,                                        TimeSpan.FromMinutes(1)),
      (info.Groups[0].RootUrn,                                                  "analytics:service:heating:public_state:_1Minutes"),
      (info.Groups[0].States[PubState.Active].RootUrn,                          "analytics:service:heating:public_state:_1Minutes:Active"),
      (info.Groups[0].States[PubState.Active].Occurrence,                       "analytics:service:heating:public_state:_1Minutes:Active:occurrence"),
      (info.Groups[0].States[PubState.Active].Duration,                         "analytics:service:heating:public_state:_1Minutes:Active:duration"),
      (info.Groups[0].States[PubState.Active].Variations.Length,                1),
      (info.Groups[0].States[PubState.Active].Variations[0].InputUrn,           "instrumentation:gas_index:measure"),
      (info.Groups[0].States[PubState.Active].Variations[0].OutputUrn,          "analytics:service:heating:public_state:_1Minutes:Active:gas_index_delta"),
      (info.Groups[0].States[PubState.Active].Accumulators.Length,              1),
      (info.Groups[0].States[PubState.Active].Accumulators[0].InputUrn,         "production:main_circuit:supply_temperature:measure"),
      (info.Groups[0].States[PubState.Active].Accumulators[0].RootUrn,          "analytics:service:heating:public_state:_1Minutes:Active:supply_temperature"),
      (info.Groups[0].States[PubState.Active].Accumulators[0].AccumulatedValue, "analytics:service:heating:public_state:_1Minutes:Active:supply_temperature:accumulated_value"),
      (info.Groups[0].States[PubState.Active].Accumulators[0].SamplesCount,     "analytics:service:heating:public_state:_1Minutes:Active:supply_temperature:samples_count")
    };
    // @formatter:on

    expected.ForEach(e =>
    {
      switch (e.Challenger)
      {
        case Urn urn:
          Check.That(urn.Value).IsEqualTo(e.ExpectedValue);
          break;
        default:
          Check.That(e.Challenger).IsEqualTo(e.ExpectedValue);
          break;
      }
    });
  }

  [Test]
  public void GivenWithWindow_WhenICreate()
  {
    var metric = MetricsDSL.Metric(_outputUrn)
      .Is
      .Every(3).Minutes
      .OnAWindowOf(15).Minutes
      .StateMonitoringOf(_inputUrn)
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(3));
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.Groups).IsEmpty();
    Check.That(info.WindowRetention.IsSome).IsTrue();
    Check.That(info.WindowRetention.GetValue()).IsEqualTo(TimeSpan.FromMinutes(15));
  }
  
  [Test]
  public void GivenWithStorageRetention_WhenICreate()
  {
    var metric = MetricsDSL.Metric(_outputUrn)
      .Is
      .Every(3).Minutes
      .StateMonitoringOf(_inputUrn)
      .Over.ThePast(15).Days
      .Group.Daily.Over.ThePast(30).Days
      .Builder.Build<Metric<MetricUrn>>();

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,_stateTypes);

    Check.That(info.PublicationPeriod).IsEqualTo(TimeSpan.FromMinutes(3));
    Check.That(info.InputUrn).IsEqualTo(_inputUrn);
    Check.That(info.RootUrn).IsEqualTo(metric.TargetUrn);
    Check.That(info.StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromDays(15));
    Check.That(info.WindowRetention.IsSome).IsFalse();
    Check.That(info.Groups).HasSize(1);
    Check.That(info.Groups[0].StorageRetention.GetValue()).IsEqualTo(TimeSpan.FromDays(30));
  }
  
  [Test]
  public void MetricInputIsStateOfSomeStateMachine()
  {
    var stateMachineNode = new SubSystemNode("machine", new RootModelNode("root"));
    var metric = MFactory
      .CreateStateMonitoringOfMetric(_outputUrn, stateMachineNode.state, 5)
      .Builder.Build<Metric<MetricUrn>>();

    var stateTypes = new Dictionary<Urn, Type>
    {
      [stateMachineNode.state] = typeof(PubState)
    };

    var info = MetricInfoFactory.CreateStateMonitoringInfo(metric,stateTypes);

    Check.That(info.PublicationPeriod).IsEqualTo(T._5);
    Check.That(info.InputUrn).IsEqualTo(stateMachineNode.state);
    Check.That(info.RootUrn).IsEqualTo(_outputUrn);
    Check.That(info.StorageRetention.IsSome).IsFalse();
    Check.That(info.States).HasSize(2);
    Check.That(info.States[PubState.Disabled].Occurrence).IsEqualTo(ToMUrn("outputUrn:Disabled:occurrence"));
    Check.That(info.States[PubState.Disabled].Duration).IsEqualTo(ToMUrn("outputUrn:Disabled:duration"));
    Check.That(info.States[PubState.Disabled].Accumulators).IsEmpty();
    Check.That(info.States[PubState.Disabled].Variations).IsEmpty();
    Check.That(info.States[PubState.Active].Occurrence).IsEqualTo(ToMUrn("outputUrn:Active:occurrence"));
    Check.That(info.States[PubState.Active].Duration).IsEqualTo(ToMUrn("outputUrn:Active:duration"));
    Check.That(info.States[PubState.Active].Accumulators).IsEmpty();
    Check.That(info.States[PubState.Active].Variations).IsEmpty();
    Check.That(info.WindowRetention.IsSome).IsFalse();
  }

}