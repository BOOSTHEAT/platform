using ImpliciX.Language.Model;

namespace ImpliciX.TimeMath.Tests.Helpers;

public class fake_model : RootModelNode
{
  public enum PublicState
  {
    Running = 2,
    Disabled = 0
  }

  public enum PublicState2
  {
    Running = 2,
    Disabled = 0,
    Other = 3
  }

  static fake_model()
  {
    var parent = new fake_model();
    public_state = PropertyUrn<PublicState>.Build("fake_model", nameof(public_state));
    public_state2 = PropertyUrn<PublicState2>.Build("fake_model", nameof(public_state2));
    notInDeclarations = PropertyUrn<Counter>.Build("fake_model", nameof(notInDeclarations));
    C666 = new AlarmNode(nameof(C666), parent);
    temperature = new fake_temperature(parent);
    fake_index = PropertyUrn<Flow>.Build("fake_model", nameof(fake_index));
    fake_index_again = PropertyUrn<Flow>.Build("fake_model", nameof(fake_index_again));
    dummy_subsystem = new dummy_subsystem(parent);
  }

  public fake_model() : base(nameof(fake_model))
  {
  }

  public static PropertyUrn<PublicState> public_state { get; }
  public static fake_temperature temperature { get; }
  public static PropertyUrn<Flow> fake_index { get; }
  public static PropertyUrn<Flow> fake_index_again { get; }
  public static PropertyUrn<PublicState2> public_state2 { get; }
  public static PropertyUrn<Counter> notInDeclarations { get; }
  public static AlarmNode C666 { get; }
  public static dummy_subsystem dummy_subsystem { get; }
}

public class dummy_subsystem : SubSystemNode
{
  public dummy_subsystem(ModelNode parent) : base(nameof(dummy_subsystem), parent)
  {
  }
}

public class fake_temperature : MeasureNode<Temperature>
{
  public fake_temperature(ModelNode parent) : base(nameof(fake_temperature), parent)
  {
  }
}
