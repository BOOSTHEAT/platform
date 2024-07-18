using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers;

public abstract class SubTimeMathComputer
{
  public abstract float ComputeValueToStore(
    Urn rootUrn,
    ITimeMathReader timeMathReader,
    IDataModelValue updateValue
  );

  public virtual FloatValueAt ComputeValueToPublish(
    Urn rootUrn,
    ITimeMathReader timeMathReader,
    TimeSpan start,
    TimeSpan now,
    FloatValueAt lastUpdateValue
  )
  {
    return lastUpdateValue;
  }

  public abstract Option<FloatValueAt> GetDefaultValue();
}
