using System;
using ImpliciX.Language.Core;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers.Variation;

internal sealed class VariationStore : StoreBase
{
  private string _firstKey;
  private string _lastKey;

  public Option<float> FirstValue
  {
    get => TimeMathReader.ReadTsLast(_firstKey).Map(o => o.Value);
    set => value.Tap(
      () => TimeMathWriter.DeleteTs(_firstKey),
      newValue => TimeMathWriter.WriteTsAndApplyRetention(_firstKey, TimeSpan.Zero, newValue)
    );
  }

  public Option<float> LastValue
  {
    get => TimeMathReader.ReadTsLast(_lastKey).Map(o => o.Value);
    set =>
      value.Tap(
        () => TimeMathWriter.DeleteTs(_lastKey),
        newValue => TimeMathWriter.WriteTsAndApplyRetention(_lastKey, TimeSpan.Zero, newValue)
      );
  }

  public VariationStore(
    string rootKey,
    ITimeMathReader timeMathReader,
    ITimeMathWriter timeMathWriter
  ) : base(rootKey, timeMathReader, timeMathWriter)
  {
    _firstKey = $"{rootKey}${nameof(_firstKey)}";
    _lastKey = $"{rootKey}${nameof(_lastKey)}";

    timeMathWriter.SetupTimeSeries(_firstKey, KeepLatestOnly);
    timeMathWriter.SetupTimeSeries(_lastKey, KeepLatestOnly);
  }
}