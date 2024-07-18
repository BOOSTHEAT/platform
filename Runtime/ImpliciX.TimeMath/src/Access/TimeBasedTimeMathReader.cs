using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using static ImpliciX.TimeMath.Access.ITimeSeriesSuffixes;

namespace ImpliciX.TimeMath.Access;

public class TimeBasedTimeMathReader : ITimeMathReader
{
  private readonly IReadTimeSeries _tsReader;

  public TimeBasedTimeMathReader(
    IReadTimeSeries tsReader
  )
  {
    _tsReader = tsReader;
  }

  public Option<FloatValueAt> ReadLastUpdate(
    string rootUrn,
    string suffix
  )
  {
    return ReadTsLast(
      ToTsName(
        rootUrn,
        suffix,
        ValueAtUpdateSuffix
      )
    );
  }

  public TimeSpan ReadStartAt(
    string rootUrn
  )
  {
    return _tsReader.ReadLast(
        ToTsName(
          rootUrn,
          StartSuffix
        )
      )
      .Match(
        () => TimeSpan.Zero,
        value => value.At
      );
  }

  public Option<FloatValueAt> ReadFirstValueAtPublish(
    string rootUrn,
    string suffix,
    TimeSpan start
  )
  {
    return ReadTsLast(
      ToTsName(
        rootUrn,
        suffix,
        ValueAtPublishedSuffix
      ),
      start
    );
  }

  public TimeSpan ReadEndAt(
    string rootUrn
  )
  {
    return _tsReader.ReadLast(
        ToTsName(
          rootUrn,
          EndSuffix
        )
      )
      .Match(
        () => TimeSpan.Zero,
        value => value.At
      );
  }

  public Option<TimeSpan> ReadLastPublishedInstant(
    string rootUrn
  )
  {
    return _tsReader.ReadLast(
        ToTsName(
          rootUrn,
          LastPublishedInstantSuffix
        )
      )
      .Match(
        () => Option<TimeSpan>.None(),
        value => Option<TimeSpan>.Some(value.At)
      );
  }

  #region IReadTimeSeries Decoration

  public Option<FloatValueAt> ReadTsFirst(
    string key,
    TimeSpan? from = null
  )
  {
    return _tsReader.ReadFirst(
      key,
      from?.Ticks
    ).ToFloatValueAtOption();
  }

  public Option<FloatValueAt> ReadTsLast(
    string key,
    TimeSpan? upTo = null
  )
  {
    return _tsReader.ReadLast(
      key,
      upTo?.Ticks
    ).ToFloatValueAtOption();
  }

  public IEnumerable<FloatValueAt> ReadTsAll(
    string key
  )
  {
    return _tsReader.ReadAll(
      key
    ).Match(
      Enumerable.Empty<FloatValueAt>,
      xs => xs.Select(
        x => new FloatValueAt(x.At, x.Value)
        )
    );
  }

  #endregion
}
