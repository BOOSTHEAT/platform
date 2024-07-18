using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers;

internal class AccumulatorComputer : TimeMathComputer
{
  private static readonly Dictionary<string, SubTimeMathComputer> SubComputers = new ()
  {
    { MetricUrn.BuildAccumulatedValue(), new SubAccumulatedValueComputer() },
    { MetricUrn.BuildSamplesCount(), new SubSamplesCountComputer() }
  };

  public AccumulatorComputer(
    PropertyUrn<MetricValue> rootUrn,
    ITimeMathWriter timeMathWriter,
    ITimeMathReader timeMathReader,
    Option<TimeSpan> windowRetention,
    TimeSpan start
  ) : base(
    rootUrn,
    timeMathReader,
    timeMathWriter,
    windowRetention,
    start,
    SubComputers
  )
  {
  }

  private class SubAccumulatedValueComputer : SubTimeMathComputer
  {
    private readonly string _suffix = MetricUrn.BuildAccumulatedValue();

    public override float ComputeValueToStore(
      Urn rootUrn,
      ITimeMathReader timeMathReader,
      IDataModelValue updateValue
    )
    {
      var previousValue = timeMathReader.ReadLastUpdate(
        rootUrn,
        _suffix
      );
      return updateValue.ToFloat().Value + previousValue.Match(
        () => 0.0f,
        at => at.Value
      );
    }

    public override FloatValueAt ComputeValueToPublish(
      Urn rootUrn,
      ITimeMathReader timeMathReader,
      TimeSpan start,
      TimeSpan now,
      FloatValueAt lastUpdateValue
    )
    {
      var firstPublishedValue = timeMathReader.ReadFirstValueAtPublish(
          rootUrn,
          _suffix,
          start
        )
        .Match(
          () => 0,
          at => at.Value
        );
      var newValue = lastUpdateValue.Value - firstPublishedValue;
      return new FloatValueAt(
        lastUpdateValue.At,
        newValue
      );
    }

    public override Option<FloatValueAt> GetDefaultValue()
    {
      return Option<FloatValueAt>.Some(new FloatValueAt());
    }
  }

  private class SubSamplesCountComputer : SubTimeMathComputer
  {
    private readonly string _suffix = MetricUrn.BuildSamplesCount();

    public override float ComputeValueToStore(
      Urn rootUrn,
      ITimeMathReader timeMathReader,
      IDataModelValue updateValue
    )
    {
      var previousCount = timeMathReader.ReadLastUpdate(
          rootUrn,
          _suffix
        )
        .Match(
          () => 0,
          at => at.Value
        );

      return 1 + previousCount;
    }

    public override FloatValueAt ComputeValueToPublish(
      Urn rootUrn,
      ITimeMathReader timeMathReader,
      TimeSpan start,
      TimeSpan now,
      FloatValueAt lastUpdateValue
    )
    {
      var firstCount = timeMathReader.ReadFirstValueAtPublish(
          rootUrn,
          _suffix,
          start
        )
        .Match(
          () => 0,
          at => at.Value
        );

      var newValue = lastUpdateValue.Value - firstCount;
      return new FloatValueAt(
        lastUpdateValue.At,
        newValue
      );
    }

    public override Option<FloatValueAt> GetDefaultValue()
    {
      return Option<FloatValueAt>.Some(new FloatValueAt());
    }
  }
}
