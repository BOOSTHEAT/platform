using ImpliciX.Language.Model;

namespace ImpliciX.ReferenceApp.Model.Tree;

public class modboss : ModelNode
{
  public modboss(string name, ModelNode parent) : base(name, parent)
  {
    temperature = new MeasureNode<Temperature>(nameof(temperature), this);
    percentage = new MeasureNode<Percentage>(nameof(percentage), this);
    value1 = new MeasureNode<SomeEnum>(nameof(value1), this);
    value2 = new MeasureNode<SomeEnum>(nameof(value2), this);
  }

  public MeasureNode<Temperature> temperature { get; }
  public MeasureNode<Percentage> percentage { get; }
  public MeasureNode<SomeEnum> value1 { get; }
  public MeasureNode<SomeEnum> value2 { get; }
}

[ValueObject]
public enum SomeEnum
{
  Zero = 0,
  One = 1,
  Two = 2,
}