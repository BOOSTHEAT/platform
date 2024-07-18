using System;
using ImpliciX.Language.Core;
using ImpliciX.TimeMath.Access;

namespace ImpliciX.TimeMath.Computers;

internal abstract class StoreBase
{
  private string _samplingEndAtKey;
  private string _samplingStartAtKey;

  public static TimeSpan KeepLatestOnly = TimeSpan.Zero;
  protected ITimeMathReader TimeMathReader { get; }
  protected ITimeMathWriter TimeMathWriter { get; }

  public Option<TimeSpan> SamplingStartAt
  {
    get => TimeMathReader.ReadTsLast(_samplingStartAtKey).Map(floatAt => floatAt.At);
    set => value.Tap(
      () => TimeMathWriter.DeleteTs(_samplingStartAtKey),
      newValue => TimeMathWriter.WriteTsAndApplyRetention(_samplingStartAtKey, newValue, 0)
    );
  }

  public TimeSpan SamplingEndAt
  {
    get => TimeMathReader.ReadTsLast(_samplingEndAtKey).GetValue().At;
    set => TimeMathWriter.WriteTsAndApplyRetention(_samplingEndAtKey, value, 0);
  }

  protected StoreBase(string rootKey, ITimeMathReader timeMathReader, ITimeMathWriter timeMathWriter)
  {
    TimeMathReader = timeMathReader ?? throw new ArgumentNullException(nameof(timeMathReader));
    TimeMathWriter = timeMathWriter ?? throw new ArgumentNullException(nameof(timeMathWriter));

    _samplingStartAtKey = $"{rootKey}${nameof(_samplingStartAtKey)}";
    _samplingEndAtKey = $"{rootKey}${nameof(_samplingEndAtKey)}";
    timeMathWriter.SetupTimeSeries(_samplingStartAtKey, KeepLatestOnly);
    timeMathWriter.SetupTimeSeries(_samplingEndAtKey, KeepLatestOnly);
  }
}