using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;

namespace ImpliciX.TimeMath.Access;

public interface ITimeMathReader
{
  public Option<FloatValueAt> ReadLastUpdate(
    string rootUrn,
    string suffix
  );

  public TimeSpan ReadStartAt(
    string rootUrn
  );

  public Option<FloatValueAt> ReadFirstValueAtPublish(
    string rootUrn,
    string suffix,
    TimeSpan start
  );

  public TimeSpan ReadEndAt(
    string rootUrn
  );

  Option<TimeSpan> ReadLastPublishedInstant(
    string rootUrn
  );

  Option<FloatValueAt> ReadTsFirst(
    string key,
    TimeSpan? from = null
  );

  Option<FloatValueAt> ReadTsLast(
    string key,
    TimeSpan? upTo = null
  );

  IEnumerable<FloatValueAt> ReadTsAll(
    string key
  );
}
