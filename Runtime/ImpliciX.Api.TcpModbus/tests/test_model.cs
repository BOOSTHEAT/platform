using ImpliciX.Language.Model;

namespace ImpliciX.Api.TcpModbus.Tests;

public class test_model : RootModelNode
{
  static test_model()
  {
    temp1 = new MeasureNode<Temperature>(nameof(temp1), new test_model());
    temp2 = new MeasureNode<Temperature>(nameof(temp2), new test_model());
    temp3 = new MeasureNode<Temperature>(nameof(temp3), new test_model());
    c001 = PropertyUrn<AlarmState>.Build(nameof(test_model), nameof(c001));
    c002 = PropertyUrn<AlarmState>.Build(nameof(test_model), nameof(c002));
    not_mapped = new MeasureNode<Temperature>(nameof(not_mapped), new test_model());
    presence = PropertyUrn<Presence>.Build(nameof(test_model), nameof(presence));
    dummy = PropertyUrn<DummyState>.Build(nameof(test_model), nameof(dummy));
    counter1 = MetricUrn.Build(nameof(test_model), nameof(counter1));
    threshold = UserSettingUrn<Percentage>.Build(nameof(test_model), nameof(threshold));
    change = CommandUrn<Percentage>.Build(nameof(test_model), nameof(change));
  }

  public static MetricUrn counter1 { get; set; }

  public static MeasureNode<Temperature> temp1 { get; }
  public static MeasureNode<Temperature> temp2 { get; }
  public static MeasureNode<Temperature> temp3 { get; }
  public static MeasureNode<Temperature> not_mapped { get; }

  public static PropertyUrn<Presence> presence { get; }
  public static PropertyUrn<DummyState> dummy { get; }
  public static PropertyUrn<AlarmState> c001 { get; }
  public static PropertyUrn<AlarmState> c002 { get; }

  public static UserSettingUrn<Percentage> threshold { get; }
  
  public static CommandUrn<Percentage> change { get; }

  public test_model() : base(nameof(test_model))
  {
  }
}

[ValueObject]
public enum DummyState
{
  A = 23,
  B = 66
}